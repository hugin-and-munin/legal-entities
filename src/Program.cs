using LegalEntities;
using LegalEntities.Database;
using LegalEntities.Reputation;

var builder = WebApplication.CreateBuilder(args);

// Add services to the container.
builder.Services.AddHttpClient<ReputationApi>();
builder.Services.AddSingleton<IReputationApi, ReputationApi>();
builder.Services.AddSingleton<IRepository, Repository>();
builder.Services.AddGrpc();
builder.Services.AddOptions<AppOptions>()
    .Bind(builder.Configuration.GetSection(AppOptions.Name))
    .ValidateDataAnnotations();

var app = builder.Build();

app.MapGrpcService<LegalEntities.LegalEntityChecker>();
app.MapGet("/", () => "Communication with gRPC endpoints must be made through a gRPC client.");

app.Run();
