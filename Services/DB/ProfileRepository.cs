using DynamicDtoCore;
using IReadThis.Recommender.Models;
using IReadThis.Recommender.Services.AI;
using System.Data.Common;
using System.Text.Json;

namespace IReadThis.Recommender.Services.DB
{
    public static class ProfileRepository
    {
        public static IRecommendationSearchKey GetByProfileId(int profileId)
        {
            // Busca os dados do perfil no banco de dados
            const string profileQuery = @"
            SELECT BirthYear, Sex 
            FROM Profiles 
            WHERE ProfileID = {0}";

            try
            {
                using (var conn = ProviderHelper.CreateConnection())
                {
                    var factory = new DynamicDtoCore.DynamicClassFactory(conn.CreateCommand());
                    var result = factory.Select<IRecommendationSearchKey>(profileQuery, profileId).FirstOrDefault();
                    return result;
                }
            }
            catch (Exception ex)
            {
                throw new InvalidOperationException($"Erro ao buscar perfil com ID {profileId}.", ex);
            }
        }
    }
}
