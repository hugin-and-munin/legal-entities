namespace LegalEntities.Database;

public interface IRepository
{
    public Task<ReputationApiResponse?> GetAsync<T>(long tin, CancellationToken ct);
    public Task UpsertAsync<T>(long tin, T response, CancellationToken ct) where T : notnull;
}

