using DynamicDtoCore;
using IReadThis.Recommender.Models;
using IReadThis.Recommender.Services.AI;
using System.Text.Json;
using System.Linq;
using IReadThis.Recommender.Services.DB;

public class RecommendationService
{
    private readonly ReaderEmbeddingGenerator _generator;

    // Injeção de dependência do gerador Singleton
    public RecommendationService(ReaderEmbeddingGenerator generator)
    {
        _generator = generator;
    }

    public async Task<IEnumerable<IBook>> GetRecommendationsAsync(int? profileId,int? birthYear, string sex)
    {
        int? birthVal;
        string sexVal;
        birthVal = birthYear;
        sexVal = sex;

        // 1. Resolução de Perfil (Mantida a lógica do DynamicDto)
        if (profileId.HasValue)
        {
            // Busca os dados do perfil no banco de dados
            var searchKey = ProfileRepository.GetByProfileId(profileId.Value);
            birthVal = searchKey.BirthYear;
            sexVal = searchKey.Sex;
        }

        if (!birthVal.HasValue) throw new ArgumentException("BirthYear é obrigatório.");
        if (string.IsNullOrWhiteSpace(sexVal)) throw new ArgumentException("Sex é obrigatório.");

        // 2. INFERÊNCIA LIMPA: Usamos o gerador que já possui a Session e os pesos carregados
        // Não há tf.Session() aqui, reduzindo latência e uso de memória.
        var readerVector = _generator.GenerateTensorEmbedding(birthVal.Value, sexVal);

        // 3. Consulta Vetorial (Híbrida)
        return await ExecuteVectorSearchAsync(readerVector);
    }

    /// <summary>
    /// Realiza a busca vetorial híbrida no SQL Server utilizando similaridade de cosseno.
    /// </summary>
    private async Task<IEnumerable<IBook>> ExecuteVectorSearchAsync(float[] readerVector)
    {
        // Converte o array de float para a representação JSON esperada pelo SQL Server
        string jsonVector = JsonSerializer.Serialize(readerVector);

        return BookRepository.GetBooksByVector(jsonVector);
    }
}