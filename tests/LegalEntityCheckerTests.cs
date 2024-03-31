using Grpc.Core;
using LegalEntities.Database;
using LegalEntityChecker;
using Microsoft.Extensions.Caching.Memory;
using Microsoft.Extensions.DependencyInjection;
using WireMock.RequestBuilders;
using static LegalEntities.Tests.TestHelpers;

namespace LegalEntities.Tests;

[TestClass]
public class LegalEntityCheckerTests
{
    /// <summary>
    /// Unexisting means it doesn't exist at all
    /// </summary>
    [TestMethod]
    public async Task RequestOfUnexistingCompanyReturnsNull()
    {
        // Arrange
        var (mockServer, apiClient) = GetApiClient();
        var repository = new Mock<IRepository>();
        repository
            .Setup(x => x.GetAsync<BasicInfo>(It.IsAny<long>(), CancellationToken.None))
            .Returns(Task.FromResult<ReputationApiResponse?>(null));
        var serviceCollection = new ServiceCollection();
        serviceCollection.AddMemoryCache();
        serviceCollection.AddLogging();
        serviceCollection.AddSingleton(new MemoryCacheEntryOptions() { SlidingExpiration = TimeSpan.FromHours(24) });
        serviceCollection.AddSingleton(apiClient);
        serviceCollection.AddSingleton(x => repository.Object);
        serviceCollection.AddSingleton<LegalEntityChecker>();
        var provider = serviceCollection.BuildServiceProvider();
        var sut = provider.GetRequiredService<LegalEntityChecker>();
        var request = new LegalEntityInfoRequest { Tin = 123 };

        // Act
        var actual = await sut.GetLegalEntityInfo(request, Mock.Of<ServerCallContext>());

        // Assert
        actual.Should().Be(null);
        mockServer.FindLogEntries(UnexistingEntitiesIdRequest).Count().Should().Be(1);
    }

    /// <summary>
    /// New company means it doesn't exist in memory cache and database
    /// </summary>
    [DataTestMethod]
    [DynamicData(nameof(TestData))]
    public async Task RequestOfNewCompanyCausesApiCall(
        long tin,
        LegalEntityInfo expected,
        IRequestBuilder entitiesIdRequest,
        IRequestBuilder entitiesCompanyRequest,
        IRequestBuilder proceedingsInfoRequest,
        IRequestBuilder financePeriodsRequest,
        IRequestBuilder financeValuesRequest)
    {
        // Arrange
        var (mockServer, apiClient) = GetApiClient();
        var repository = new Mock<IRepository>();
        repository
            .Setup(x => x.GetAsync<BasicInfo>(It.IsAny<long>(), CancellationToken.None))
            .Returns(Task.FromResult<ReputationApiResponse?>(null));
        var serviceCollection = new ServiceCollection();
        serviceCollection.AddMemoryCache();
        serviceCollection.AddSingleton(new MemoryCacheEntryOptions() { SlidingExpiration = TimeSpan.FromHours(24) });
        serviceCollection.AddLogging();
        serviceCollection.AddSingleton(apiClient);
        serviceCollection.AddSingleton(x => repository.Object);
        serviceCollection.AddSingleton<LegalEntityChecker>();
        var provider = serviceCollection.BuildServiceProvider();
        var sut = provider.GetRequiredService<LegalEntityChecker>();
        var request = new LegalEntityInfoRequest() { Tin = tin };

        // Act
        var actual = await sut.GetLegalEntityInfo(request, Mock.Of<ServerCallContext>());

        // Assert
        actual.Should().BeEquivalentTo(expected);
        mockServer.FindLogEntries(entitiesIdRequest).Count().Should().Be(1);
        mockServer.FindLogEntries(entitiesCompanyRequest).Count().Should().Be(1);
        mockServer.FindLogEntries(proceedingsInfoRequest).Count().Should().Be(1);
        mockServer.FindLogEntries(financePeriodsRequest).Count().Should().Be(1);
        mockServer.FindLogEntries(financeValuesRequest).Count().Should().Be(1);
    }

    [DataTestMethod]
    [DynamicData(nameof(TestData))]
    public async Task MultipleRequestsCausesOnlySingleApiCall(
        long tin,
        LegalEntityInfo expected,
        IRequestBuilder entitiesIdRequest,
        IRequestBuilder entitiesCompanyRequest,
        IRequestBuilder proceedingsInfoRequest,
        IRequestBuilder financePeriodsRequest,
        IRequestBuilder financeValuesRequest)
    {
        // Arrange
        var (mockServer, apiClient) = GetApiClient();
        var repository = new Mock<IRepository>();
        repository
            .Setup(x => x.GetAsync<BasicInfo>(It.IsAny<long>(), CancellationToken.None))
            .Returns(Task.FromResult<ReputationApiResponse?>(null));
        var serviceCollection = new ServiceCollection();
        serviceCollection.AddMemoryCache();
        serviceCollection.AddSingleton(new MemoryCacheEntryOptions() { SlidingExpiration = TimeSpan.FromSeconds(24) });
        serviceCollection.AddLogging();
        serviceCollection.AddSingleton(apiClient);
        serviceCollection.AddSingleton(x => repository.Object);
        serviceCollection.AddSingleton<LegalEntityChecker>();
        var provider = serviceCollection.BuildServiceProvider();
        var sut = provider.GetRequiredService<LegalEntityChecker>();
        var request = new LegalEntityInfoRequest() { Tin = tin };

        // Act
        var actual1 = await sut.GetLegalEntityInfo(request, Mock.Of<ServerCallContext>());
        var actual2 = await sut.GetLegalEntityInfo(request, Mock.Of<ServerCallContext>());
        var actual3 = await sut.GetLegalEntityInfo(request, Mock.Of<ServerCallContext>());

        // Assert
        actual1.Should().BeEquivalentTo(expected);
        actual2.Should().BeEquivalentTo(expected);
        actual3.Should().BeEquivalentTo(expected);
        mockServer.FindLogEntries(entitiesIdRequest).Count().Should().Be(1);
        mockServer.FindLogEntries(entitiesCompanyRequest).Count().Should().Be(1);
        mockServer.FindLogEntries(proceedingsInfoRequest).Count().Should().Be(1);
        mockServer.FindLogEntries(financePeriodsRequest).Count().Should().Be(1);
        mockServer.FindLogEntries(financeValuesRequest).Count().Should().Be(1);
    }

    [TestMethod]
    public async Task CacheExpirationResultsToDbQuery()
    {
        // Arrange
        var (mockServer, apiClient) = GetApiClient();
        var expected = OzonInfo;
        var tin = OzonInfo.BasicInfo.Tin;
        var repository = GetRepositoryMock();
        var serviceCollection = new ServiceCollection();
        serviceCollection.AddMemoryCache();
        serviceCollection.AddSingleton(new MemoryCacheEntryOptions() { SlidingExpiration = TimeSpan.FromSeconds(5) });
        serviceCollection.AddLogging();
        serviceCollection.AddSingleton(apiClient);
        serviceCollection.AddSingleton(x => repository.Object);
        serviceCollection.AddSingleton<LegalEntityChecker>();
        var provider = serviceCollection.BuildServiceProvider();
        var sut = provider.GetRequiredService<LegalEntityChecker>();
        var request = new LegalEntityInfoRequest() { Tin = tin };

        // Act
        await sut.GetLegalEntityInfo(request, Mock.Of<ServerCallContext>());
        await sut.GetLegalEntityInfo(request, Mock.Of<ServerCallContext>());
        await sut.GetLegalEntityInfo(request, Mock.Of<ServerCallContext>());
        await sut.GetLegalEntityInfo(request, Mock.Of<ServerCallContext>());
        Thread.Sleep(10_000);
        await sut.GetLegalEntityInfo(request, Mock.Of<ServerCallContext>());

        // Assert
        mockServer.FindLogEntries(OzonEntitiesIdRequest).Count().Should().Be(0);
        mockServer.FindLogEntries(OzonEntitiesCompanyRequest).Count().Should().Be(0);
        mockServer.FindLogEntries(OzonProceedingsInfoRequest).Count().Should().Be(0);
        mockServer.FindLogEntries(OzonFinancePeriodsRequest).Count().Should().Be(0);
        mockServer.FindLogEntries(OzonFinanceValuesRequest).Count().Should().Be(0);
        repository.Verify(x => x.GetAsync<Company_DA_Entities>(tin, CancellationToken.None), Times.Exactly(2));
        repository.Verify(x => x.GetAsync<CollectionContainerWithAggregations_DA_Proceeding_DA_Fssp_SumAggregationItem_DA_SumAggregationItem>(tin, CancellationToken.None), Times.Exactly(2));
        repository.Verify(x => x.GetAsync<ICollection<ReportPeriod_DA_FinancialReports> >(tin, CancellationToken.None), Times.Exactly(2));
        repository.Verify(x => x.GetAsync<FinancialCalculation_DA_FinancialReports>(tin, CancellationToken.None), Times.Exactly(2));
    }

    [TestMethod]
    public async Task DbValueExpirationResultsToApiCall()
    {
        // Arrange
        var (mockServer, apiClient) = GetApiClient();
        var expected = OzonInfo;
        var tin = OzonInfo.BasicInfo.Tin;
        var repository = GetRepositoryMock(DateTimeOffset.UtcNow.AddDays(-31));   
        var serviceCollection = new ServiceCollection();
        serviceCollection.AddMemoryCache();
        serviceCollection.AddSingleton(new MemoryCacheEntryOptions() { SlidingExpiration = TimeSpan.FromSeconds(5) });
        serviceCollection.AddLogging();
        serviceCollection.AddSingleton(apiClient);
        serviceCollection.AddSingleton(x => repository.Object);
        serviceCollection.AddSingleton<LegalEntityChecker>();
        var provider = serviceCollection.BuildServiceProvider();
        var sut = provider.GetRequiredService<LegalEntityChecker>();
        var request = new LegalEntityInfoRequest() { Tin = tin };

        // Act
        await sut.GetLegalEntityInfo(request, Mock.Of<ServerCallContext>());

        // Assert
        mockServer.FindLogEntries(OzonEntitiesIdRequest).Count().Should().Be(1);
        mockServer.FindLogEntries(OzonEntitiesCompanyRequest).Count().Should().Be(1);
        mockServer.FindLogEntries(OzonProceedingsInfoRequest).Count().Should().Be(1);
        mockServer.FindLogEntries(OzonFinancePeriodsRequest).Count().Should().Be(1);
        mockServer.FindLogEntries(OzonFinanceValuesRequest).Count().Should().Be(1);
        repository.Verify(x => x.GetAsync<Company_DA_Entities>(tin, CancellationToken.None), Times.Once);
        repository.Verify(x => x.GetAsync<CollectionContainerWithAggregations_DA_Proceeding_DA_Fssp_SumAggregationItem_DA_SumAggregationItem>(tin, CancellationToken.None), Times.Once);
        repository.Verify(x => x.GetAsync<ICollection<ReportPeriod_DA_FinancialReports> >(tin, CancellationToken.None), Times.Once);
        repository.Verify(x => x.GetAsync<FinancialCalculation_DA_FinancialReports>(tin, CancellationToken.None), Times.Once);
    }

    private static IEnumerable<object> TestData => new object[][]
    {
        [7704414297, YandexInfo, YandexEntitiesIdRequest, YandexEntitiesCompanyRequest, YandexProceedingsInfoRequest, YandexFinancePeriodsRequest, YandexFinanceValuesRequest],
        [7714617793, SvyaznoyInfo, SvyaznoyEntitiesIdRequest, SvyaznoyEntitiesCompanyRequest, SvyaznoyProceedingsInfoRequest, SvyaznoyFinancePeriodsRequest, SvyaznoyFinanceValuesRequest],
        [7703475603, OzonInfo, OzonEntitiesIdRequest, OzonEntitiesCompanyRequest, OzonProceedingsInfoRequest, OzonFinancePeriodsRequest, OzonFinanceValuesRequest]
    };
}
