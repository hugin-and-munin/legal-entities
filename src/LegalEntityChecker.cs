using Microsoft.Extensions.Caching.Memory;
using Grpc.Core;
using LegalEntities.Database;
using LegalEntityChecker;
using static LegalEntityChecker.LegalEntityChecker;

namespace LegalEntities;

public class LegalEntityChecker(
    IMemoryCache _memoryCache,
    MemoryCacheEntryOptions _memoryCacheOptions,
    ReputationApiClient _reputationApiClient,
    IRepository _repository,
    ILogger<LegalEntityChecker> _logger) : LegalEntityCheckerBase
{
    private record EntityIdKey(long Tin);
    private record LegalEntityIdKey(long Tin);
    private delegate Task<T> ReputationApiCall<T>(ReputationApiClient client, Guid entityId, CancellationToken ct);

    public override async Task<LegalEntityInfo?> GetLegalEntityInfo(LegalEntityInfoRequest request, ServerCallContext context)
    {
        var tin = request.Tin;
        var legalEntityInfoKey = new LegalEntityIdKey(tin);

        if (_memoryCache.TryGetValue<LegalEntityInfo>(legalEntityInfoKey, out var legalEntityInfo))
        {
            return legalEntityInfo;
        }

        var basicInfo = await GetBasicInfo(tin, context.CancellationToken);

        if (basicInfo is null) return null;

        var proceedingsInfo = await GetProceedingsInfo(tin, context.CancellationToken);
        var financeInfo = await GetFinanceInfo(tin, context.CancellationToken);

        legalEntityInfo = new LegalEntityInfo()
        {
            BasicInfo = basicInfo,
            ProceedingsInfo = proceedingsInfo,
            FinanceInfo = financeInfo
        };

        _memoryCache.Set(legalEntityInfoKey, legalEntityInfo, _memoryCacheOptions);

        return legalEntityInfo;
    }

    private async Task<BasicInfo?> GetBasicInfo(long tin, CancellationToken ct)
    {
        Company_DA_Entities? reponse = await GetAsync(
            tin,
            (client, entityId, ct) =>
                client.EntitiesCompanyAsync(entityId, null, ct),
            ct);

        if (reponse is null) return null;

        // Короткое имя компании
        var name = reponse.Names.Items.FirstOrDefault()?.ShortName ?? string.Empty;
        // Дата регистрации
        var incorporationDate = reponse.RegistrationDate?.ToUnixTimeSeconds() ?? -1;
        // Уставной капитал
        var authorizedCapital = reponse.AuthorizedCapitals.Items.FirstOrDefault()?.Sum ?? -1;
        // Количество сотрудников
        var employeesNumber = reponse.EmployeesInfo.Items.FirstOrDefault()?.Count ?? -1;
        // Юридический адрес
        var address = reponse.Addresses.Items.FirstOrDefault()?.UnsplittedAddress ?? string.Empty;
        // Статус
        var status = reponse.Status.Status switch
        {
            Status_DA_Entities.Active => LegalEntityStatus.Active,
            Status_DA_Entities.Bankruptcy => LegalEntityStatus.Bankruptcy,
            Status_DA_Entities.InReorganizationProcess => LegalEntityStatus.InReorganizationProcess,
            Status_DA_Entities.InTerminationProcess => LegalEntityStatus.InTerminationProcess,
            Status_DA_Entities.Terminated => LegalEntityStatus.Terminated,
            _ => throw new NotImplementedException(),
        };

        Manager manager = new();

        var managerDto = reponse.Managers.Items
            .FirstOrDefault(x => x.IsActual.HasValue && x.IsActual.Value);

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

        var shareholdersDtos = reponse.Shareholders.Items
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

        var basicInfo = new BasicInfo()
        {
            Name = name,
            Tin = tin,
            IncorporationDate = incorporationDate,
            AuthorizedCapital = authorizedCapital,
            EmployeesNumber = employeesNumber,
            Address = address,
            LegalEntityStatus = status,
            Manager = manager,
        };

        basicInfo.Shareholders.AddRange(shareholders);

        return basicInfo;
    }

    private async Task<ProceedingsInfo?> GetProceedingsInfo(long tin, CancellationToken ct)
    {
        CollectionContainerWithAggregations_DA_Proceeding_DA_Fssp_SumAggregationItem_DA_SumAggregationItem? reponse = await GetAsync(
            tin,
            (client, entityId, ct) =>
                client.FsspProceedingsAsync(
                entityId,
                entityType: EntityTypeDto_A.Company,
                statuses: [],
                executionObjects: [],
                minYear: null,
                maxYear: null,
                orderBy: null,
                page: null,
                cancellationToken: ct),
            ct);

        if (reponse is null) return null;

        ProceedingsInfo proceedingsInfo = new();

        if (reponse.Aggregations.TryGetValue("ExecutionObject", out var executionsDtos))
        {
            var proceedigDto = executionsDtos.FirstOrDefault(x =>
                string.Equals(
                    x.Name,
                    "Оплата труда и иные выплаты по трудовым правоотношениям",
                    StringComparison.OrdinalIgnoreCase));

            if (proceedigDto is not null)
            {
                proceedingsInfo.Amount = proceedigDto.Sum ?? -1;
                proceedingsInfo.Count = proceedigDto.Count ?? -1;
                proceedingsInfo.Description = proceedigDto.Name;
            }
        }

        return proceedingsInfo;
    }

    private async Task<FinanceInfo?> GetFinanceInfo(long tin, CancellationToken ct)
    {
        ICollection<ReportPeriod_DA_FinancialReports>? financePeriods = await GetAsync(
            tin,
            (client, entityId, ct) =>
                client.FinancePeriodsAsync(
                entityId,
                entityType: EntityType_DA_Entities.Company,
                cancellationToken: ct),
            ct);

        if (financePeriods is null) return null;

        int? year = financePeriods.Select(x => x.Year).LastOrDefault();

        if (year is null) return null;

        FinancialCalculation_DA_FinancialReports? financeReport = await GetAsync(
            tin,
            (client, entityId, ct) =>
                client.FinanceValuesAsync(
                valueCodes: null,
                year: year.Value,
                quarter: null,
                entityId,
                entityType: EntityType_DA_Entities.Company,
                cancellationToken: ct),
            ct);

        if (financeReport is null) return null;

        year = financeReport.Year;

        // Выручка
        double income =
            financeReport.Values.TryGetValue("21103", out var incomeValue) ?
                incomeValue.Value ?? -1 : -1;

        // Чистая прибыль
        double profit =
            financeReport.Values.TryGetValue("24003", out var profitValue) ?
                profitValue.Value ?? -1 : -1;

        // Дебиторская задолженность
        double аccountsReceivable =
            financeReport.Values.TryGetValue("12303", out var debtorsValue) ?
                debtorsValue.Value ?? -1 : -1;

        // Капитал и резервы
        double capitalAndReserves =
            financeReport.Values.TryGetValue("13003", out var capitalAndReservesValue) ?
                capitalAndReservesValue.Value ?? -1 : -1;

        // Долгосрочные обязательства
        double longTermLiabilities =
            financeReport.Values.TryGetValue("14003", out var longTermLiabilitiesValue) ?
                longTermLiabilitiesValue.Value ?? -1 : -1;

        // Краткосрочные обязательства
        double currentLiabilities =
            financeReport.Values.TryGetValue("15003", out var currentLiabilitiesValue) ?
                currentLiabilitiesValue.Value ?? -1 : -1;

        // Платежи в связи с оплатой труда работников
        double paidSalary =
            financeReport.Values.TryGetValue("41223", out var paidSalaryValue) ?
                paidSalaryValue.Value ?? -1 : -1;

        return new FinanceInfo()
        {
            Year = year.Value,
            Income = income,
            Profit = profit,
            AccountsReceivable = аccountsReceivable,
            CapitalAndReserves = capitalAndReserves,
            LongTermLiabilities = longTermLiabilities,
            CurrentLiabilities = currentLiabilities,
            PaidSalary = paidSalary
        };
    }

    private async Task<T?> GetAsync<T>(long tin, ReputationApiCall<T> apiCall, CancellationToken ct)
    {
        T? result = default;

        // Fast path -> check DB
        var response = await _repository.GetAsync<T>(tin, ct);

        // Check if the cached value is not expired
        if (response != null && !response.IsExpired)
        {
            result = response.TryDeserialze<T>();
            return result;
        }

        // Slow path -> call to API
        var entityIdKey = new EntityIdKey(tin);
        if (!_memoryCache.TryGetValue<Guid>(entityIdKey, out var entityId) || entityId == Guid.Empty)
        {
            var entitiesResponse = await _reputationApiClient.EntitiesIdAsync(null, $"{tin}", null, ct);
            if (entitiesResponse.Items.Count == 0)
            {
                _logger.LogError("Failed to get entity ID for tin = {tin}", tin);
                return result;
            }

            entityId = entitiesResponse.Items.First().Id;
            _memoryCache.Set(entityIdKey, entityId, _memoryCacheOptions);
        }

        result = await apiCall(_reputationApiClient, entityId, ct);

        // Save to DB
        await _repository.UpsertAsync(tin, result, ct);

        return result;
    }
}

public class FinanceReportMap : Dictionary<int, string> { }