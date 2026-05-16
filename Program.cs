
using DynamicDtoCore;
using Google.Protobuf.WellKnownTypes;
using IReadThis.Recommender.Services.AI;
using IReadThis.Recommender.Services.DB;
using Microsoft.AspNetCore.Builder;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using System;
using Tensorflow;
using static Tensorflow.Binding;
namespace IReadThis.Recommender
{
    public class Program
    {
        private static bool gpu = false;
        public static async Task Main(string[] args)
        {
            var builder = WebApplication.CreateBuilder(args);

            // Add services to the container.
            var sessionManager = await PrepareServicesAsync(builder);

            builder.Services.AddControllers();
            // Learn more about configuring OpenAPI at https://aka.ms/aspnet/openapi
            builder.Services.AddOpenApi();

            var app = builder.Build();

            // Configura o gerenciamento de hardware para garantir o encerramento adequado dos recursos.
            HardwareShutdown(app, sessionManager);

            // Configure the HTTP request pipeline.
            if (app.Environment.IsDevelopment())
            {
                app.MapOpenApi();
            }

            app.UseHttpsRedirection();
            app.UseAuthorization();
            app.MapControllers();
            app.Run();
        }

        /// <summary>
        /// Método responsável por preparar e registrar os serviços de IA (Torre do Leitor e Torre do Livro) na DI do ASP.NET Core.
        /// </summary>
        /// <param name="builder"></param>
        private static async Task<SessionManager> PrepareServicesAsync(WebApplicationBuilder builder)
        {
            var session = tf.Session(); // Cria uma sessão TensorFlow vazia para ser preenchida com o checkpoint

            // 1. Cria o SessionManager para gerenciar o ciclo de vida da Session
            var sessionManager = new SessionManager(session);
            builder.Services.AddSingleton<SessionManager>(sessionManager);

            // 2. Constrói os grafos (Towers) usando a sessão carregada
            var readerTower = ReaderTowerCoreBuilder.BuildReaderTowerCore(session);
            var bookTower = BookTowerCoreBuilder.BuildBookTowerCore(session);

            // 3. Inicializa variáveis do grafo após a construção das towers
            // Isso garante que TODAS as variáveis (restauradas e novas) estejam inicializadas
            try
            {
                session.run(tf.global_variables_initializer());
                Console.WriteLine("Variáveis do grafo TensorFlow inicializadas com sucesso!");
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Aviso ao inicializar variáveis: {ex.Message}");
            }

            // 4. Carrega o checkpoint mais recente do SQL ou inicializa novo
            Console.WriteLine("Carregando inteligência persistida...");
            await ModelCheckpointRepository.LoadLatestCheckpointAsync(session);

            // 5. Registra os Modelos das Towers como Singletons para reutilização
            builder.Services.AddSingleton<ReaderTowerModel>(readerTower);
            builder.Services.AddSingleton<BookTowerModel>(bookTower);

            // 7. Registra os Geradores como Singletons (compartilhando a mesma Session)
            var readerGenerator = new ReaderEmbeddingGenerator(session, readerTower.SexInput, readerTower.YearInput, readerTower.ReaderVectorOutput);
            builder.Services.AddSingleton<ReaderEmbeddingGenerator>(readerGenerator);

            // 8. Registra a Engine e o Serviço
            builder.Services.AddScoped<IRecommendationEngine, RecommendationEngine>();
            builder.Services.AddScoped<RecommendationService>();

            return sessionManager;
        }

        /// <summary>
        /// Performs hardware resource cleanup and registers a shutdown callback to release TensorFlow-related resources when
        /// the application is stopping.
        /// </summary>
        /// <remarks>This method should be called during application initialization to ensure that TensorFlow
        /// resources and GPU memory, such as TensorFlow sessions, are properly released when the application is shutting down. Proper
        /// cleanup helps prevent resource leaks and ensures that hardware resources are freed for subsequent
        /// use.</remarks>
        /// <param name="app">The web application instance whose lifetime events are used to trigger resource cleanup during shutdown.</param>
        /// <param name="sessionManager">The SessionManager object that manages the TensorFlow session lifecycle.</param>
        private static void HardwareShutdown(WebApplication app, SessionManager sessionManager)
        {
            // =====================================================================
            // GERENCIAMENTO DE HARDWARE (GRACEFUL SHUTDOWN)
            // =====================================================================
            // Esta é a peça fundamental para sistemas que utilizam GPU/TensorFlow:
            // Garante que os tensores e a Sessão sejam destruídos na VRAM da RTX 4070 quando a API for parada.
            // Agora SEMPRE executa o cleanup, independente da flag gpu.
            app.Lifetime.ApplicationStopping.Register(() =>
            {
                try
                {
                    Console.WriteLine("Encerrando a API. Liberando recursos CUDA/GPU da RTX 4070...");
                    sessionManager.Dispose();
                    Console.WriteLine("Recursos CUDA/GPU liberados com sucesso!");
                }
                catch (Exception ex)
                {
                    Console.WriteLine($"Erro durante cleanup de recursos: {ex.Message}");
                }
            });
        }
    }
}
