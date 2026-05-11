using DynamicDtoCore;
using System;
using System.Collections.Generic;
using System.Linq;
using Tensorflow;
using Tensorflow.NumPy;
using static Tensorflow.Binding;

namespace IReadThis.Recommender.Services.AI
{
    /// <summary>
    /// Classe que encapsula os tensores de entrada e a sessão do TensorFlow para o processo de treinamento do modelo de recomendação.
    /// </summary>
    public class RecommendationTrainerData
    {

        private readonly Session _session;
        // Este modelo é responsável por criar as camadas de Embedding e processar os dados de entrada
        // Ele não contém a lógica de treinamento, apenas a definição da arquitetura da rede neural
        public Tensor SexInput { get; set; }
        public Tensor YearInput { get; set; }
        public Tensor ReaderVector { get; set; }
        public Tensor CategoryInput { get; set; }
        public Tensor TextInput { get; set; }
        public Tensor BookVector { get; set; }
        public Session Session { get; private set; }

        /// <summary>
        /// Initializes a new instance of the RecommendationTrainerData class using the specified session.
        /// </summary>
        /// <param name="session">The session context to associate with this instance. Cannot be null.</param>
        public RecommendationTrainerData(Session session)
        {
            if (session == null)
                throw new ArgumentNullException(nameof(session), "Session cannot be null.");
            this._session = session;
        }
        public RecommendationTrainerData(Session session, Tensor sexInput, Tensor yearInput, Tensor readerVector, Tensor categoryInput, Tensor textInput, Tensor bookVector) :this(session)
        {
            SexInput = sexInput;
            YearInput = yearInput;
            ReaderVector = readerVector;
            CategoryInput = categoryInput;
            TextInput = textInput;
            BookVector = bookVector;
        }

        public RecommendationTrainerData(Session session, ReaderTowerModel readerModel, BookTowerModel bookModel) : 
            this(session, readerModel.SexInput, readerModel.YearInput, readerModel.ReaderVectorOutput, bookModel.CategoryInput, bookModel.TextInput, bookModel.FinalEmbedding)
        {}
    }
    public class TrainBatchData
    {
        public TrainBatchData () { }
        public TrainBatchData(int[] birthYears, int[] sexIds, int[,] paddedCategories, int[,] textTokens, float[] trueRatings) :this()
        {
            BirthYears = birthYears;
            SexIds = sexIds;
            PaddedCategories = paddedCategories;
            TextTokens = textTokens;
            TrueRatings = trueRatings;
        }

        public int[] BirthYears { get; set; }
        public int[] SexIds { get; set; }
        public int[,] PaddedCategories { get; set; }
        public int[,] TextTokens { get; set; }
        public float[] TrueRatings { get; set; }
    }

    public class RecommendationTrainer
    {
        // Ponteiros do Treinamento
        private readonly Tensor _trueRatingInput;
        private readonly Tensor _loss;
        private readonly Operation _trainOp;

        private readonly RecommendationTrainerData model;

        public RecommendationTrainer(RecommendationTrainerData model)
        {
            // ==========================================
            // 1. A LÓGICA DE CRUZAMENTO (DOT PRODUCT)
            // ==========================================
            // Multiplicamos os tensores (element-wise) e somamos o eixo 1 para obter um valor escalar (A Nota Prevista)
            var predictedRating = tf.reduce_sum(tf.multiply(model.BookVector, model.ReaderVector), axis: 1);

            // ==========================================
            // 2. FUNÇÃO DE PERDA (MEAN SQUARED ERROR)
            // ==========================================
            // Placeholder para recebermos a nota real do banco de dados (0 a 4)
            _trueRatingInput = tf.placeholder(tf.float32, shape: new[] { -1 }, name: "true_rating");

            // Calculamos o erro: (Nota Prevista - Nota Real)^2
            _loss = tf.reduce_mean(tf.square(predictedRating - _trueRatingInput));

            // ==========================================
            // 3. OTIMIZADOR (BACKPROPAGATION)
            // ==========================================
            // O AdamOptimizer vai navegar pela rede ajustando os pesos para diminuir o erro (Loss)
            var optimizer = tf.train.AdamOptimizer(learning_rate: 0.001f);
            _trainOp = optimizer.minimize(_loss);
        }

        
        // Método que executará o laço de treinamento com os dados reais do DynamicDto
        public float TrainBatch(TrainBatchData data)
        {
            if (data == null)
                throw new ArgumentNullException(nameof(data), "TrainBatchData cannot be null.");

            // Criamos os Shapes para processamento em lote (Batch)
            int batchSize = data.BirthYears.Length;

            // Normalizando os anos de nascimento
            float[] normalizedYears = data.BirthYears.Select(y => (y - 1900) / 100.0f).ToArray();

            // Transformando em matrizes NumPy para injetar na GPU
            var sexNdArray = np.array(data.SexIds).reshape(new Shape(batchSize));
            var yearNdArray = np.array(normalizedYears).reshape(new Shape(batchSize, 1));
            var categoryNdArray = np.array(data.PaddedCategories).reshape(new Shape(batchSize, 50)); // Assumindo máximo de 50
            var textNdArray = np.array(data.TextTokens).reshape(new Shape(batchSize, 50));
            var ratingNdArray = np.array(data.TrueRatings).reshape(new Shape(batchSize));

            // Mapeando os dados da memória RAM para as portas da Rede Neural
            var feedDict = new FeedItem[]
            {
                new FeedItem(this.model.SexInput, sexNdArray),
                new FeedItem(this.model.YearInput, yearNdArray),
                new FeedItem(this.model.CategoryInput, categoryNdArray),
                new FeedItem(this.model.TextInput, textNdArray),
                new FeedItem(this._trueRatingInput, ratingNdArray)
            };

            // Executa a operação de Treinamento e extrai o valor de Perda (Loss) da iteração
            var result = this.model.Session.run(new object[] {this._trainOp, this._loss }, feedDict);

            // O result[5] contém o valor do erro numérico. Quanto mais próximo de 0, mais inteligente o modelo está.
            float currentLoss = (float)result[5];
            return currentLoss;
        }

        // Método de Treinamento a ser adicionado na RecommendationEngine
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
                    p.Sex Sex Sex,
                    b.BookID BookId,
                    b.Title Title,
                    b.Author Author
                FROM Ratings r
                INNER JOIN Profiles p ON r.ProfileID = p.ProfileID
                INNER JOIN Books b ON r.BookID = b.BookID";
            // Buscamos as categorias e agrupamos por BookID (reaproveitando a lógica de extração)
            const string categoriesQuery = "SELECT BookID, CategoryID FROM BookCategories";

            IEnumerable<dynamic> trainingData;
            IEnumerable<dynamic> bookCategories;
            // Utilizamos o DynamicDtoCore para ler a massa de dados sem rastreamento (ReadOnly)
            using (var conn = ProviderHelper.CreateConnection())
            {
                var factory = new DynamicClassFactory(conn.CreateCommand());
                trainingData = factory.Select(ratingsQuery);

                factory = new DynamicClassFactory(conn.CreateCommand());
                bookCategories = factory.Select(categoriesQuery);
            }

            var groupedCategories = bookCategories
                .GroupBy(bc => bc.BookID)
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
                birthYears[index] = row.BirthYear;
                sexIds[index] = ObterSexId(row.Sex);

                // B. Feature de Avaliação: Nota real do banco (0 a 4)
                trueRatings[index] = (float)row.Rating;

                // C. Feature do Livro: Categorias (Padding)
                List<int> catIds = groupedCategories.ContainsKey(row.BookId)
                    ? groupedCategories[row.BookId]
                    : new List<int>();

                for (int c = 0; c < Math.Min(catIds.Count, 50); c++)
                {
                    paddedCategories[index, c] = catIds[c];
                }

                // D. Feature do Livro: Texto (Tokenização simples de Título + Autor)
                string fullText = $"{row.Title} {row.Author}";
                int[] tokens = TokenizeText(fullText, 50);
                for (int t = 0; t < 50; t++)
                {
                    textTokens[index, t] = tokens[t];
                }

                index++;
            }

            // ====================================================================
            // 3. EXECUÇÃO DO LAÇO DE TREINAMENTO (EPOCHS) NO TENSORFLOW
            // ====================================================================

            // Instanciamos as Torres para gerar o Grafo Completo
            //var bookBuilder = new BookTowerCoreBuilder();
            var btModel = BookTowerCoreBuilder.BuildBookTowerCore();

            //var readerBuilder = new ReaderTowerCoreBuilder();
            var rtModel = ReaderTowerCoreBuilder.BuildReaderTowerCore();

            using (var session = tf.Session())
            {
                session.run(tf.global_variables_initializer());

                var trainer = new RecommendationTrainer(new RecommendationTrainerData(session, rtModel.SexInput, rtModel.YearInput, rtModel.ReaderVectorOutput, btModel.CategoryInput, btModel.TextInput, btModel.FinalEmbedding));

                Console.WriteLine($"Iniciando treinamento com {batchSize} avaliações...");

                // O modelo iterará 'epochs' vezes sobre a mesma massa para reduzir o erro
                for (int epoch = 1; epoch <= epochs; epoch++)
                {
                    float loss = trainer.TrainBatch(new TrainBatchData(
                        birthYears, sexIds,
                        paddedCategories, textTokens,
                        trueRatings));

                    Console.WriteLine($"Época {epoch}/{epochs} - Erro Médio (Loss): {loss:F4}");
                }

                // Após o loop, as instâncias de tf.Variable na memória estarão "inteligentes".
                // Aqui você chamaria uma rotina para salvar esses pesos treinados no disco (ex: .pb ou checkpoint)
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

        private int[] TokenizeText(string text, int maxLength)
        {
            // Implementação simplificada de Hashing para converter palavras em IDs
            int[] tokens = new int[maxLength];
            var words = text.Split(' ', StringSplitOptions.RemoveEmptyEntries);

            for (int i = 0; i < Math.Min(words.Length, maxLength); i++)
            {
                tokens[i] = Math.Abs(words[i].GetHashCode()) % 10000; // Vocabulário de 10.000 palavras
            }
            return tokens;
        }
    }
}
