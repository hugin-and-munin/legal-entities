using LegalEntities.Database;
using LegalEntities.Database.Migrator;
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

    public static IRequestBuilder UnexistingEntitiesIdRequest => Request.Create()
        .WithPath("/api/v1/Entities/id")
        .WithParam("Inn", "123")
        .UsingGet();

    public static IResponseBuilder UnexistingEntitiesIdReponse => Response.Create()
        .WithStatusCode(200)
        .WithHeader("Content-Type", "application/json")
        .WithBody(@"{""Items"":[]}");

    public static IRequestBuilder YandexEntitiesIdRequest => Request.Create()
        .WithPath("/api/v1/Entities/id")
        .WithParam("Inn", "7704414297")
        .UsingGet();

    public static IRequestBuilder YandexEntitiesCompanyRequest => Request.Create()
        .WithPath("/api/v1/Entities/Company")
        .WithParam("Id", "44ec600e-2ae1-4823-b4b6-d53da6b63a6a")
        .UsingGet();

    public static IRequestBuilder YandexProceedingsInfoRequest => Request.Create()
        .WithPath("/api/v1/fssp/proceedings")
        .WithParam("EntityId", "44ec600e-2ae1-4823-b4b6-d53da6b63a6a")
        .WithParam("EntityType", "Company")
        .UsingGet();

    public static IRequestBuilder YandexFinancePeriodsRequest => Request.Create()
        .WithPath("/api/v2/finance/periods")
        .WithParam("EntityId", "44ec600e-2ae1-4823-b4b6-d53da6b63a6a")
        .WithParam("EntityType", "Company")
        .UsingGet();

    public static IRequestBuilder YandexFinanceValuesRequest => Request.Create()
        .WithPath("/api/v2/finance/values")
        .WithParam("Year", "2022")
        .WithParam("EntityId", "44ec600e-2ae1-4823-b4b6-d53da6b63a6a")
        .UsingGet();

    public static IRequestBuilder SvyaznoyEntitiesIdRequest => Request.Create()
        .WithPath("/api/v1/Entities/id")
        .WithParam("Inn", "7714617793")
        .UsingGet();

    public static IRequestBuilder SvyaznoyEntitiesCompanyRequest => Request.Create()
        .WithPath("/api/v1/Entities/Company")
        .WithParam("Id", "f5148e3e-4e8c-4a82-b1d1-5c7d001e9e0f")
        .UsingGet();

    public static IRequestBuilder SvyaznoyProceedingsInfoRequest => Request.Create()
        .WithPath("/api/v1/fssp/proceedings")
        .WithParam("EntityId", "f5148e3e-4e8c-4a82-b1d1-5c7d001e9e0f")
        .WithParam("EntityType", "Company")
        .UsingGet();

    public static IRequestBuilder SvyaznoyFinancePeriodsRequest => Request.Create()
        .WithPath("/api/v2/finance/periods")
        .WithParam("EntityId", "f5148e3e-4e8c-4a82-b1d1-5c7d001e9e0f")
        .WithParam("EntityType", "Company")
        .UsingGet();

    public static IRequestBuilder SvyaznoyFinanceValuesRequest => Request.Create()
        .WithPath("/api/v2/finance/values")
        .WithParam("Year", "2022")
        .WithParam("EntityId", "f5148e3e-4e8c-4a82-b1d1-5c7d001e9e0f")
        .UsingGet();

    public static IRequestBuilder OzonEntitiesIdRequest => Request.Create()
        .WithPath("/api/v1/Entities/id")
        .WithParam("Inn", "7703475603")
        .UsingGet();

    public static IRequestBuilder OzonEntitiesCompanyRequest => Request.Create()
        .WithPath("/api/v1/Entities/Company")
        .WithParam("Id", "723a3831-2e9c-4f27-8928-a1d07b0edf54")
        .UsingGet();

    public static IRequestBuilder OzonProceedingsInfoRequest => Request.Create()
        .WithPath("/api/v1/fssp/proceedings")
        .WithParam("EntityId", "723a3831-2e9c-4f27-8928-a1d07b0edf54")
        .WithParam("EntityType", "Company")
        .UsingGet();

    public static IRequestBuilder OzonFinancePeriodsRequest => Request.Create()
        .WithPath("/api/v2/finance/periods")
        .WithParam("EntityId", "723a3831-2e9c-4f27-8928-a1d07b0edf54")
        .WithParam("EntityType", "Company")
        .UsingGet();

    public static IRequestBuilder OzonFinanceValuesRequest => Request.Create()
        .WithPath("/api/v2/finance/values")
        .WithParam("Year", "2022")
        .WithParam("EntityId", "723a3831-2e9c-4f27-8928-a1d07b0edf54")
        .UsingGet();

    public static Repository GetRepository(this PostgreSqlContainer postgres)
    {
        var connectionString = postgres.GetConnectionString();
        var options = GetOptions(connectionString: connectionString);
        var migrationRunner = new MigrationRunner(options);
        migrationRunner.MigrateUp();
        return new Repository(options);
    }

    public static (WireMockServer MockServer, ReputationApiClient ApiClient) GetApiClient()
    {
        var mockServer = WireMockServer.Start();

        mockServer.Given(UnexistingEntitiesIdRequest).RespondWith(UnexistingEntitiesIdReponse);

        // Яндекс Технологии
        mockServer
            .AddRequestAndResponse(YandexEntitiesIdRequest, "./Samples/api-v1-entities-id-yandex.json")
            .AddRequestAndResponse(YandexEntitiesCompanyRequest, "./Samples/api-v1-entities-company-yandex.json")
            .AddRequestAndResponse(YandexProceedingsInfoRequest, "./Samples/api-v1-fssp-proceedings-yandex.json")
            .AddRequestAndResponse(YandexFinancePeriodsRequest, "./Samples/api-v1-finance-periods-yandex.json")
            .AddRequestAndResponse(YandexFinanceValuesRequest, "./Samples/api-v1-finance-values-yandex.json");

        // Сеть Связной
        mockServer
            .AddRequestAndResponse(SvyaznoyEntitiesIdRequest, "./Samples/api-v1-entities-id-svyaznoy.json")
            .AddRequestAndResponse(SvyaznoyEntitiesCompanyRequest, "./Samples/api-v1-entities-company-svyaznoy.json")
            .AddRequestAndResponse(SvyaznoyProceedingsInfoRequest, "./Samples/api-v1-fssp-proceedings-svyaznoy.json")
            .AddRequestAndResponse(SvyaznoyFinancePeriodsRequest, "./Samples/api-v1-finance-periods-svyaznoy.json")
            .AddRequestAndResponse(SvyaznoyFinanceValuesRequest, "./Samples/api-v1-finance-values-svyaznoy.json");

        // Ozon Tech
        mockServer
            .AddRequestAndResponse(OzonEntitiesIdRequest, "./Samples/api-v1-entities-id-ozon.json")
            .AddRequestAndResponse(OzonEntitiesCompanyRequest, "./Samples/api-v1-entities-company-ozon.json")
            .AddRequestAndResponse(OzonProceedingsInfoRequest, "./Samples/api-v1-fssp-proceedings-ozon.json")
            .AddRequestAndResponse(OzonFinancePeriodsRequest, "./Samples/api-v1-finance-periods-ozon.json")
            .AddRequestAndResponse(OzonFinanceValuesRequest, "./Samples/api-v1-finance-values-ozon.json");

        var httpClient = mockServer.CreateClient();
        httpClient.DefaultRequestHeaders.Add("Authorization", "some-api-key");

        var reputationApiClient = new ReputationApiClient(httpClient.BaseAddress?.AbsoluteUri, httpClient);

        return (mockServer, reputationApiClient);
    }

    private static WireMockServer AddRequestAndResponse(this WireMockServer server, IRequestBuilder request, string file)
    {
        var body = File.OpenText(file).ReadToEnd();

        var response = Response.Create()
            .WithStatusCode(200)
            .WithHeader("Content-Type", "application/json")
            .WithBody(body);

        server.Given(request).RespondWith(response);
        return server;
    }

    public static Mock<IRepository> GetRepositoryMock(DateTimeOffset? receivedAt = null)
    {
        var repository = new Mock<IRepository>();
        receivedAt ??= DateTimeOffset.UtcNow;
        
        repository
            .Setup(x => x.GetAsync<Company_DA_Entities>(7703475603, CancellationToken.None))
            .Returns(Task.FromResult<ReputationApiResponse?>(new ReputationApiResponse()
            {
                Tin = 7703475603,
                Json = File.OpenText("./Samples/Company_DA_Entities.json").ReadToEnd(),
                ReceivedAt = receivedAt.Value
            }));
        repository
            .Setup(x => x.GetAsync<CollectionContainerWithAggregations_DA_Proceeding_DA_Fssp_SumAggregationItem_DA_SumAggregationItem>(7703475603, CancellationToken.None))
            .Returns(Task.FromResult<ReputationApiResponse?>(new ReputationApiResponse()
            {
                Tin = 7703475603,
                Json = File.OpenText("./Samples/CollectionContainerWithAggregations_DA_Proceeding_DA_Fssp_SumAggregationItem_DA_SumAggregationItem.json").ReadToEnd(),
                ReceivedAt = receivedAt.Value
            }));
        repository
            .Setup(x => x.GetAsync<ICollection<ReportPeriod_DA_FinancialReports>>(7703475603, CancellationToken.None))
            .Returns(Task.FromResult<ReputationApiResponse?>(new ReputationApiResponse()
            {
                Tin = 7703475603,
                Json = File.OpenText("./Samples/ReportPeriod_DA_FinancialReports.json").ReadToEnd(),
                ReceivedAt = receivedAt.Value
            }));
        repository
            .Setup(x => x.GetAsync<FinancialCalculation_DA_FinancialReports>(7703475603, CancellationToken.None))
            .Returns(Task.FromResult<ReputationApiResponse?>(new ReputationApiResponse()
            {
                Tin = 7703475603,
                Json = File.OpenText("./Samples/FinancialCalculation_DA_FinancialReports.json").ReadToEnd(),
                ReceivedAt = receivedAt.Value
            }));
        
        return repository;
    }

    public static LegalEntityInfo YandexInfo => new()
    {
        BasicInfo = YandexBasicInfo,
        ProceedingsInfo = YandexProceedingsInfo,
        FinanceInfo = YandexFinanceInfo,
    };

    public static LegalEntityInfo SvyaznoyInfo => new()
    {
        BasicInfo = SvyaznoyBasicInfo,
        ProceedingsInfo = SvyaznoyProceedingsInfo,
        FinanceInfo = SvyaznoyFinanceInfo,
    };

    public static LegalEntityInfo OzonInfo => new()
    {
        BasicInfo = OzonBasicInfo,
        ProceedingsInfo = OzonProceedingsInfo,
        FinanceInfo = OzonFinanceInfo,
    };

    public static BasicInfo YandexBasicInfo
    {
        get
        {
            var result = new BasicInfo()
            {
                Name = "ООО \"ЯНДЕКС.ТЕХНОЛОГИИ\"",
                Tin = 7704414297,
                IncorporationDate = new DateTimeOffset(new DateTime(2017, 05, 19)).ToUnixTimeSeconds(),
                AuthorizedCapital = 60000000,
                EmployeesNumber = -1,
                Address = "119021,  Г.Москва, УЛ. ЛЬВА ТОЛСТОГО, Д. 16",
                LegalEntityStatus = LegalEntityStatus.Active,
                Manager = new Manager()
                {
                    Name = "МАСЮК ДМИТРИЙ ВИКТОРОВИЧ",
                    Position = "ГЕНЕРАЛЬНЫЙ ДИРЕКТОР",
                    Tin = 770373093393,
                }
            };

            result.Shareholders.Add(new Shareholder()
            {
                Name = "ПУБЛИЧНАЯ КОМПАНИЯ С ОГРАНИЧЕННОЙ ОТВЕТСТВЕННОСТЬЮ \"ЯНДЕКС Н.В.\"",
                Share = 60000000,
                Size = 100,
                Tin = -1,
                Type = EntityType.ForeignCompany
            });

            return result;
        }
    }

    public static ProceedingsInfo YandexProceedingsInfo
    {
        get
        {
            var result = new ProceedingsInfo();
            return result;
        }
    }

    public static FinanceInfo YandexFinanceInfo
    {
        get
        {
            var result = new FinanceInfo()
            {
                Year = 2022,

                Income = 50_864_263_000,
                Profit = 332_332_000,

                AccountsReceivable = 8_522_467_000,

                CapitalAndReserves = 5_246_158_000,
                LongTermLiabilities = 0,
                CurrentLiabilities = 8_222_734_000,

                PaidSalary = -34914825000
            };

            return result;
        }
    }

    public static BasicInfo OzonBasicInfo
    {
        get
        {
            var result = new BasicInfo()
            {
                Name = "ООО \"ОЗОН ТЕХНОЛОГИИ\"",
                Tin = 7703475603,
                IncorporationDate = new DateTimeOffset(new DateTime(2019, 05, 13)).ToUnixTimeSeconds(),
                AuthorizedCapital = 10_000_000,
                EmployeesNumber = 4641,
                Address = "123112,  Г.МОСКВА, НАБ. ПРЕСНЕНСКАЯ, Д. 10, ПОМЕЩ. I, ЭТАЖ 41, КОМН. 7",
                LegalEntityStatus = LegalEntityStatus.Active,
                Manager = new Manager()
                {
                    Name = "ДЬЯЧЕНКО ВАЛЕРИЙ ВАЛЕРЬЕВИЧ",
                    Position = "ГЕНЕРАЛЬНЫЙ ДИРЕКТОР",
                    Tin = 501202997792,
                }
            };

            result.Shareholders.Add(new Shareholder()
            {
                Name = "ООО \"ОЗОН ХОЛДИНГ\"",
                Share = 9_900_000,
                Size = 99,
                Tin = 7743181857,
                Type = EntityType.Company
            });

            result.Shareholders.Add(new Shareholder()
            {
                Name = "ООО \"ИНТЕРНЕТ РЕШЕНИЯ\"",
                Share = 100_000,
                Size = 1,
                Tin = 7704217370,
                Type = EntityType.Company
            });

            return result;
        }
    }

    public static ProceedingsInfo OzonProceedingsInfo
    {
        get
        {
            var result = new ProceedingsInfo();

            return result;
        }
    }

    public static FinanceInfo OzonFinanceInfo
    {
        get
        {
            var result = new FinanceInfo()
            {
                Year = 2022,

                Income = 18_646_681_000,
                Profit = 629_831_000,

                AccountsReceivable = 3_034_497_000,

                CapitalAndReserves = 630_971_000,
                LongTermLiabilities = 11_151_000,
                CurrentLiabilities = 3_276_394_000,

                PaidSalary = 15_839_326_000
            };

            return result;
        }
    }

    public static BasicInfo SvyaznoyBasicInfo
    {
        get
        {
            var result = new BasicInfo()
            {
                Name = "ООО \"СЕТЬ СВЯЗНОЙ\"",
                Tin = 7714617793,
                IncorporationDate = new DateTimeOffset(new DateTime(2005, 09, 20)).ToUnixTimeSeconds(),
                AuthorizedCapital = 32143400,
                EmployeesNumber = -1,
                Address = "123007,  Г.Москва, ПР-Д 2-Й ХОРОШЁВСКИЙ, Д. 9, К. 2, ЭТАЖ 5 КОМН 4",
                LegalEntityStatus = LegalEntityStatus.InTerminationProcess,
                Manager = new Manager()
                {
                    Name = "АНГЕЛЕВСКИ ФИЛИПП МИТРЕВИЧ",
                    Position = "КОНКУРСНЫЙ УПРАВЛЯЮЩИЙ",
                    Tin = 231906423308
                }
            };

            result.Shareholders.Add(new Shareholder()
            {
                Name = "ДТСРЕТЕЙЛ ЛТД",
                Share = 22258645,
                Size = 69.25,
                Tin = -1,
                Type = EntityType.ForeignCompany
            });

            result.Shareholders.Add(new Shareholder()
            {
                Name = "АО \"ГРУППА КОМПАНИЙ \"СВЯЗНОЙ\"",
                Share = 1804755,
                Size = 5.61,
                Tin = 7703534714,
                Type = EntityType.Company
            });

            result.Shareholders.Add(new Shareholder()
            {
                Name = "СИННАМОН ШОР ЛТД.",
                Share = 80800,
                Size = 0.25,
                Tin = -1,
                Type = EntityType.ForeignCompany
            });

            result.Shareholders.Add(new Shareholder()
            {
                Name = "ЕВРОСЕТЬ ХОЛДИНГ Н.В.",
                Share = 7999200,
                Size = 24.89,
                Tin = -1,
                Type = EntityType.ForeignCompany
            });

            return result;
        }
    }

    public static ProceedingsInfo SvyaznoyProceedingsInfo
    {
        get
        {
            var result = new ProceedingsInfo()
            {
                Amount = 1843596.67,
                Count = 21,
                Description = "Оплата труда и иные выплаты по трудовым правоотношениям"
            };

            return result;
        }
    }

    public static FinanceInfo SvyaznoyFinanceInfo
    {
        get
        {
            var result = new FinanceInfo()
            {
                Year = 2022,

                Income = 56_759_628_000,
                Profit = -48_544_335_000,

                AccountsReceivable = 4_243_988_000,

                CapitalAndReserves = -44_883_153_000,
                LongTermLiabilities = 12_450_192_000,
                CurrentLiabilities = 54_133_768_000,

                PaidSalary = -6_302_428_000
            };

            return result;
        }
    }
}