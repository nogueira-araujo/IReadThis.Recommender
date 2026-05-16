using DynamicDtoCore;
using IReadThis.Recommender.Models;
using IReadThis.Recommender.Services.DB;
using Serilog.Parsing;
using System.Data.Common;
using System.Text.Json;
using Tensorflow;
using static Tensorflow.Binding;

namespace IReadThis.Recommender.Services.AI
{
    public class RecommendationEngine : IRecommendationEngine
    {
        private readonly ReaderEmbeddingGenerator _readerGenerator;
        private readonly BookTowerModel _bookTowerModel;
        private readonly ReaderTowerModel _readerTowerModel;
        // Sincroniza múltiplas requisições de treinamento para evitar uso simultâneo da Session
        // TensorFlow Session não é thread-safe para operações simultâneas
        private static readonly SemaphoreSlim _trainingSemaphore = new SemaphoreSlim(1, 1);

        // Injetamos os modelos para reaproveitar a Session única do Program.cs
        public RecommendationEngine(ReaderEmbeddingGenerator readerGenerator, BookTowerModel bookTowerModel, ReaderTowerModel readerTowerModel)
        {
            _readerGenerator = readerGenerator;
            _bookTowerModel = bookTowerModel;
            _readerTowerModel = readerTowerModel;
        }

        public async Task ProcessAndGenerateBookEmbeddingsAsync()
        {
            var books = await BookRepository.GetAllBooksAsync();
            var bookCategories = await BookRepository.GetAllBookCategoriesAsync();


            var bookIdToCategoryIds = bookCategories
                .GroupBy(bc => bc.BookId)
                .ToDictionary(g => g.Key, g => g.Select(bc => bc.CategoryId).ToList());

            // Build da torre do livro para o processo em lote
            // IMPORTANTE: Para o Engine (Batch), usamos uma sessão temporária 
            // para não interferir na sessão de inferência Singleton se estiverem em threads separadas.
            using (var batchSession = tf.Session())
            {
                batchSession.graph.as_default();
                var context = BookTowerCoreBuilder.BuildBookTowerCore(batchSession);
                var generator = new BookEmbeddingGenerator(batchSession, context.CategoryInput, context.TextInput, context.FinalEmbedding);
                batchSession.run(tf.global_variables_initializer());
                BookRepository.PersistBookEmbeddings(books, bookIdToCategoryIds, generator);
            }

        }

        public async Task TrainRecommendationModelAsync(int epochs = 10)
        {
            // Adquire lock para evitar múltiplas requisições de treinamento simultâneas
            await _trainingSemaphore.WaitAsync();
            try
            {
                const string ratingsQuery = @"
            SELECT r.Rating, p.BirthYear, p.Sex, b.BookID, b.Title, b.Author
            FROM Ratings r
            INNER JOIN Profiles p ON r.ProfileID = p.ProfileID
            INNER JOIN Books b ON r.BookID = b.BookID";

                const string categoriesQuery = "SELECT BookID, CategoryID FROM BookCategories";

                IEnumerable<dynamic> trainingData;
                IEnumerable<dynamic> bookCategories;

                using (DbConnection conn = ProviderHelper.CreateConnection())
                {
                    var factory = new DynamicClassFactory(conn.CreateCommand());
                    trainingData = GetTrainingData(ratingsQuery, factory);
                    bookCategories = GetCategories(categoriesQuery, factory);
                }

                int batchSize = trainingData.Count();
                if (batchSize == 0) return;

                // 1. Agrupamento de categorias para preenchimento da matriz
                var groupedCategories = bookCategories
                    .GroupBy(bc => (int)bc.BookID)
                    .ToDictionary(g => g.Key, g => g.Select(bc => (int)bc.CategoryID).ToList());

                // 2. Preparação das Matrizes (Batch)
                var birthYears = trainingData.Select(r => (int)r.BirthYear).ToArray();
                var sexIds = trainingData.Select(r => Tokenizer.GetSexId((string)r.Sex)).ToArray();
                var trueRatings = trainingData.Select(r => (float)r.Rating).ToArray();

                int[,] paddedCategories = new int[batchSize, 50];
                int[,] textTokens = new int[batchSize, 50];

                int index = 0;
                foreach (var row in trainingData)
                {
                    // Categorias com Padding
                    List<int> catIds = groupedCategories.GetValueOrDefault((int)row.BookID, new List<int>());
                    for (int c = 0; c < Math.Min(catIds.Count, 50); c++)
                        paddedCategories[index, c] = catIds[c];

                    // Tokenização de Texto (Título + Autor) utilizando o novo Tokenizer centralizado
                    string fullText = $"{row.Title} {row.Author}";
                    int[] tokens = fullText.TokenizeText(50);
                    for (int t = 0; t < 50; t++)
                        textTokens[index, t] = tokens[t];

                    index++;
                }

                // 3. Obter a sessão primeiro
                var session = this._readerGenerator.Session;

                // Valida que a Session não é null
                if (session == null)
                    throw new InvalidOperationException("Session do ReaderEmbeddingGenerator está null. Verifique se a inicialização em Program.cs foi concluída corretamente.");

                // 4. Reutilizar os modelos já construídos e inicializados em Program.cs
                // Isso evita a recriação de variáveis e garante que todas as variáveis estejam devidamente inicializadas
                var recommendationTrainerData = new RecommendationTrainerData(session, _readerTowerModel, _bookTowerModel);
                var trainer = new RecommendationTrainer(recommendationTrainerData);

                // 4.1 Inicializar variáveis do otimizador criadas pelo trainer
                // O AdamOptimizer cria variáveis adicionais (beta1_power, beta2_power, etc.) que precisam ser inicializadas
                try
                {
                    session.run(tf.global_variables_initializer());
                }
                catch (Exception ex)
                {
                    Console.WriteLine($"Aviso ao inicializar variáveis do otimizador: {ex.Message}");
                }

                Console.WriteLine($"Iniciando treinamento: {batchSize} amostras, {epochs} épocas.");

                // 5. Laço de Treinamento
                for (int epoch = 1; epoch <= epochs; epoch++)
                {
                    var batchData = new TrainBatchData(
                        birthYears,
                        sexIds,
                        paddedCategories,
                        textTokens,
                        trueRatings
                    );

                    // Executa o ajuste de pesos diretamente na VRAM da RTX 4070
                    float loss = trainer.TrainBatch(batchData);

                    Console.WriteLine($"Época {epoch}/{epochs} - Erro Médio (Loss): {loss:F4}");
                }

                // 6. Persistência do Checkpoint (Método de Extensão em ModelCheckpointRepository)
                await session.SaveCheckpointAsync(epochs);

                static IEnumerable<dynamic> GetTrainingData(string ratingsQuery, DynamicClassFactory factory)
                {
                    return factory.Select(ratingsQuery);
                }

                static IEnumerable<dynamic> GetCategories(string categoriesQuery, DynamicClassFactory factory)
                {
                    return factory.Select(categoriesQuery);
                }
            }
            catch(Exception ex)
            {
                throw new Exception("Erro durante o processo de treinamento do modelo de recomendação.", ex);
            }
            finally
            {
                // Sempre libera o lock
                _trainingSemaphore.Release();
            }
        }
    }
}