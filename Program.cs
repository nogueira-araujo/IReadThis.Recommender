
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
        public static void Main(string[] args)
        {
            var builder = WebApplication.CreateBuilder(args);

            // Add services to the container.
            var session = PrepareServices(builder);

            builder.Services.AddControllers();
            // Learn more about configuring OpenAPI at https://aka.ms/aspnet/openapi
            builder.Services.AddOpenApi();

            var app = builder.Build();

            // Configura o gerenciamento de hardware para garantir o encerramento adequado dos recursos da GPU.
            HardwareShutdown(app, session);

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
        private static Session PrepareServices(WebApplicationBuilder builder)
        {
            // 1. Carrega o checkpoint mais recente do SQL ou inicializa novo
            Console.WriteLine("Carregando inteligência persistida...");
            var session = ModelCheckpointRepository.LoadLatestCheckpoint();

            // 2. Constrói os grafos (Towers)
            var readerTower = ReaderTowerCoreBuilder.BuildReaderTowerCore();
            var bookTower = BookTowerCoreBuilder.BuildBookTowerCore();

            // 3. Registra os Geradores como Singletons (compartilhando a mesma Session)
            var readerGenerator = new ReaderEmbeddingGenerator(session, readerTower.SexInput, readerTower.YearInput, readerTower.ReaderVectorOutput);
            builder.Services.AddSingleton<ReaderEmbeddingGenerator>(readerGenerator);

            // 4. Registra a Engine e o Serviço
            builder.Services.AddScoped<IRecommendationEngine, RecommendationEngine>();
            builder.Services.AddScoped<RecommendationService>();

            return session;
        }

        /// <summary>
        /// Performs hardware resource cleanup and registers a shutdown callback to release GPU-related resources when
        /// the application is stopping.
        /// </summary>
        /// <remarks>This method should be called during application initialization to ensure that GPU
        /// resources, such as TensorFlow sessions, are properly released when the application is shutting down. Proper
        /// cleanup helps prevent resource leaks and ensures that hardware resources are freed for subsequent
        /// use.</remarks>
        /// <param name="app">The web application instance whose lifetime events are used to trigger resource cleanup during shutdown.</param>
        /// <param name="readerSession">The session object representing the hardware or GPU resource context to be disposed when the application
        /// stops. Cannot be null.</param>
        private static void HardwareShutdown(WebApplication app, Session readerSession)
        {
            // Verificação simples para garantir que a GPU foi detectada e está sendo utilizada.
            if (!gpu) return;
            // Método para liberar recursos de hardware, como sessões do TensorFlow, caso seja necessário.
            // Pode ser chamado durante a finalização da aplicação ou em cenários específicos de gerenciamento de recursos.
            // =====================================================================
            // 5. GERENCIAMENTO DE HARDWARE (GRACEFUL SHUTDOWN)
            // =====================================================================
            // Esta é a peça fundamental para sistemas que utilizam GPU:
            // Garante que os tensores e a Sessão sejam destruídos na VRAM da RTX 4070 quando a API for parada.
            app.Lifetime.ApplicationStopping.Register(() =>
            {
                Console.WriteLine("Encerrando a API. Liberando recursos CUDA/GPU da RTX 4070...");
                readerSession.Dispose();
            });
        }
    }
}
