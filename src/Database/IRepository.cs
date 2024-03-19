using LegalEntities.Reputation;

namespace LegalEntities.Database;

public interface IRepository
{
    public Task<ReputationApiResponse?> GetBasicInfo(long tin, CancellationToken ct);
    public Task UpsertBasicInfo(ReputationApiResponse response, CancellationToken ct);
    
    public Task<ReputationApiResponse?> GetProceedingsInfo(long tin, CancellationToken ct);
    public Task UpsertProceedingsInfo(ReputationApiResponse response, CancellationToken ct);
}
