using DynamicDtoCore;
using IReadThis.Recommender.Models;
using Microsoft.Data.SqlClient;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text.Json;
using System.Threading.Tasks;
using Tensorflow;
using static Tensorflow.Binding;
using IReadThis.Recommender.Services.AI;
using System.Data.Common;

namespace IReadThis.Recommender.Services.AI
{
    public class RecommendationEngine : IRecommendationEngine
    {
        public RecommendationEngine() {}

        public async Task ProcessAndGenerateBookEmbeddingsAsync()
        {
            // PASSO 1: Extração de Dados Legados (Modo ReadOnly)
            // Lemos a tabela Books pura, sem conhecimento de vetores ou IA
            const string booksQuery = "SELECT BookID, Title, Author, Publisher, ReleaseYear, PageCount FROM Books";
            // Lemos as categorias atreladas a cada livro
            const string bookCategoriesQuery = "SELECT BookID, CategoryID FROM BookCategories";
            // O comando de inserção na tabela proprietária de vetores, onde o vetor é armazenado como um tipo específico do SQL Server (VECTOR(768)).
            const string insertQuery = @"INSERT INTO Sidecar_BookEmbeddings (BookID, Embedding) 
            VALUES (@BookID, CAST(@Embedding AS VECTOR(768)))";

            IEnumerable<IBook> books;
            IEnumerable<IBookCategoryRelationship> bookCategories;

            using (var conn = ProviderHelper.CreateConnection())
            {
                var factory = new DynamicClassFactory(conn.CreateCommand());
                books = factory.Select<IBook>(booksQuery);
                bookCategories = factory.Select<IBookCategoryRelationship>(bookCategoriesQuery);

                // PASSO 2: Preparação e Agrupamento na Memória
                // Agrupamos as categorias por livro para formar a base do contexto semântico
                var groupedCategories = bookCategories
                    .GroupBy(bc => bc.BookId).ToArray();

                var bookIdToCategoryIds = new Dictionary<int, List<int>>();
                foreach (var group in groupedCategories.Distinct())
                {
                    bookIdToCategoryIds.Add(group.Key, group.Select(bc => bc.CategoryId).ToList());
                }
                //var bookIdToCategoryIds = groupedCategories.ToDictionary(g => (int)g.Key, g => g.Select(bc => (int)bc.CategoryId).ToList());

                // 2. INICIALIZAÇÃO DA INTELIGÊNCIA ARTIFICIAL (A SEQUÊNCIA ENTRA AQUI)
                // Constrói o grafo matemático e obtém os ponteiros das portas de entrada e saída armazenados em uma classe de contexto.
                var context = BookTowerCoreBuilder.BuildBookTowerCore();

                using (var session = new Tensorflow.Session())
                {
                    // IMPORTANTE: Antes de executar qualquer operação no grafo, é necessário inicializar as variáveis (Variables) do TensorFlow.
                    session.run(tf.global_variables_initializer());

                    var generator = new BookEmbeddingGenerator(session, context.CategoryInput, context.TextInput, context.FinalEmbedding);

                    foreach (var book in books)
                    {
                        int bookId = book.BookId;
                        string title = book.Title;
                        string author = book.Author;

                        // Resgatamos as categorias do livro atual.
                        List<int> categoryIds = bookIdToCategoryIds.ContainsKey(bookId) ? bookIdToCategoryIds[bookId] : new List<int>();

                        // PASSO 3: Processamento com TensorFlow.NET (CPU)
                        // Aqui transformaremos 'title', 'author' e 'categoryIds' em um tensor...
                        var embeddingVector = generator.GenerateTensorEmbedding(title, author, categoryIds);

                        // Converte o array para a representação de string JSON (ex: "[0.12, -0.45, ...]")
                        string jsonVector = JsonSerializer.Serialize(embeddingVector);

                        // PASSO 4: Persistência no Sidecar Pattern
                        // Aqui executaremos um comando de INSERT/UPDATE apenas na tabela proprietária 'BookEmbeddings'
                        // passando o 'bookId' e o vetor gerado.
                        using (var command = ProviderHelper.CreateCommand(conn, insertQuery))
                        {
                            // Parametrização forte evitando Injeção de SQL e garantindo performance
                            
                            var param1 = command.CreateParameter();
                            var param2 = command.CreateParameter();
                            param1.ParameterName = "@BookID";
                            param1.Value = bookId;
                            param2.ParameterName = "@Embedding";
                            param2.Value = jsonVector;
                            command.Parameters.Add(param1);
                            command.Parameters.Add(param2);
                            command.Prepare(); // Prepara o comando para execução, otimizando a performance ao reutilizar o plano de execução.
                            command.ExecuteNonQuery();
                        }
                    }
                }
            }
        }

        public async Task TrainRecommendationModelAsync(int epochs = 10)
        {
            // ====================================================================
            // 1. EXTRAÇÃO DOS DADOS VIA DYNAMIC DTO CORE
            // ====================================================================

            // Buscamos as avaliações (0 a 4) cruzando os dados do Leitor e do Livro
            const string ratingsQuery = @"
                SELECT 
                    r.Rating Rating,
                    p.BirthYear BirthYear,
                    p.Sex Sex,
                    b.BookID BookId,
                    b.Title Title,
                    b.Author Author
                FROM Ratings r
                INNER JOIN Profiles p ON r.ProfileID = p.ProfileID
                INNER JOIN Books b ON r.BookID = b.BookID";
            const string categoriesQuery = "SELECT BookID BookId, CategoryID CategoryId FROM BookCategories";

            IEnumerable<dynamic> trainingData = new List<dynamic>();
            IEnumerable<dynamic> bookCategories = new List<dynamic>();
            using (DbConnection conn = ProviderHelper.CreateConnection())
            {
                var factory = new DynamicClassFactory(conn.CreateCommand());
                trainingData = factory.Select(ratingsQuery);
                factory = new DynamicClassFactory(conn.CreateCommand());
                bookCategories = factory.Select(categoriesQuery);
            }
           
            var groupedCategories = bookCategories
                .GroupBy(bc => bc.BookId)
                .ToDictionary(g => (int)g.Key, g => g.Select(bc => (int)bc.CategoryID).ToList());

            // ====================================================================
            // 2. PREPARAÇÃO DAS MATRIZES (BATCH) PARA A GPU
            // ====================================================================
            int batchSize = trainingData.Count();

            int[] birthYears = new int[batchSize];
            int[] sexIds = new int[batchSize];
            int[,] paddedCategories = new int[batchSize, 50]; // Limite de 50 acordado
            int[,] textTokens = new int[batchSize, 50];       // Limite de 50 acordado
            float[] trueRatings = new float[batchSize];

            int index = 0;
            foreach (var row in trainingData)
            {
                // A. Feature do Leitor: Ano de Nascimento e Sexo
                birthYears[index] = row.AnoNascimento;
                sexIds[index] = ObterSexId(row.Sexo);

                // B. Feature de Avaliação: Nota real do banco (0 a 4)
                trueRatings[index] = (float)row.Rating;

                // C. Feature do Livro: Categorias (Padding)
                List<int> catIds = groupedCategories.ContainsKey(row.BookID)
                    ? groupedCategories[row.BookID]
                    : new List<int>();

                for (int c = 0; c < Math.Min(catIds.Count, 50); c++)
                {
                    paddedCategories[index, c] = catIds[c];
                }

                // D. Feature do Livro: Texto (Tokenização simples de Título + Autor)
                string fullText = $"{row.Title} {row.Author}";
                int[] tokens = fullText.TokenizeText(50);
                for (int t = 0; t < 50; t++)
                {
                    textTokens[index, t] = tokens[t];
                }

                index++;
            }

            // ====================================================================
            // 3. EXECUÇÃO DO LAÇO DE TREINAMENTO (EPOCHS) NO TENSORFLOW
            // ====================================================================

            // Instanciamos as Torres para gerar o Grafo Complet
            var bookTower = BookTowerCoreBuilder.BuildBookTowerCore();

            var readerTower = ReaderTowerCoreBuilder.BuildReaderTowerCore();

            using (var session = tf.Session())
            {
                session.run(tf.global_variables_initializer());

                var trainer = new RecommendationTrainer(new RecommendationTrainerData(session,readerTower, bookTower));

                Console.WriteLine($"Iniciando treinamento com {batchSize} avaliações...");

                // O modelo iterará 'epochs' vezes sobre a mesma massa para reduzir o erro
                for (int epoch = 1; epoch <= epochs; epoch++)
                {
                    var trainBatchData = new TrainBatchData
                    {
                        BirthYears = birthYears,
                        SexIds = sexIds,
                        PaddedCategories = paddedCategories,
                        TextTokens = textTokens,
                        TrueRatings = trueRatings
                    };
                    float loss = trainer.TrainBatch(trainBatchData);
                    Console.WriteLine($"Época {epoch}/{epochs} - Erro Médio (Loss): {loss:F4}");
                }

                // Após o loop, as instâncias de tf.Variable na memória estarão "inteligentes".
                // Aqui chamar rotina para salvar os pesos do modelo em um formato proprietário (ex: tabela SQL, arquivo binário, etc.)
                // para que a API (RecommendationService) possa carregá-los no Program.cs.
            }
        }

        // --- Métodos Auxiliares ---
        private int ObterSexId(string sexo)
        {
            if (string.IsNullOrWhiteSpace(sexo)) return 0;
            if (sexo.Equals("M", StringComparison.OrdinalIgnoreCase)) return 1;
            if (sexo.Equals("F", StringComparison.OrdinalIgnoreCase)) return 2;
            return 0;
        }
    }
}
