using DynamicDtoCore;
using IReadThis.Recommender.Services.DB;
using System.IO.Compression;
using Tensorflow;
using static Tensorflow.Binding;

namespace IReadThis.Recommender.Services.DB
{
    public static class ModelCheckpointRepository
    {
        private static readonly string tempFileDir = Path.Combine(Path.GetTempPath(), "IReadThis_Train_" + Guid.NewGuid());
        public static string TempFileDir {  get { return tempFileDir; } }
        public static Task SaveCheckpointAsync(this Session session, int epochs) {
            try
            {
                Directory.CreateDirectory(tempFileDir);
                string checkpointPrefix = Path.Combine(tempFileDir, "model.ckpt");

                var saver = tf.train.Saver();
                saver.save(session, checkpointPrefix);

                SaveCheckpointAsync(tempFileDir, epochs);

                // 3. Limpeza da memória local
                Directory.Delete(tempFileDir, true);
                Console.WriteLine("Inteligência persistida com sucesso pelo ModelCheckpointRepository!");

                return Task.FromResult(true);
            } catch (Exception ex) { return Task.FromException(ex); }
        }

        // Método para compactar e salvar os pesos no SQL Server
        private static void SaveCheckpointAsync(string checkpointDirectory, int epochs)
        {
            string tempZipPath = checkpointDirectory + ".zip";
            ZipFile.CreateFromDirectory(checkpointDirectory, tempZipPath);
            byte[] zipBytes = File.ReadAllBytes(tempZipPath);

            using (var connection = ProviderHelper.CreateConnection())
            {
                string insertQuery = @"
                    INSERT INTO ModelCheckpoints (VersionName, ModelZipData) 
                    VALUES (@Version, @Data)";
                var command = ProviderHelper.CreateCommand(connection, insertQuery);

                var param1 = command.CreateParameter();
                    param1.ParameterName = "@Version";
                    param1.Value = $"Epochs_{epochs}_" + DateTime.Now.ToString("yyyyMMddHHmm");
                var param2 = command.CreateParameter();
                    param2.ParameterName = "@Data";
                    param2.Value = zipBytes;

                command.Parameters.Add(param1);
                command.Parameters.Add(param2);
                command.ExecuteNonQuery();
            }

            File.Delete(tempZipPath);
        }

        // Método para resgatar e extrair o Checkpoint mais recente
        public static Session LoadLatestCheckpoint()
        {
            var readerSession = tf.Session();
            byte[] modelZipData = null;

            const string query = "SELECT TOP 1 ModelZipData FROM Sidecar_ModelCheckpoints ORDER BY CheckpointID DESC";

            using (var connection = ProviderHelper.CreateConnection())
            {

                var factory = new DynamicClassFactory(connection.CreateCommand());
                var response = factory.Select(query);
                if (response != null && response.Count() > 0)
                {
                    modelZipData = response.First().ModelZipData;
                }

            }

            if (modelZipData != null && modelZipData.Length > 0)
            {
                Directory.CreateDirectory(tempFileDir);
                string tempZipPath = Path.Combine(tempFileDir, Guid.NewGuid().ToString() + ".zip");

                File.WriteAllBytes(tempZipPath, modelZipData);
                ZipFile.ExtractToDirectory(tempZipPath, tempFileDir);
                File.Delete(tempZipPath);

                
                var saver = tf.train.Saver();
                string checkpointPrefix = Path.Combine(tempFileDir, "model.ckpt");
                saver.restore(readerSession, checkpointPrefix);
                Console.WriteLine("Pesos da Rede Neural carregados do SQL Server com sucesso!");
            }
            else
            {
                Console.WriteLine("Nenhum modelo encontrado. Inicializando pesos aleatórios...");
                readerSession.run(tf.global_variables_initializer());
            }
            return readerSession;
        }
    }
}