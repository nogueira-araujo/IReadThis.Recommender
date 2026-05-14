namespace IReadThis.Recommender.Services.AI
{
    public interface IRecommendationEngine
    {
        Task ProcessAndGenerateBookEmbeddingsAsync();
        Task TrainRecommendationModelAsync(int epochs = 10);
    }
}
