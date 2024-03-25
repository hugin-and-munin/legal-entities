using LegalEntities;
using LegalEntities.Database;
using LegalEntities.Database.Migrator;
using LegalEntities.Reputation;
using Microsoft.AspNetCore.Server.Kestrel.Core;

var builder = WebApplication.CreateBuilder(args);

builder.WebHost.ConfigureKestrel(kestrel =>
    kestrel.ConfigureEndpointDefaults(listen => listen.Protocols = HttpProtocols.Http2));

builder.Services.AddHttpClient<ReputationApi>();
builder.Services.AddMemoryCache();
builder.Services.AddSingleton<IReputationApi, ReputationApi>();
builder.Services.AddSingleton<IRepository, Repository>();
builder.Services.AddSingleton<MigrationRunner>();
builder.Services.AddGrpc();
builder.Services.AddOptions<AppOptions>()
    .Bind(builder.Configuration.GetSection(AppOptions.Name))
    .ValidateDataAnnotations();

var app = builder.Build();

app.MapGrpcService<LegalEntities.LegalEntityChecker>();
app.MapGrpcService<HealthCheck>();

app.Services.GetRequiredService<MigrationRunner>().MigrateUp();

await app.RunAsync();
