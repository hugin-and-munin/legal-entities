using LegalEntities.Reputation;
using Dapper;
using Microsoft.Extensions.Options;
using Npgsql;

namespace LegalEntities.Database;

public class Repository(IOptions<AppOptions> _options) : IRepository
{
    private readonly NpgsqlConnection connection = new(_options.Value.DbConnectionString);

    public async Task<ReputationApiResponse?> GetProceedingsInfo(long tin, CancellationToken ct)
    {
        var sql = 
            @"SELECT ""Tin"", ""Json"", ""ReceivedAt""
            FROM ""Proceedings"" WHERE ""Tin"" = @Tin";
        return await connection.QueryFirstOrDefaultAsync<ReputationApiResponse>(sql, new { Tin = tin });
    }

    public async Task<ReputationApiResponse?> GetBasicInfo(long tin, CancellationToken ct)
    {
        var sql = 
            @"SELECT ""Tin"", ""Json"", ""ReceivedAt""
            FROM ""LegalEntities"" WHERE ""Tin"" = @Tin";
        return await connection.QueryFirstOrDefaultAsync<ReputationApiResponse>(sql, new { Tin = tin });
    }

    public async Task UpsertProceedingsInfo(ReputationApiResponse response, CancellationToken ct)
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

    public async Task UpsertBasicInfo(ReputationApiResponse response, CancellationToken ct)
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

