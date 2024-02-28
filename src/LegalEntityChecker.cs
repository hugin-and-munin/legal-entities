using System.Collections.Concurrent;
using Grpc.Core;
using LegalEntities.Reputation;
using LegalEntityChecker;
using static LegalEntityChecker.LegalEntityChecker;

namespace LegalEntities;

public class LegalEntityChecker(IReputationApi _api) : LegalEntityCheckerBase
{
    private readonly ConcurrentDictionary<long, LegalEntityInfoReponse> _cache = new();

    public override async Task<LegalEntityInfoReponse?> Get(LegalEntityInfoRequest request, ServerCallContext context)
    {
        // The fastest way -> get report from memory cache
        if (_cache.TryGetValue(request.Tin, out var LegalEntityInfoReponse)) return LegalEntityInfoReponse;

        // Slower way -> get report from Reputation API
        LegalEntityInfoReponse = await _api.Get(request, context.CancellationToken);
        
        if (LegalEntityInfoReponse is null) return null;

        // Cache the result
        _cache.TryAdd(request.Tin, LegalEntityInfoReponse);
        
        return LegalEntityInfoReponse;
    }
}
