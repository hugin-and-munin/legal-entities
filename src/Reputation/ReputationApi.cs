using System.Text.Json;
using LegalEntities.Database;
using LegalEntityChecker;
using Microsoft.Extensions.Options;

namespace LegalEntities.Reputation;

public class ReputationApi : IReputationApi
{
    private readonly ReputationApiClient _reputationApiClient;
    private readonly IRepository _repository;

    public ReputationApi(IOptions<AppOptions> options, HttpClient client, IRepository repository)
    {
        client.DefaultRequestHeaders.Add("Authorization", options.Value.ApiKey);
        _reputationApiClient = new ReputationApiClient(options.Value.ApiBase, client);
        _repository = repository;
    }

    public async Task<LegalEntityInfoReponse?> Get(LegalEntityInfoRequest request, CancellationToken ct)
    {
        var companyInfo = await GetCompanyInfo(request.Tin, ct);
        if (companyInfo is null) return null;

        return new LegalEntityInfoReponse()
        {
            Name = companyInfo.Names.Items.FirstOrDefault()?.ShortName ?? string.Empty,
            Tin = Convert.ToInt64(companyInfo.Inn),
            IncorporationDate = companyInfo.RegistrationDate?.ToUnixTimeSeconds() ?? -1,
            AuthorizedCapital = Convert.ToInt32(companyInfo.AuthorizedCapitals.Items.FirstOrDefault()?.Sum ?? -1),
            EmployeesNumber = companyInfo.EmployeesInfo.Items.FirstOrDefault()?.Count ?? -1,
            Address = companyInfo.Addresses.Items.FirstOrDefault()?.UnsplittedAddress ?? string.Empty,
            LegalEntityStatus = companyInfo.Status.Status switch
            {
                Status_DA_Entities.Active => LegalEntityStatus.Active,
                Status_DA_Entities.Bankruptcy => LegalEntityStatus.Bankruptcy,
                Status_DA_Entities.InReorganizationProcess => LegalEntityStatus.InReorganizationProcess,
                Status_DA_Entities.InTerminationProcess => LegalEntityStatus.InTerminationProcess,
                Status_DA_Entities.Terminated => LegalEntityStatus.Terminated,
                _ => throw new NotImplementedException(),
            }
        };
    }

    public async Task<LegalEntityExtendedInfoResponse?> GetExtendedInfo(LegalEntityExtendedInfoRequest request, CancellationToken ct)
    {
        var companyInfo = await GetCompanyInfo(request.Tin, ct);
        if (companyInfo is null) return null;

        return new LegalEntityExtendedInfoResponse()
        {
            Name = companyInfo.Names.Items.FirstOrDefault()?.ShortName ?? string.Empty,
            Tin = Convert.ToInt64(companyInfo.Inn),
            IncorporationDate = companyInfo.RegistrationDate?.ToUnixTimeSeconds() ?? -1,
            AuthorizedCapital = Convert.ToInt32(companyInfo.AuthorizedCapitals.Items.FirstOrDefault()?.Sum ?? -1),
            EmployeesNumber = companyInfo.EmployeesInfo.Items.FirstOrDefault()?.Count ?? -1,
            Address = companyInfo.Addresses.Items.FirstOrDefault()?.UnsplittedAddress ?? string.Empty,
            LegalEntityStatus = companyInfo.Status.Status switch
            {
                Status_DA_Entities.Active => LegalEntityStatus.Active,
                Status_DA_Entities.Bankruptcy => LegalEntityStatus.Bankruptcy,
                Status_DA_Entities.InReorganizationProcess => LegalEntityStatus.InReorganizationProcess,
                Status_DA_Entities.InTerminationProcess => LegalEntityStatus.InTerminationProcess,
                Status_DA_Entities.Terminated => LegalEntityStatus.Terminated,
                _ => throw new NotImplementedException(),
            }
        };
    }

    private async Task<Company_DA_Entities?> GetCompanyInfo(long tin, CancellationToken ct)
    {
        // Fast path -> check in DB
        var response = await _repository.GetReputationResponse(tin, ct);

        // If found in DB and not older than 7 days -> return from DB
        // 7 days is a max TTL for legal entity info
        if (response != null && response.ReceivedAt > DateTimeOffset.Now - TimeSpan.FromDays(7))
        {
            return JsonSerializer.Deserialize<Company_DA_Entities>(response.Json);
        }

        // Otherwise slow path -> call to API
        var entitiesResponse = await _reputationApiClient.EntitiesIdAsync(null, $"{tin}", null, ct);
        if (entitiesResponse.Items.Count == 0) return null;

        var entity = entitiesResponse.Items.First();
        Company_DA_Entities companyInfo = await _reputationApiClient.EntitiesCompanyAsync(entity.Id, null, ct);

        // Upsert to DB to avoid multiple API calls
        response = new ReputationApiResponse()
        {
            Tin = tin,
            Json = JsonSerializer.Serialize(companyInfo),
            ReceivedAt = DateTimeOffset.UtcNow
        };
        await _repository.UpsertReputationResponse(response, ct);

        return companyInfo;
    }
}
