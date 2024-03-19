using System.Text.Json;
using LegalEntities.Database;
using LegalEntityChecker;
using Microsoft.Extensions.Options;
using ProceedingsInfo = LegalEntities.CollectionContainerWithAggregations_DA_Proceeding_DA_Fssp_SumAggregationItem_DA_SumAggregationItem;

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
        var proceedingsInfo = await GetProceedingsInfo(request.Tin, ct);

        if (companyInfo is null) return null;

        // Короткое имя компании
        var name = companyInfo.Names.Items.FirstOrDefault()?.ShortName ?? string.Empty;
        // ИНН
        var tin = long.TryParse(companyInfo.Inn, out var cached) ? cached : -1;
        // Дата регистрации
        var incorporationDate = companyInfo.RegistrationDate?.ToUnixTimeSeconds() ?? -1;
        // Уставной капитал
        var authorizedCapital = companyInfo.AuthorizedCapitals.Items.FirstOrDefault()?.Sum ?? -1;
        // Количество сотрудников
        var employeesNumber = companyInfo.EmployeesInfo.Items.FirstOrDefault()?.Count ?? -1;
        // Юридический адрес
        var address = companyInfo.Addresses.Items.FirstOrDefault()?.UnsplittedAddress ?? string.Empty;
        // Статус
        var status = companyInfo.Status.Status switch
        {
            Status_DA_Entities.Active => LegalEntityStatus.Active,
            Status_DA_Entities.Bankruptcy => LegalEntityStatus.Bankruptcy,
            Status_DA_Entities.InReorganizationProcess => LegalEntityStatus.InReorganizationProcess,
            Status_DA_Entities.InTerminationProcess => LegalEntityStatus.InTerminationProcess,
            Status_DA_Entities.Terminated => LegalEntityStatus.Terminated,
            _ => throw new NotImplementedException(),
        };

        // Сведения о невыплате зарплаты
        bool salaryDelays = false;

        if (proceedingsInfo != null && 
            proceedingsInfo.Aggregations.TryGetValue("ExecutionObject", out var executions))
        {
            foreach (var e in executions)
            {
                if (string.Equals(
                    e.Name,
                    "Оплата труда и иные выплаты по трудовым правоотношениям",
                    StringComparison.OrdinalIgnoreCase))
                {
                    salaryDelays = true;
                }
            }
        }

        return new LegalEntityInfoReponse()
        {
            Name = name,
            Tin = tin,
            IncorporationDate = incorporationDate,
            AuthorizedCapital = authorizedCapital,
            EmployeesNumber = employeesNumber,
            Address = address,
            LegalEntityStatus = status,
            SalaryDelays = salaryDelays
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
        var response = await _repository.GetBasicInfo(tin, ct);

        // If found in DB and not older than 30 days -> return from DB
        // 30 days is a max TTL for legal entity info
        if (response != null && response.ReceivedAt > DateTimeOffset.Now - TimeSpan.FromDays(30))
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
        await _repository.UpsertBasicInfo(response, ct);

        return companyInfo;
    }

    private async Task<ProceedingsInfo?> GetProceedingsInfo(long tin, CancellationToken ct)
    {
        // Fast path -> check in DB
        var response = await _repository.GetProceedingsInfo(tin, ct);

        // If found in DB and not older than 30 days -> return from DB
        // 30 days is a max TTL for legal entity info
        if (response != null && response.ReceivedAt > DateTimeOffset.Now - TimeSpan.FromDays(30))
        {
            return JsonSerializer.Deserialize<ProceedingsInfo>(response.Json);
        }

        // Otherwise slow path -> call to API
        var entitiesResponse = await _reputationApiClient.EntitiesIdAsync(null, $"{tin}", null, ct);
        if (entitiesResponse.Items.Count == 0) return null;

        var entity = entitiesResponse.Items.First();
        ProceedingsInfo proceedingsInfo = await _reputationApiClient.FsspProceedingsAsync(
            entity.Id,
            entityType: EntityTypeDto_A.Company,
            statuses: [],
            executionObjects: [],
            minYear: null,
            maxYear: null,
            orderBy: null,
            page: null,
            cancellationToken: ct);

        // Upsert to DB to avoid multiple API calls
        response = new ReputationApiResponse()
        {
            Tin = tin,
            Json = JsonSerializer.Serialize(proceedingsInfo),
            ReceivedAt = DateTimeOffset.UtcNow
        };
        await _repository.UpsertProceedingsInfo(response, ct);

        return proceedingsInfo;
    }
}
