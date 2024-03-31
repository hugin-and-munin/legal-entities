using Dapper;
using Microsoft.Extensions.Options;
using Npgsql;
using ProceedingsInfo = LegalEntities.CollectionContainerWithAggregations_DA_Proceeding_DA_Fssp_SumAggregationItem_DA_SumAggregationItem;

namespace LegalEntities.Database;

public class Repository(IOptions<AppOptions> _options) : IRepository
{
    private readonly NpgsqlConnection connection = new(_options.Value.DbConnectionString);

    public Task<ReputationApiResponse?> GetAsync<T>(long tin, CancellationToken ct)
    {
        if (typeof(T) == typeof(Company_DA_Entities)) return GetLegalEntitiesInfo(tin, ct);
        if (typeof(T) == typeof(ProceedingsInfo)) return GetProceedingsInfo(tin, ct);
        if (typeof(T) == typeof(ProceedingsInfo)) return GetProceedingsInfo(tin, ct);

        throw new NotSupportedException(typeof(T).Name);
    }

    private async Task<ReputationApiResponse?> GetProceedingsInfo(long tin, CancellationToken ct)
    {
        var sql =
            @"SELECT ""Tin"", ""Json"", ""ReceivedAt""
            FROM ""Proceedings"" WHERE ""Tin"" = @Tin";
        return await connection.QueryFirstOrDefaultAsync<ReputationApiResponse>(sql, new { Tin = tin });
    }

    private async Task<ReputationApiResponse?> GetLegalEntitiesInfo(long tin, CancellationToken ct)
    {
        var sql =
            @"SELECT ""Tin"", ""Json"", ""ReceivedAt""
            FROM ""LegalEntities"" WHERE ""Tin"" = @Tin";
        return await connection.QueryFirstOrDefaultAsync<ReputationApiResponse>(sql, new { Tin = tin });
    }

    public Task UpsertAsync<T>(long tin, T response, CancellationToken ct)
    {
        var serialized = ReputationApiResponse.Create(tin, response);

        if (typeof(T) == typeof(Company_DA_Entities)) return UpsertBasicInfo(serialized, ct);
        if (typeof(T) == typeof(ProceedingsInfo)) return UpsertProceedingsInfo(serialized, ct);
        if (typeof(T) == typeof(ProceedingsInfo)) return UpsertProceedingsInfo(serialized, ct);

        throw new NotSupportedException(typeof(T).Name);
    }

    private async Task UpsertProceedingsInfo(ReputationApiResponse response, CancellationToken ct)
    {
        var sql =
            @"INSERT INTO ""Proceedings"" (""Tin"", ""Json"", ""ReceivedAt"") 
            VALUES (@Tin, @Json, @ReceivedAt)
            ON CONFLICT (""Tin"") 
            DO UPDATE SET 
            ""Json"" = EXCLUDED.""Json"", 
            ""ReceivedAt"" = EXCLUDED.""ReceivedAt""";
        await connection.ExecuteAsync(sql, response);
    }

    private async Task UpsertBasicInfo(ReputationApiResponse response, CancellationToken ct)
    {
        var sql =
            @"INSERT INTO ""LegalEntities"" (""Tin"", ""Json"", ""ReceivedAt"") 
            VALUES (@Tin, @Json, @ReceivedAt)
            ON CONFLICT (""Tin"") 
            DO UPDATE SET 
            ""Json"" = EXCLUDED.""Json"", 
            ""ReceivedAt"" = EXCLUDED.""ReceivedAt""";
        await connection.ExecuteAsync(sql, response);
    }
}

