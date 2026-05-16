using IReadThis.Recommender.Models;
using Microsoft.AspNetCore.Mvc;
using DynamicDtoCore;
using System.Data.Common;
using System.Data.SqlTypes;
using IReadThis.Recommender.Services.DB;

namespace IReadThis.Recommender.Controllers
{
    [ApiController]
    [Route("[controller]")]
    public class BookCatalogController : Controller
    {
        private readonly ILogger<BookCatalogController> _logger;

        public BookCatalogController(ILogger<BookCatalogController> logger)
        {
            _logger = logger;
        }

        [HttpGet(Name = "GetBookCatalog")]
        public async Task<IActionResult> Get()
        {
            try
            {
                var books = await BookRepository.GetAllBooksAsync();

                return Ok(books);

            }
            catch (DbException ex)
            {
                _logger.LogError(ex, "Erro ao conectar ao banco de dados ao buscar catálogo de livros");
                return StatusCode(StatusCodes.Status503ServiceUnavailable, new { message = "Serviço de banco de dados indisponível. Retornando dados em cache." });
            }
            catch (InvalidOperationException ex)
            {
                _logger.LogError(ex, "Operação inválida ao executar query do catálogo de livros");
                return StatusCode(StatusCodes.Status500InternalServerError, new { message = "Erro ao processar dados do catálogo." });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Erro inesperado ao buscar catálogo de livros");
                return StatusCode(StatusCodes.Status500InternalServerError, new { message = "Erro inesperado ao buscar catálogo." });
            }
        }

        public IActionResult Index()
        {
            return View();
        }
    }
}
