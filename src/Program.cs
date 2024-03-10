using LegalEntities;
using LegalEntities.Database;
using LegalEntities.Reputation;
using Microsoft.AspNetCore.Server.Kestrel.Core;

var builder = WebApplication.CreateBuilder(args);

builder.WebHost.ConfigureKestrel(kestrel =>
    kestrel.ConfigureEndpointDefaults(listen => listen.Protocols = HttpProtocols.Http2));

builder.Services.AddHttpClient<ReputationApi>();
builder.Services.AddHealthChecks().AddCheck<HealthCheck>("Health");
builder.Services.AddSingleton<IReputationApi, ReputationApi>();
builder.Services.AddSingleton<IRepository, Repository>();
builder.Services.AddGrpc();
builder.Services.AddOptions<AppOptions>()
    .Bind(builder.Configuration.GetSection(AppOptions.Name))
    .ValidateDataAnnotations();

var app = builder.Build();

app.MapGrpcService<LegalEntities.LegalEntityChecker>();
app.MapHealthChecks("/health");

await app.RunAsync();
