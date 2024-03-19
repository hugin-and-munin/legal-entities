using LegalEntities.Database;
using LegalEntities.Database.Migrator;
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

    public static IRequestBuilder GetYandexBasicInfoRequest() => Request.Create()
        .WithPath("/api/v1/Entities/Company")
        .WithParam("Id", "44ec600e-2ae1-4823-b4b6-d53da6b63a6a")
        .UsingGet();

    public static IRequestBuilder GetYandexProceedingsInfoRequest() => Request.Create()
        .WithPath("/api/v1/fssp/proceedings")
        .WithParam("EntityId", "44ec600e-2ae1-4823-b4b6-d53da6b63a6a")
        .WithParam("EntityType", "Company")
        .UsingGet();

    public static IRequestBuilder GetSvyaznoyBasicInfoRequest() => Request.Create()
        .WithPath("/api/v1/Entities/Company")
        .WithParam("Id", "f5148e3e-4e8c-4a82-b1d1-5c7d001e9e0f")
        .UsingGet();

    public static IRequestBuilder GetSvyaznoyProceedingsInfoRequest() => Request.Create()
        .WithPath("/api/v1/fssp/proceedings")
        .WithParam("EntityId", "f5148e3e-4e8c-4a82-b1d1-5c7d001e9e0f")
        .WithParam("EntityType", "Company")
        .UsingGet();

    public static Repository GetRepository(this PostgreSqlContainer postgres)
    {
        var connectionString = postgres.GetConnectionString();
        var options = GetOptions(connectionString: connectionString);
        var migrationRunner = new MigrationRunner(options);
        migrationRunner.MigrateUp();
        return new Repository(options);
    }

    public static (WireMockServer, ReputationApi) GetReputationApi(IRepository repository)
    {
        var server = WireMockServer.Start();

        // Unexisting company
        var responseUnexistingCompanyId = Response.Create()
            .WithStatusCode(200)
            .WithHeader("Content-Type", "application/json")
            .WithBody(@"{""Items"":[]}");

        server.Given(GetIdRequest("123")).RespondWith(responseUnexistingCompanyId);

        // Яндекс Технологии
        var bodyYandexCompanyId = File.OpenText("./Samples/api-v1-entities-id-yandex.json").ReadToEnd();
        var bodyYandexCompanyInfo = File.OpenText("./Samples/api-v1-entities-company-yandex.json").ReadToEnd();
        var bodyYandexProceedingsInfo = File.OpenText("./Samples/api-v1-fssp-proceedings-yandex.json").ReadToEnd();

        var responseYandexCompanyId = Response.Create()
            .WithStatusCode(200)
            .WithHeader("Content-Type", "application/json")
            .WithBody(bodyYandexCompanyId);

        var responseYandexCompanyInfo = Response.Create()
            .WithStatusCode(200)
            .WithHeader("Content-Type", "application/json")
            .WithBody(bodyYandexCompanyInfo);

        var responseYandexProceedingsInfo = Response.Create()
            .WithStatusCode(200)
            .WithHeader("Content-Type", "application/json")
            .WithBody(bodyYandexProceedingsInfo);

        server.Given(GetIdRequest("7704414297")).RespondWith(responseYandexCompanyId);
        server.Given(GetYandexBasicInfoRequest()).RespondWith(responseYandexCompanyInfo);
        server.Given(GetYandexProceedingsInfoRequest()).RespondWith(responseYandexProceedingsInfo);

        // Сеть Связной
        var bodySvyaznoyCompanyId = File.OpenText("./Samples/api-v1-entities-id-svyaznoy.json").ReadToEnd();
        var bodySvyaznoyCompanyInfo = File.OpenText("./Samples/api-v1-entities-company-svyaznoy.json").ReadToEnd();
        var bodySvyaznoyProceedingsInfo = File.OpenText("./Samples/api-v1-fssp-proceedings-svyaznoy.json").ReadToEnd();

        var responseSvyaznoyCompanyId = Response.Create()
            .WithStatusCode(200)
            .WithHeader("Content-Type", "application/json")
            .WithBody(bodySvyaznoyCompanyId);

        var responseSvyaznoyCompanyInfo = Response.Create()
            .WithStatusCode(200)
            .WithHeader("Content-Type", "application/json")
            .WithBody(bodySvyaznoyCompanyInfo);

        var responseSvyaznoyProceedingsInfo = Response.Create()
            .WithStatusCode(200)
            .WithHeader("Content-Type", "application/json")
            .WithBody(bodySvyaznoyProceedingsInfo);

        server.Given(GetIdRequest("7714617793")).RespondWith(responseSvyaznoyCompanyId);
        server.Given(GetSvyaznoyBasicInfoRequest()).RespondWith(responseSvyaznoyCompanyInfo);
        server.Given(GetSvyaznoyProceedingsInfoRequest()).RespondWith(responseSvyaznoyProceedingsInfo);

        var client = server.CreateClient();

        var options = GetOptions(apiBase: client.BaseAddress?.AbsoluteUri, apiKey: "123");

        return (server, new(options, client, repository));
    }

    public static LegalEntityInfoReponse YandexInfo => new()
    {
        Tin = 7704414297,
        Name = "ООО \"ЯНДЕКС.ТЕХНОЛОГИИ\"",
        Address = "119021,  Г.Москва, УЛ. ЛЬВА ТОЛСТОГО, Д. 16",
        AuthorizedCapital = 60000000,
        EmployeesNumber = -1,
        IncorporationDate = new DateTimeOffset(new DateTime(2017, 05, 19)).ToUnixTimeSeconds(),
        LegalEntityStatus = LegalEntityStatus.Active,
        SalaryDelays = false
    };

    public static LegalEntityInfoReponse SvyaznoyInfo => new()
    {
        Tin = 7714617793,
        Name = "ООО \"СЕТЬ СВЯЗНОЙ\"",
        Address = "123007,  Г.Москва, ПР-Д 2-Й ХОРОШЁВСКИЙ, Д. 9, К. 2, ЭТАЖ 5 КОМН 4",
        AuthorizedCapital = 32143400,
        EmployeesNumber = -1,
        IncorporationDate = new DateTimeOffset(new DateTime(2005, 09, 20)).ToUnixTimeSeconds(),
        LegalEntityStatus = LegalEntityStatus.InTerminationProcess,
        SalaryDelays = true
    };

    public static Mock<IRepository> GetRepositoryMock(ReputationApiResponse? message = null)
    {
        var repositoryMock = new Mock<IRepository>();
        repositoryMock.Setup(x => x.GetBasicInfo(It.IsAny<long>(), CancellationToken.None))
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