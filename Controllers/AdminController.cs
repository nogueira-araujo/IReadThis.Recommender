using Microsoft.AspNetCore.Mvc;
using System;
using System.Threading.Tasks;
using IReadThis.Recommender.Services.AI;

namespace IReadThis.Recommender.Controllers
{
    [ApiController]
    [Route("api/[controller]")]
    public class AdminController : ControllerBase
    {
        private readonly IRecommendationEngine _recommendationEngine;

        // Injetamos a engine de lote que registramos como Transient no Program.cs
        public AdminController(IRecommendationEngine recommendationEngine)
        {
            _recommendationEngine = recommendationEngine ?? throw new ArgumentNullException(nameof(recommendationEngine));
        }

        /// <summary>
        /// Gatilho Administrativo: Dispara o treinamento da Rede Neural.
        /// Lê a tabela Ratings, treina as Duas Torres, salva os vetores em BookEmbeddings 
        /// e persiste os pesos do modelo em ModelCheckpoints.
        /// </summary>
        /// <param name="epochs">Número de épocas para o treinamento (Padrão: 10)</param>
        /// <example>
        /// POST https://localhost:7109/api/admin/train?epochs=10
        /// </example>
        [HttpPost("train")]
        public async Task<IActionResult> TrainModel([FromQuery] int epochs = 10)
        {
            try
            {
                // Inicia o processamento pesado assíncrono
                await _recommendationEngine.TrainRecommendationModelAsync(epochs);

                return Ok(new
                {
                    message = "Treinamento concluído com sucesso! O banco de dados foi atualizado.",
                    epochsRun = epochs,
                    timestamp = DateTime.UtcNow
                });
            }
            catch (Exception ex)
            {
                // Tratamento de erro detalhado para facilitar o debug pelo Postman
                return StatusCode(500, new
                {
                    message = "Falha crítica durante o treinamento do modelo de IA.",
                    details = ex.Message
                });
            }
        }
    }
}
