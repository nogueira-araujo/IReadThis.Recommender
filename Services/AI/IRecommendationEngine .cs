using System.Threading.Tasks;

namespace IReadThis.Recommender.Services.AI
{
    public interface IRecommendationEngine
    {
        /// <summary>
        /// Gera embeddings para todos os livros do catálogo (Processamento em lote).
        /// </summary>
        Task ProcessAndGenerateBookEmbeddingsAsync();

        /// <summary>
        /// Executa o loop de treinamento do modelo baseado nas avaliações existentes.
        /// </summary>
        Task TrainRecommendationModelAsync(int epochs = 10);
    }
}
