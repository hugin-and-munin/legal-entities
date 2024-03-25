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
    private readonly ILogger<IReputationApi> _logger;

    public ReputationApi(
        IOptions<AppOptions> options,
        HttpClient client,
        IRepository repository,
        ILogger<IReputationApi> logger)
    {
        client.DefaultRequestHeaders.Add("Authorization", options.Value.ApiKey);
        _reputationApiClient = new ReputationApiClient(options.Value.ApiBase, client);
        _repository = repository;
        _logger = logger;
    }

    public async Task<BasicInfo?> GetBasicInfo(
            LegalEntityInfoRequest request,
            CancellationToken ct)
    {
        var companyInfo = await GetCompanyInfo(request.Tin, ct);
        var proceedingsInfo = await GetProceedingsInfo(request.Tin, ct);

        if (companyInfo is null || proceedingsInfo is null)
        {
            return null;
        }

        return GetBasicInfo(companyInfo, proceedingsInfo);
    }

    public async Task<ExtendedInfo?> GetExtendedInfo(
        LegalEntityInfoRequest request,
        CancellationToken ct)
    {
        var companyInfo = await GetCompanyInfo(request.Tin, ct);
        var proceedingsInfo = await GetProceedingsInfo(request.Tin, ct);

        if (companyInfo is null || proceedingsInfo is null)
        {
            return null;
        }

        var result = new ExtendedInfo();

        var basicInfo = GetBasicInfo(companyInfo, proceedingsInfo);

        var managerDto = companyInfo.Managers.Items
            .FirstOrDefault(x => x.IsActual.HasValue && x.IsActual.Value);

        Manager manager = new();

        if (managerDto is not null)
        {
            var managerName = managerDto.Entity.Name ?? string.Empty;
            if (!long.TryParse(managerDto.Entity.Inn, out var managerTin)) managerTin = -1;
            var managerPosition = managerDto.Position
                .FirstOrDefault(x => x.IsActual.HasValue && x.IsActual.Value)?.PositionName ?? string.Empty;

            manager.Name = managerName;
            manager.Tin = managerTin;
            manager.Position = managerPosition;
        }

        var shareholdersDtos = companyInfo.Shareholders.Items
            .Where(x => x.IsActual.HasValue && x.IsActual.Value)
            // For some reason Reputation API sometimes returns duplicates
            // That's why we use Distinct here
            .DistinctBy(x => x.Entity.Name);

        var shareholders = new List<Shareholder>();

        foreach (var shareholderDto in shareholdersDtos)
        {
            var shareholderName = shareholderDto.Entity.Name ?? string.Empty;
            if (!long.TryParse(shareholderDto.Entity.Inn, out var shareholderTin)) shareholderTin = -1;
            var shareValue = shareholderDto.Share.FirstOrDefault(x => x.IsActual.HasValue && x.IsActual.Value);
            var shareholderShare = shareValue?.FaceValue ?? -1.0;
            var shareholderSize = shareValue?.Size ?? -1.0;
            var shareholderType = shareholderDto.Entity.Type switch
            {
                EntityType_DA_Entities.Company => EntityType.Company,
                EntityType_DA_Entities.Person => EntityType.Person,
                EntityType_DA_Entities.ForeignCompany => EntityType.ForeignCompany,
                EntityType_DA_Entities.Entrepreneur => EntityType.Entrepreneur,
                EntityType_DA_Entities.MunicipalSubject => EntityType.MunicipalSubject,
                _ => throw new NotImplementedException(),
            };
            var shareholder = new Shareholder()
            {
                Name = shareholderName,
                Tin = shareholderTin,
                Share = shareholderShare,
                Size = shareholderSize,
                Type = shareholderType
            };
            shareholders.Add(shareholder);
        }

        result.BasicInfo = basicInfo;
        result.Manager = manager;
        result.Shareholders.AddRange(shareholders);

        return result;
    }
    private static BasicInfo GetBasicInfo(
        Company_DA_Entities companyInfo,
        ProceedingsInfo proceedingsInfo)
    {
        // Короткое имя компании
        var name = companyInfo.Names.Items.FirstOrDefault()?.ShortName ?? string.Empty;
        // ИНН
        if (!long.TryParse(companyInfo.Inn, out var tin)) tin = -1;
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
        Proceeding proceeding = new();

        if (proceedingsInfo.Aggregations.TryGetValue("ExecutionObject", out var executionsDtos))
        {
            var proceedigDto = executionsDtos.FirstOrDefault(x =>
                string.Equals(
                    x.Name,
                    "Оплата труда и иные выплаты по трудовым правоотношениям",
                    StringComparison.OrdinalIgnoreCase));

            if (proceedigDto is not null)
            {
                proceeding.Amount = proceedigDto.Sum ?? -1;
                proceeding.Count = proceedigDto.Count ?? -1;
                proceeding.Description = proceedigDto.Name;
            }

        }

        return new()
        {
            Name = name,
            Tin = tin,
            IncorporationDate = incorporationDate,
            AuthorizedCapital = authorizedCapital,
            EmployeesNumber = employeesNumber,
            Address = address,
            LegalEntityStatus = status,
            Proceedings = proceeding
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
        if (entitiesResponse.Items.Count == 0)
        {
            _logger.LogWarning("Failed to get company info for {tin}", tin);
            return null;
        }

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
        if (entitiesResponse.Items.Count == 0)
        {
            _logger.LogWarning("Failed to get proceedings info for {tin}", tin);
            return null;
        }

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
