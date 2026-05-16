using DynamicDtoCore;
using IReadThis.Recommender.Services.DB;
using System.IO.Compression;
using Tensorflow;
using static Tensorflow.Binding;

namespace IReadThis.Recommender.Services.DB
{
    public static class ModelCheckpointRepository
    {
        const string FILE_NAME = "model.ckpt";
        const string ZIP_FILE_EXTENSION = ".zip";

        private static readonly string tempFileDir = Path.Combine(Path.GetTempPath(), "IReadThis_Train_" + Guid.NewGuid());
        public static string TempFileDir {  get { return tempFileDir; } }
        public static Task SaveCheckpointAsync(this Session session, int epochs) {
            try
            {
                Directory.CreateDirectory(tempFileDir);
                string checkpointPrefix = Path.Combine(tempFileDir, FILE_NAME);

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
            string tempZipPath = Path.Combine(checkpointDirectory + ZIP_FILE_EXTENSION);
            ZipFile.CreateFromDirectory(checkpointDirectory, tempZipPath);
            byte[] zipBytes = File.ReadAllBytes(tempZipPath);

            using (var connection = ProviderHelper.CreateConnection())
            {
                connection.Open();
                string insertQuery = @"
                    INSERT INTO Sidecar_ModelCheckpoints (VersionName, ModelZipData) 
                    VALUES (@Version, @Data)";
                var command = ProviderHelper.CreateCommand(connection, insertQuery);
                command.CommandType = System.Data.CommandType.Text;

                var param1 = command.CreateParameter();
                    param1.ParameterName = "@Version";
                     var value = DateTime.Now.ToString("yyyy-MM-dd HH:mm:ss");
                    param1.Value = value;
                    param1.DbType = System.Data.DbType.String;
                    param1.Size = value.Length;
                var param2 = command.CreateParameter();
                    param2.ParameterName = "@Data";
                    param2.Value = zipBytes;
                    param2.Size = zipBytes.Length;
                    param2.DbType = System.Data.DbType.Binary;

                command.Parameters.Add(param1);
                command.Parameters.Add(param2);
                command.Prepare();
                command.ExecuteNonQuery();
            }

            File.Delete(tempZipPath);
        }

        // Método para resgatar e extrair o Checkpoint mais recente
        public static  async Task LoadLatestCheckpointAsync(Session session)
        {
            var readerSession = session;
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
                string tempZipPath = Path.Combine(tempFileDir, Guid.NewGuid().ToString() + ZIP_FILE_EXTENSION);

                File.WriteAllBytes(tempZipPath, modelZipData);
                ZipFile.ExtractToDirectory(tempZipPath, tempFileDir);
                string checkpointPrefix = Path.Combine(tempFileDir, FILE_NAME);
                File.Delete(tempZipPath);

                if (Directory.Exists(tempFileDir))
                {
                    var saver = tf.train.Saver();
                    saver.restore(readerSession, checkpointPrefix);
                    Console.WriteLine("Pesos da Rede Neural carregados do SQL Server com sucesso!");
                    Directory.Delete(tempFileDir, true);
                }
                else
                {
                    Console.WriteLine("Checkpoint extraído, mas arquivo de pesos não encontrado...");
                }
            }
            else
            {
                Console.WriteLine("Nenhum modelo encontrado. Inicializando pesos aleatórios...");
            }
        }
    }
}