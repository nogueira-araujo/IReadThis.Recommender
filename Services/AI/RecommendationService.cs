using DynamicDtoCore;
using IReadThis.Recommender.Models;
using Microsoft.Data.SqlClient;
using System.Text.Json;
using static System.Runtime.InteropServices.JavaScript.JSType;
using static Tensorflow.Binding;

namespace IReadThis.Recommender.Services.AI
{
    public class RecommendationService
    {

        // Método unificado que atende aos dois Endpoints da Visão de Funcionalidades
        public static async Task<IEnumerable<IBook>> GetRecommendationsAsync(int? profileId, int? birthYear, string sex)
        {
            // 1. RESOLUÇÃO DE DADOS (Endpoint 1 vs Endpoint 2)
            if (profileId.HasValue)
            {
                // Se passou o ProfileID, usamos o DynamicDtoCore para ler o legado em modo ReadOnly
                string profileQuery = $"SELECT BirthYear, Sex FROM Profiles WHERE ProfileID = {profileId.Value}";
                dynamic? profile;
                using (var conn = ProviderHelper.CreateConnection())
                {
                    var factory = new DynamicClassFactory(conn.CreateCommand());
                    var profiles = factory.Select(profileQuery);
                    profile = profiles.FirstOrDefault();
                }

                if (profile != null)
                {
                    birthYear = profile.BirthYear;
                    sex = profile.Sex;
                }
            }

            if (!birthYear.HasValue)
                throw new ArgumentException("Ano de nascimento é obrigatório para o Cold Start.");

            // 2. GERAÇÃO DO VETOR DO LEITOR (INFERÊNCIA EM TEMPO REAL)
            float[] readerVector;

            var readerTowerModel = ReaderTowerCoreBuilder.BuildReaderTowerCore();

            using (var session = tf.Session())
            {
                session.run(tf.global_variables_initializer());
                var generator = new ReaderEmbeddingGenerator(session, readerTowerModel.SexInput, readerTowerModel.YearInput, readerTowerModel.ReaderVectorOutput);

                // Gera o vetor de 768 dimensões com base no perfil
                readerVector = generator.GenerateTensorEmbedding(birthYear.Value, sex);
            }

            // 3. CONSULTA VETORIAL HÍBRIDA NO SQL SERVER 2025
            string jsonVector = JsonSerializer.Serialize(readerVector);

            // Utilizamos VECTOR_DISTANCE para calcular a similaridade de cosseno. 
            // Os menores valores de distância representam a maior probabilidade de nota 3 ou 4.
            string recommendQuery = @"
                SELECT TOP 5 
                    b.BookId, 
                    b.Title, 
                    b.Author, 
                    b.Publisher, 
                    b.ReleaseYear, 
                    b.PageCount
                FROM Books b
                INNER JOIN BookEmbeddings be ON b.BookId = be.BookId
                ORDER BY VECTOR_DISTANCE('cosine', be.Embedding, CAST(@ReaderVector AS VECTOR(768))) ASC
            ";
            IEnumerable<IBook> toReturn = new IBook[0];
            using (var conn = ProviderHelper.CreateConnection())
            {
                var command = conn.CreateCommand();
                var vectoParam = command.CreateParameter();
                vectoParam.ParameterName = "@ReaderVector";
                vectoParam.Value = jsonVector;
                command.Parameters.Add(vectoParam);
                var factory = new DynamicClassFactory(command);
                toReturn = factory.Select<IBook>(recommendQuery);
            }

            return toReturn;
        }
    }

}
