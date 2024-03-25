using Grpc.Core;
using LegalEntities.Reputation;
using LegalEntityChecker;
using Microsoft.Extensions.Caching.Memory;
using static LegalEntityChecker.LegalEntityChecker;

namespace LegalEntities;

public class LegalEntityChecker(IReputationApi _api, IMemoryCache _memoryCache) : LegalEntityCheckerBase
{
    private readonly MemoryCacheEntryOptions cacheEntryOptions = new()
    {
        SlidingExpiration = TimeSpan.FromHours(24)
    };

    public override async Task<BasicInfo?> GetBasicInfo(
        LegalEntityInfoRequest request,
        ServerCallContext context)
    {
        // The fastest way -> get report from memory cache
        object key = request.Tin;
        if (_memoryCache.TryGetValue<BasicInfo>(key, out var LegalEntityInfoReponse))
        {
            return LegalEntityInfoReponse;
        }

        // Slower way -> get report from Reputation API
        LegalEntityInfoReponse = await _api.GetBasicInfo(request, context.CancellationToken);

        if (LegalEntityInfoReponse is null) return null;

        // Cache the result
        _memoryCache.Set(key, LegalEntityInfoReponse, cacheEntryOptions);

        return LegalEntityInfoReponse;
    }

    public override async Task<ExtendedInfo?> GetExtendedInfo(
        LegalEntityInfoRequest request,
        ServerCallContext context)
    {
        // The fastest way -> get report from memory cache
        object key = request.Tin;
        if (_memoryCache.TryGetValue<ExtendedInfo>(key, out var LegalEntityInfoReponse))
        {
            return LegalEntityInfoReponse;
        }

        // Slower way -> get report from Reputation API
        LegalEntityInfoReponse = await _api.GetExtendedInfo(request, context.CancellationToken);

        if (LegalEntityInfoReponse is null) return null;

        // Cache the result
        _memoryCache.Set(key, LegalEntityInfoReponse, cacheEntryOptions);

        return LegalEntityInfoReponse;
    }
}
