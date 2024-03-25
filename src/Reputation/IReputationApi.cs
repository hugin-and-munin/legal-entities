using LegalEntityChecker;

namespace LegalEntities.Reputation;

public interface IReputationApi
{
    public Task<BasicInfo?> GetBasicInfo(LegalEntityInfoRequest request, CancellationToken ct);
    public Task<ExtendedInfo?> GetExtendedInfo(LegalEntityInfoRequest request, CancellationToken ct);
}
