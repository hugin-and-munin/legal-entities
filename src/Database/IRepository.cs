using LegalEntities.Reputation;

namespace LegalEntities.Database;

public interface IRepository
{
    public Task<ReputationApiResponse?> GetReputationResponse(long tin, CancellationToken ct);
    public Task UpsertReputationResponse(ReputationApiResponse response, CancellationToken ct);
}
