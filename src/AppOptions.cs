using System.ComponentModel.DataAnnotations;
using System.Diagnostics.CodeAnalysis;

namespace LegalEntities;

[ExcludeFromCodeCoverage]
public record AppOptions
{
    public const string Name = "AppOptions";

    [MinLength(1)]
    public required string ApiBase { get; init; }

    [MinLength(1)]
    public required string ApiKey { get; init; }

    [MinLength(1)]
    public required string DbConnectionString { get; init; }
}
