using Dapper;
using Microsoft.Extensions.Options;
using Npgsql;
using LegalEntitiesInfo = LegalEntities.Company_DA_Entities;
using ProceedingsInfo = LegalEntities.CollectionContainerWithAggregations_DA_Proceeding_DA_Fssp_SumAggregationItem_DA_SumAggregationItem;
using FinancialReports = LegalEntities.FinancialCalculation_DA_FinancialReports;

namespace LegalEntities.Database;

public class Repository(IOptions<AppOptions> _options) : IRepository
{
    private readonly NpgsqlConnection connection = new(_options.Value.DbConnectionString);

    private readonly string _selectLegalEntitiesInfo =
        @"SELECT ""Tin"", ""Json"", ""ReceivedAt""
        FROM ""LegalEntities"" WHERE ""Tin"" = @Tin";

    private readonly string _selectProceedingsInfo =
        @"SELECT ""Tin"", ""Json"", ""ReceivedAt""
        FROM ""Proceedings"" WHERE ""Tin"" = @Tin";

    private readonly string _selectFinancialPeriods =
        @"SELECT ""Tin"", ""Json"", ""ReceivedAt""
        FROM ""FinancialPeriods"" WHERE ""Tin"" = @Tin";

    private readonly string _selectFinancialReports =
        @"SELECT ""Tin"", ""Json"", ""ReceivedAt""
        FROM ""FinancialReports"" WHERE ""Tin"" = @Tin";

    private readonly string _insertLegalEntities =
            @"INSERT INTO ""LegalEntities"" (""Tin"", ""Json"", ""ReceivedAt"") 
            VALUES (@Tin, @Json, @ReceivedAt)
            ON CONFLICT (""Tin"") 
            DO UPDATE SET 
            ""Json"" = EXCLUDED.""Json"", 
            ""ReceivedAt"" = EXCLUDED.""ReceivedAt""";
    private readonly string _insertProceedings =
            @"INSERT INTO ""Proceedings"" (""Tin"", ""Json"", ""ReceivedAt"") 
            VALUES (@Tin, @Json, @ReceivedAt)
            ON CONFLICT (""Tin"") 
            DO UPDATE SET 
            ""Json"" = EXCLUDED.""Json"", 
            ""ReceivedAt"" = EXCLUDED.""ReceivedAt""";
    private readonly string _insertFinancialPeriods =
            @"INSERT INTO ""FinancialPeriods"" (""Tin"", ""Json"", ""ReceivedAt"") 
            VALUES (@Tin, @Json, @ReceivedAt)
            ON CONFLICT (""Tin"") 
            DO UPDATE SET 
            ""Json"" = EXCLUDED.""Json"", 
            ""ReceivedAt"" = EXCLUDED.""ReceivedAt""";
    private readonly string _insertFinancialReports =
            @"INSERT INTO ""FinancialReports"" (""Tin"", ""Json"", ""ReceivedAt"") 
            VALUES (@Tin, @Json, @ReceivedAt)
            ON CONFLICT (""Tin"") 
            DO UPDATE SET 
            ""Json"" = EXCLUDED.""Json"", 
            ""ReceivedAt"" = EXCLUDED.""ReceivedAt""";

    public Task<ReputationApiResponse?> GetAsync<T>(long tin, CancellationToken ct)
    {
        string sql;
        var type = typeof(T);

        // LegalEntitiesInfo table
        if (type == typeof(LegalEntitiesInfo)) sql = _selectLegalEntitiesInfo;        
        // ProceedingsInfo table
        else if (type == typeof(ProceedingsInfo)) sql = _selectProceedingsInfo;        
        // FinancialPeriods table
        else if (type == typeof(ICollection<ReportPeriod_DA_FinancialReports>)) sql = _selectFinancialPeriods;
        // FinancialReports table
        else if (type == typeof(FinancialReports)) sql = _selectFinancialReports;
        // 
        else throw new NotSupportedException(typeof(T).Name);
        
        return connection.QueryFirstOrDefaultAsync<ReputationApiResponse>(sql, new { Tin = tin });
    }

    public Task UpsertAsync<T>(long tin, T response, CancellationToken ct) where T : notnull
    {
        var serialized = ReputationApiResponse.Create(tin, response);

        var sql = response switch
        {
            LegalEntitiesInfo _ => _insertLegalEntities,
            ProceedingsInfo _ => _insertProceedings,
            ICollection<ReportPeriod_DA_FinancialReports> _ => _insertFinancialPeriods,
            FinancialReports _ => _insertFinancialReports,
            _ => throw new NotSupportedException(response.GetType().FullName)
        };
        return connection.ExecuteAsync(sql, serialized);
    }
}
