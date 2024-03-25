using Grpc.Core;
using LegalEntityChecker;
using WireMock.RequestBuilders;

namespace LegalEntities.Tests;

[TestClass]
public class LegalEntitiesInfoService
{
    [TestMethod]
    public async Task RequestOfUnknownCompanyWorksOk()
    {
        // Arrange
        var postgres = TestHelpers.GetPostgres();
        await postgres.StartAsync();
        var repository = postgres.GetRepository();
        var (server, api) = TestHelpers.GetReputationApi(repository);
        var tin = 123; // unknown tin
        var request = new LegalEntityInfoRequest() { Tin = tin };
        var memoryCache = TestHelpers.GetMemoryCache();
        var sut = new LegalEntityChecker(api, memoryCache);

        // Act
        var actual = await sut.GetBasicInfo(request, Mock.Of<ServerCallContext>());

        // Assert
        actual.Should().BeNull();
        server.FindLogEntries(TestHelpers.GetIdRequest($"{tin}")).Count().Should().Be(2);
        server.FindLogEntries(TestHelpers.GetYandexBasicInfoRequest()).Count().Should().Be(0);
        server.FindLogEntries(TestHelpers.GetYandexProceedingsInfoRequest()).Count().Should().Be(0);
        await postgres.DisposeAsync().AsTask();
    }

    [DataTestMethod]
    [DynamicData(nameof(LegalEntitiesInfos))]
    public async Task RequestOfExistingCompanyWorksOk(
        BasicInfo expected,
        IRequestBuilder basicInfoRequest,
        IRequestBuilder proceedingsInfoRequest)
    {
        // Arrange
        var postgres = TestHelpers.GetPostgres();
        await postgres.StartAsync();
        var repository = postgres.GetRepository();
        var (server, api) = TestHelpers.GetReputationApi(repository);
        var tin = expected.Tin;
        var request = new LegalEntityInfoRequest() { Tin = tin };
        var memoryCache = TestHelpers.GetMemoryCache();
        var sut = new LegalEntityChecker(api, memoryCache);

        // Act
        var actual = await sut.GetBasicInfo(request, Mock.Of<ServerCallContext>());

        // Assert
        actual.Should().Be(expected);
        server.FindLogEntries(TestHelpers.GetIdRequest($"{tin}")).Count().Should().Be(2);
        server.FindLogEntries(basicInfoRequest).Count().Should().Be(1);
        server.FindLogEntries(proceedingsInfoRequest).Count().Should().Be(1);
        await postgres.DisposeAsync().AsTask();
    }

    [DataTestMethod]
    [DynamicData(nameof(LegalEntitiesInfos))]
    public async Task RequestOfExistingCompanyAfterRebootWorksOk(
        BasicInfo expected,
        IRequestBuilder basicInfoRequest,
        IRequestBuilder proceedingsInfoRequest)
    {
        // Arrange
        var postgres = TestHelpers.GetPostgres();
        await postgres.StartAsync();
        var repository = postgres.GetRepository();
        var (serverBefore, apiBefore) = TestHelpers.GetReputationApi(repository);
        var (serverAfter, apiAfter) = TestHelpers.GetReputationApi(repository);
        var tin = expected.Tin;
        var request = new LegalEntityInfoRequest() { Tin = tin };
        var memoryCacheBefore = TestHelpers.GetMemoryCache();
        var sutBefore = new LegalEntityChecker(apiBefore, memoryCacheBefore);
        var actualBefore = await sutBefore.GetBasicInfo(request, Mock.Of<ServerCallContext>());
        var memoryCacheAfter = TestHelpers.GetMemoryCache();
        var sutAfter = new LegalEntityChecker(apiAfter, memoryCacheAfter); // new instance to simulate reboot

        // Act
        var actualAfter = await sutAfter.GetBasicInfo(request, Mock.Of<ServerCallContext>());

        // Assert
        actualBefore.Should().Be(expected);
        actualAfter.Should().Be(expected);
        serverBefore.FindLogEntries(TestHelpers.GetIdRequest($"{tin}")).Count().Should().Be(2);
        serverBefore.FindLogEntries(basicInfoRequest).Count().Should().Be(1);
        serverBefore.FindLogEntries(proceedingsInfoRequest).Count().Should().Be(1);
        serverAfter.FindLogEntries(TestHelpers.GetIdRequest($"{tin}")).Count().Should().Be(0);
        serverAfter.FindLogEntries(basicInfoRequest).Count().Should().Be(0);
        serverAfter.FindLogEntries(proceedingsInfoRequest).Count().Should().Be(0);
        await postgres.DisposeAsync().AsTask();
    }

    [DataTestMethod]
    [DynamicData(nameof(LegalEntitiesInfos))]
    public async Task MultipleApiCallsCausesCachingInDatabase(
        BasicInfo expected,
        IRequestBuilder basicInfoRequest,
        IRequestBuilder proceedingsInfoRequest)
    {
        // Arrange
        var postgres = TestHelpers.GetPostgres();
        await postgres.StartAsync();
        var repository = postgres.GetRepository();
        var (server, api) = TestHelpers.GetReputationApi(repository);
        var tin = expected.Tin;
        var request = new LegalEntityInfoRequest() { Tin = tin };
        var memoryCache = TestHelpers.GetMemoryCache();
        var sut = new LegalEntityChecker(api, memoryCache);

        // Act
        var actual1 = await sut.GetBasicInfo(request, Mock.Of<ServerCallContext>());
        var actual2 = await sut.GetBasicInfo(request, Mock.Of<ServerCallContext>());
        var actual3 = await sut.GetBasicInfo(request, Mock.Of<ServerCallContext>());
        var actual4 = await sut.GetBasicInfo(request, Mock.Of<ServerCallContext>());

        // Assert
        actual1.Should().Be(expected);
        actual2.Should().Be(expected);
        actual3.Should().Be(expected);
        actual4.Should().Be(expected);

        // Only one call to API due to caching
        server.FindLogEntries(TestHelpers.GetIdRequest($"{tin}")).Count().Should().Be(2);
        server.FindLogEntries(basicInfoRequest).Count().Should().Be(1);
        server.FindLogEntries(proceedingsInfoRequest).Count().Should().Be(1);
        await postgres.DisposeAsync().AsTask();
    }

    public static IEnumerable<object[]> LegalEntitiesInfos =>
    [
        [
            TestHelpers.YandexBasicInfo,
            TestHelpers.GetYandexBasicInfoRequest(),
            TestHelpers.GetYandexProceedingsInfoRequest()
        ],
        [
            TestHelpers.SvyaznoyBasicInfo,
            TestHelpers.GetSvyaznoyBasicInfoRequest(),
            TestHelpers.GetSvyaznoyProceedingsInfoRequest()
        ]
    ];
}
