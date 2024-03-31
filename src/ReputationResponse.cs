using System.Text.Json;

namespace LegalEntities;

public record ReputationApiResponse
{
    public required long Tin { get; init; }
    public required string Json { get; init; }
    public required DateTimeOffset ReceivedAt { get; init; }

    /// <summary>
    /// The value is vaild if it not older than 30 days
    /// </summary>
    public bool IsExpired => ReceivedAt < DateTimeOffset.UtcNow - TimeSpan.FromDays(30);
    
    public T? TryDeserialze<T>() => JsonSerializer.Deserialize<T>(Json);

    public static ReputationApiResponse Create<T>(long tin, T item) => new()
    {
        Tin = tin,
        Json = JsonSerializer.Serialize(item),
        ReceivedAt = DateTimeOffset.UtcNow
    };
}