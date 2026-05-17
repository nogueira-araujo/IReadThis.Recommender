using DynamicDtoCore;
using IReadThis.Recommender.Models;
using IReadThis.Recommender.Services.AI;
using System.Data.Common;
using System.Text.Json;

namespace IReadThis.Recommender.Services.DB
{
    public static class BookRepository
    {
        public static async Task<IEnumerable<IBook>> GetAllBooksAsync()
        {
            const string SQL = @"SELECT [BookID] ,[Title] ,[Author] ,[Publisher] ,[ReleaseYear] ,[PageCount]
                                  FROM [Books]
                                  order by
                                  Title, author, ReleaseYear;";

            var results = (IEnumerable<IBook>)new IBook[0];
            try
            {
                using (DbConnection conn = ProviderHelper.CreateConnection())
                {
                    var factory = new DynamicClassFactory(conn.CreateCommand());
                    results = factory.Select<IBook>(SQL);
                    return results;
                }
            }

            catch (Exception ex)
            {
                // Log the exception (not implemented here)
                return results;
            }
        }

        public static async Task<IEnumerable<IBookCategoryRelationship>> GetAllBookCategoriesAsync()
        {
            const string SQL = "SELECT BookID, CategoryID FROM BookCategories";

            var results = (IEnumerable<IBookCategoryRelationship>)new IBookCategoryRelationship[0];
            try
            {
                using (DbConnection conn = ProviderHelper.CreateConnection())
                {
                    var factory = new DynamicClassFactory(conn.CreateCommand());
                    results = factory.Select<IBookCategoryRelationship>(SQL);
                    return results;
                }
            }

            catch (Exception ex)
            {
                // Log the exception (not implemented here)
                return results;
            }
        }

        public static void PersistBookEmbeddings(IEnumerable<IBook> books, Dictionary<int, List<int>> bookIdToCategoryIds, BookEmbeddingGenerator generator)
        {
            try

            {
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
                    conn.Open();
                    var tran = conn.BeginTransaction();
                    try
                    {
                        using (var command = ProviderHelper.CreateCommand(conn, insertQuery))
                        {
                            command.CommandType = System.Data.CommandType.Text;
                            command.Transaction = tran;
                            foreach (var book in books)
                            {
                                var categoryIds = bookIdToCategoryIds.GetValueOrDefault(book.BookId, new List<int>());
                                var embeddingVector = generator.GenerateTensorEmbedding(book.Title, book.Author, categoryIds);
                                string jsonVector = JsonSerializer.Serialize(embeddingVector);

                                command.Parameters.Clear();
                                var p1 = command.CreateParameter(); p1.ParameterName = "@BookID"; p1.Value = book.BookId;
                                var p2 = command.CreateParameter(); p2.ParameterName = "@Embedding"; p2.Value = jsonVector;
                                command.Parameters.Add(p1); command.Parameters.Add(p2);
                                //command.Prepare();
                                command.ExecuteNonQuery();
                            }
                        }
                        tran.Commit();
                    }
                    catch (Exception ex)
                    {
                        tran.Rollback();
                        throw new Exception("Erro ao persistir embeddings dos livros no banco de dados.", ex);
                    }
                }
            }
            catch (Exception ex)
            {
                // Log the exception (not implemented here)
                throw new Exception("Erro ao persistir embeddings dos livros no banco de dados.", ex);
            }
        }

        public static IEnumerable<IBook> GetBooksByVector(string jsonVector)
        {
            // Query otimizada para o SQL Server 2025 com suporte a vetores
            // Buscamos os 5 livros com a menor distância de cosseno em relação ao perfil do leitor
            const string recommendQuery = @"
                                        SELECT TOP 5 
                                            b.BookId, 
                                            b.Title, 
                                            b.Author, 
                                            b.Publisher, 
                                            b.ReleaseYear, 
                                            b.PageCount
                                        FROM Books b
                                        INNER JOIN Sidecar_BookEmbeddings be ON b.BookId = be.BookId
                                        ORDER BY VECTOR_DISTANCE('cosine', be.Embedding, CAST({0} AS VECTOR(768))) ASC
                                    ";

            try
            {
                using (var conn = ProviderHelper.CreateConnection())
                {
                    var command = conn.CreateCommand();
                    // Utilizamos o DynamicDtoCore para mapear o resultado para a interface IBook
                    var factory = new DynamicClassFactory(command);
                    var books = factory.Select<IBook>(recommendQuery, jsonVector);
                    return books;
                }
            }
            catch (Exception ex)
            {
                // Log de erro seguindo o padrão de rigor técnico
                throw new InvalidOperationException("Falha na execução da busca vetorial no SQL Server.", ex);
            }
        }
    }
}

    
