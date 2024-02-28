using LegalEntityChecker;

namespace LegalEntities.Reputation;

public interface IReputationApi
{
    public Task<LegalEntityInfoReponse?> Get(LegalEntityInfoRequest request, CancellationToken ct);
}
