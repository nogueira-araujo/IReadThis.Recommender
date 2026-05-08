namespace IReadThis.Recommender.Services.AI
{
    public interface IRecommendationEngine
    {
        Task ProcessAndGenerateBookEmbeddingsAsync();
    }
}
