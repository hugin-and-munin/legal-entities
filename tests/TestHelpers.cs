using LegalEntities.Database;
using LegalEntities.Migrator;
using LegalEntities.Reputation;
using LegalEntityChecker;
using Microsoft.Extensions.Options;
using Testcontainers.PostgreSql;
using WireMock.RequestBuilders;
using WireMock.ResponseBuilders;
using WireMock.Server;

namespace LegalEntities.Tests;

public static class TestHelpers
{
    private static IOptions<AppOptions> GetOptions(
        string? apiBase = null,
        string? apiKey = null,
        string? connectionString = null) => Options.Create(new AppOptions()
        {
            ApiBase = apiBase ?? string.Empty,
            ApiKey = apiKey ?? string.Empty,
            DbConnectionString = connectionString ?? string.Empty
        });

    public static PostgreSqlContainer GetPostgres() => new PostgreSqlBuilder()
        .WithImage("postgres:16-alpine")
        .Build();

    public static IRequestBuilder GetIdRequest(string id) => Request.Create()
        .WithPath("/api/v1/Entities/id")
        .WithParam("Inn", id)
        .UsingGet();

    public static IRequestBuilder GetCompanyRequest() => Request.Create()
        .WithPath("/api/v1/Entities/Company")
        .WithParam("Id", "44ec600e-2ae1-4823-b4b6-d53da6b63a6a")
        .UsingGet();

    public static Repository GetRepository(this PostgreSqlContainer postgres)
    {
        var connectionString = postgres.GetConnectionString();
        MigrationRunner.MigrateUp(connectionString);
        var options = GetOptions(connectionString: connectionString);
        return new Repository(options);
    }

    public static (WireMockServer, ReputationApi) GetReputationApi(IRepository repository)
    {
        var bodyCompanyId = File.OpenText("./Samples/api-v1-entities-id.json").ReadToEnd();
        var bodyCompanyInfo = File.OpenText("./Samples/api-v1-entities-company.json").ReadToEnd();

        var responseCompanyId = Response.Create()
            .WithStatusCode(200)
            .WithHeader("Content-Type", "application/json")
            .WithBody(bodyCompanyId);

        var responseUnexistingCompanyId = Response.Create()
            .WithStatusCode(200)
            .WithHeader("Content-Type", "application/json")
            .WithBody(@"{""Items"":[]}");

        var responseCompanyInfo = Response.Create()
            .WithStatusCode(200)
            .WithHeader("Content-Type", "application/json")
            .WithBody(bodyCompanyInfo);

        var server = WireMockServer.Start();
        server.Given(GetIdRequest("7704414297")).RespondWith(responseCompanyId);
        server.Given(GetIdRequest("123")).RespondWith(responseUnexistingCompanyId);
        server.Given(GetCompanyRequest()).RespondWith(responseCompanyInfo);

        var client = server.CreateClient();

        var options = GetOptions(apiBase: client.BaseAddress?.AbsoluteUri, apiKey: "123");

        return (server, new(options, client, repository));
    }

    public static LegalEntityInfoReponse ExpectedLegalEntityInfoReponse => new()
    {
        Tin = 7704414297,
        Name = "ООО \"ЯНДЕКС.ТЕХНОЛОГИИ\"",
        Address = "119021,  Г.Москва, УЛ. ЛЬВА ТОЛСТОГО, Д. 16",
        AuthorizedCapital = 60000000,
        EmployeesNumber = -1,
        IncorporationDate = new DateTimeOffset(new DateTime(2017, 05, 19)).ToUnixTimeSeconds(),
        LegalEntityStatus = LegalEntityStatus.Active
    };

    public static Mock<IRepository> GetRepositoryMock(ReputationApiResponse? message = null)
    {
        var repositoryMock = new Mock<IRepository>();
        repositoryMock.Setup(x => x.GetReputationResponse(It.IsAny<long>(), CancellationToken.None))
            .Returns(Task.FromResult(message));
        return repositoryMock;
    }

    public static Mock<IReputationApi> GetReputationApiMock(LegalEntityInfoReponse? message = null)
    {
        var apiMock = new Mock<IReputationApi>();
        apiMock.Setup(x => x.Get(It.IsAny<LegalEntityInfoRequest>(), CancellationToken.None))
            .Returns(Task.FromResult(message));
        return apiMock;
    }
}