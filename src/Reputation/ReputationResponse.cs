namespace LegalEntities.Reputation;

public record ReputationApiResponse
{
    public required long Tin { get; init; }
    public required string Json { get; init; }
    public required DateTimeOffset ReceivedAt { get; init; }
}