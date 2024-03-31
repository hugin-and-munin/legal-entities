using LegalEntities;
using LegalEntities.Database;
using LegalEntities.Database.Migrator;
using Microsoft.AspNetCore.Server.Kestrel.Core;
using Microsoft.Extensions.Options;

var builder = WebApplication.CreateBuilder(args);

builder.WebHost.ConfigureKestrel(kestrel =>
    kestrel.ConfigureEndpointDefaults(listen => listen.Protocols = HttpProtocols.Http2));

builder.Services.AddHttpClient<LegalEntities.LegalEntityChecker>();
builder.Services.AddMemoryCache();
builder.Services.AddSingleton<IRepository, Repository>();
builder.Services.AddSingleton<MigrationRunner>();
builder.Services.AddSingleton(provider =>
{
    var httpClient = provider.GetRequiredService<HttpClient>();
    var appOptions = provider.GetRequiredService<IOptions<AppOptions>>();
    httpClient.DefaultRequestHeaders.Add("Authorization", appOptions.Value.ApiKey);
    return new ReputationApiClient(appOptions.Value.ApiBase, httpClient);
});
builder.Services.AddGrpc();
builder.Services.AddOptions<AppOptions>()
    .Bind(builder.Configuration.GetSection(AppOptions.Name))
    .ValidateDataAnnotations();

var app = builder.Build();
System.Buffers.ArrayPool<int>.Shared.Rent(12);

app.MapGrpcService<LegalEntities.LegalEntityChecker>();
app.MapGrpcService<HealthCheck>();

app.Services.GetRequiredService<MigrationRunner>().MigrateUp();

await app.RunAsync();
