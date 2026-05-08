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
            const string insertQuery = @"INSERT INTO BookEmbeddings (BookID, Embedding) 
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
    }
}
