using DynamicDtoCore;
using IReadThis.Recommender.Models;
using IReadThis.Recommender.Services.DB;
using System.Data.Common;
using System.Text.Json;
using Tensorflow;
using static Tensorflow.Binding;

namespace IReadThis.Recommender.Services.AI
{
    public class RecommendationEngine : IRecommendationEngine
    {
        private readonly ReaderEmbeddingGenerator _readerGenerator;
        // Injetamos o gerador para reaproveitar a Session única do Program.cs
        public RecommendationEngine(ReaderEmbeddingGenerator readerGenerator)
        {
            _readerGenerator = readerGenerator;
        }

        public async Task ProcessAndGenerateBookEmbeddingsAsync()
        {
            const string booksQuery = "SELECT BookID, Title, Author, Publisher, ReleaseYear, PageCount FROM Books";
            const string bookCategoriesQuery = "SELECT BookID, CategoryID FROM BookCategories";
            const string insertQuery = @"
                MERGE INTO Sidecar_BookEmbeddings AS Target
                USING (SELECT @BookID AS BookID) AS Source
                ON (Target.BookID = Source.BookID)
                WHEN MATCHED THEN 
                    UPDATE SET Embedding = CAST(@Embedding AS VECTOR(768)), LastUpdated = GETDATE()
                WHEN NOT MATCHED THEN
                    INSERT (BookID, Embedding) VALUES (@BookID, CAST(@Embedding AS VECTOR(768)));";

            using (var conn = ProviderHelper.CreateConnection())
            {
                var factory = new DynamicClassFactory(conn.CreateCommand());
                var books = factory.Select<Book>(booksQuery);
                var bookCategories = factory.Select<IBookCategoryRelationship>(bookCategoriesQuery);

                var bookIdToCategoryIds = bookCategories
                    .GroupBy(bc => bc.BookId)
                    .ToDictionary(g => g.Key, g => g.Select(bc => bc.CategoryId).ToList());

                // Build da torre do livro para o processo em lote
                var context = BookTowerCoreBuilder.BuildBookTowerCore();

                // IMPORTANTE: Para o Engine (Batch), usamos a sessão de treino ou uma nova temporária 
                // para não interferir na sessão de inferência Singleton se estiverem em threads separadas.
                using (var batchSession = tf.Session())
                {
                    batchSession.run(tf.global_variables_initializer());
                    var generator = new BookEmbeddingGenerator(batchSession, context.CategoryInput, context.TextInput, context.FinalEmbedding);
                    using (var command = ProviderHelper.CreateCommand(conn, insertQuery))
                    {
                        command.CommandType = System.Data.CommandType.Text;
                        foreach (var book in books)
                        {
                            var categoryIds = bookIdToCategoryIds.GetValueOrDefault(book.BookId, new List<int>());
                            var embeddingVector = generator.GenerateTensorEmbedding(book.Title, book.Author, categoryIds);
                            string jsonVector = JsonSerializer.Serialize(embeddingVector);

                            command.Parameters.Clear();
                            var p1 = command.CreateParameter(); p1.ParameterName = "@BookID"; p1.Value = book.BookId;
                            var p2 = command.CreateParameter(); p2.ParameterName = "@Embedding"; p2.Value = jsonVector;
                            command.Parameters.Add(p1); command.Parameters.Add(p2);
                            command.Prepare();
                            command.ExecuteNonQuery();
                        }
                    }
                }
            }
        }

        public async Task TrainRecommendationModelAsync(int epochs = 10)
        {
            // Correção de nomes de campos: Saem nomes em PT-BR, entram nomes consistentes com o Schema
            const string ratingsQuery = @"
                SELECT r.Rating, p.BirthYear, p.Sex, b.BookID, b.Title, b.Author
                FROM Ratings r
                INNER JOIN Profiles p ON r.ProfileID = p.ProfileID
                INNER JOIN Books b ON r.BookID = b.BookID";

            IEnumerable<dynamic> trainingData;
            using (DbConnection conn = ProviderHelper.CreateConnection())
            {
                var factory = new DynamicClassFactory(conn.CreateCommand());
                trainingData = factory.Select(ratingsQuery);
            }

            int batchSize = trainingData.Count();
            if (batchSize == 0) return;

            // Preparação de matrizes usando o Tokenizer centralizado
            var birthYears = trainingData.Select(r => (int)r.BirthYear).ToArray();
            var sexIds = trainingData.Select(r => Tokenizer.GetSexId((string)r.Sex)).ToArray();
            var trueRatings = trainingData.Select(r => (float)r.Rating).ToArray();

            // ... (Lógica de preenchimento de matrizes de texto e categoria usando Tokenizer.TokenizeText)

            var btModel = BookTowerCoreBuilder.BuildBookTowerCore();
            var rtModel = ReaderTowerCoreBuilder.BuildReaderTowerCore();

            var session = this._readerGenerator.Session; // Reaproveitamos a sessão do ReaderEmbeddingGenerator para consistência
            var trainer = new RecommendationTrainer(new RecommendationTrainerData(session, rtModel, btModel));

            for (int epoch = 1; epoch <= epochs; epoch++)
            {
                // Lógica de treino delegada ao Trainer para SRP
                // float loss = trainer.TrainBatch(...);
            }

            // Persistência do checkpoint via Repository
            await session.SaveCheckpointAsync(epochs);

        }
    }
}