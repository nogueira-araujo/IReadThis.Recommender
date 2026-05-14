using DynamicDtoCore;
using IReadThis.Recommender.Services.AI;
using Microsoft.AspNetCore.Mvc;
using System.Data;
using System.Data.Common;

namespace IReadThis.Recommender.Controllers
{
    public record RecommendationRequest(int BirthYear, char? Genre);

    [ApiController]
    [Route("[controller]")]
    public class RecommendationController : Controller
    {
        private readonly ILogger<RecommendationController> _logger;
        // Interface que será implementada no Passo 3
        private readonly IRecommendationEngine _engine;

        public RecommendationController(ILogger<RecommendationController> logger, IRecommendationEngine engine)
        {
            _logger = logger;
            _engine = engine;
        }

        // ENDPOINT 1: Recomendação Baseada no ID do Perfil
        // GET: api/recommendations/profile/15
        [HttpGet("profile/{id}")]
        public async Task<IActionResult> GetByProfile(int id)
        {
            try
            {
                var books = await RecommendationService.GetRecommendationsAsync(id, null, null);
                return Ok(books);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Erro ao processar recomendação por perfil.");
                return StatusCode(500, "Erro interno no processamento da recomendação.");
            }
        }

        // ENDPOINT 2: Recomendação Genérica (Cold Start)
        // GET: api/recommendations/generic?birthYear=1990&sex=M
        [HttpGet("generic")]
        public async Task<IActionResult> GetGeneric([FromQuery] int birthYear, [FromQuery] string sex)
        {
            try
            {
                var books = await RecommendationService.GetRecommendationsAsync(null, birthYear, sex);
                return Ok(books);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Erro ao processar recomendação genérica.");
                return StatusCode(500, "Erro interno no processamento da recomendação.");
            }
        }
    }
}
