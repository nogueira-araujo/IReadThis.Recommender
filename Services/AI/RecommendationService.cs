using DynamicDtoCore;
using IReadThis.Recommender.Models;
using IReadThis.Recommender.Services.AI;
using System.Text.Json;

public class RecommendationService
{
    private readonly ReaderEmbeddingGenerator _generator;

    // Injeção de dependência do gerador Singleton
    public RecommendationService(ReaderEmbeddingGenerator generator)
    {
        _generator = generator;
    }

    public async Task<IEnumerable<IBook>> GetRecommendationsAsync(int? profileId, int? birthYear, string sex)
    {
        // 1. Resolução de Perfil (Mantida a lógica do DynamicDto)
        if (profileId.HasValue)
        {
            // ... busca no banco ...
        }

        if (!birthYear.HasValue) throw new ArgumentException("BirthYear é obrigatório.");

        // 2. INFERÊNCIA LIMPA: Usamos o gerador que já possui a Session e os pesos carregados
        // Não há tf.Session() aqui, reduzindo latência e uso de memória.
        var readerVector = _generator.GenerateTensorEmbedding(birthYear.Value, sex);

        // 3. Consulta Vetorial (Híbrida)
        return await ExecuteVectorSearch(readerVector);
    }

    /// <summary>
    /// Realiza a busca vetorial híbrida no SQL Server utilizando similaridade de cosseno.
    /// </summary>
    private async Task<IEnumerable<IBook>> ExecuteVectorSearch(float[] readerVector)
    {
        // Converte o array de float para a representação JSON esperada pelo SQL Server
        string jsonVector = JsonSerializer.Serialize(readerVector);

        // Query otimizada para o SQL Server 2025 com suporte a vetores
        // Buscamos os 5 livros com a menor distância de cosseno em relação ao perfil do leitor
        const string recommendQuery = @"
        SELECT TOP 5 
            b.BookId, 
            b.Title, 
            b.Author, 
            b.Publisher, 
            b.ReleaseYear, 
            b.PageCount
        FROM Books b
        INNER JOIN Sidecar_BookEmbeddings be ON b.BookId = be.BookId
        ORDER BY VECTOR_DISTANCE('cosine', be.Embedding, CAST({0} AS VECTOR(768))) ASC
    ";

        try
        {
            using (var conn = ProviderHelper.CreateConnection())
            {
                var command = conn.CreateCommand();
                // Utilizamos o DynamicDtoCore para mapear o resultado para a interface IBook
                var factory = new DynamicClassFactory(command);
                return factory.Select<IBook>(recommendQuery, jsonVector);
            }
        }
        catch (Exception ex)
        {
            // Log de erro seguindo o padrão de rigor técnico
            throw new InvalidOperationException("Falha na execução da busca vetorial no SQL Server.", ex);
        }
    }
}