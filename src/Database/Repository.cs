using LegalEntities.Reputation;
using Dapper;
using Microsoft.Extensions.Options;
using Npgsql;

namespace LegalEntities.Database;

public class Repository(IOptions<AppOptions> _options) : IRepository
{
    private readonly NpgsqlConnection connection = new(_options.Value.DbConnectionString);
    private readonly string _selectQuery =
        @"SELECT ""Tin"", ""Json"", ""ReceivedAt""
        FROM ""LegalEntities"" WHERE ""Tin"" = @Tin";
        
    private readonly string _upsertQuery = 
        @"INSERT INTO ""LegalEntities"" (""Tin"", ""Json"", ""ReceivedAt"") 
        VALUES (@Tin, @Json, @ReceivedAt)
        ON CONFLICT (""Tin"") 
        DO UPDATE SET 
        ""Json"" = EXCLUDED.""Json"", 
        ""ReceivedAt"" = EXCLUDED.""ReceivedAt""";

    public async Task<ReputationApiResponse?> GetReputationResponse(long tin, CancellationToken ct)
    {
        return await connection.QueryFirstOrDefaultAsync<ReputationApiResponse>(_selectQuery, new { Tin = tin });
    }

    public async Task UpsertReputationResponse(ReputationApiResponse response, CancellationToken ct)
    {
        await connection.ExecuteAsync(_upsertQuery, response);
    }
}

