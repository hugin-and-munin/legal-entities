using Grpc.Core;
using LegalEntityChecker;

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
        var sut = new LegalEntityChecker(api);

        // Act
        var actual = await sut.Get(request, Mock.Of<ServerCallContext>());

        // Assert
        actual.Should().BeNull();
        server.FindLogEntries(TestHelpers.GetIdRequest($"{tin}")).Count().Should().Be(1);
        server.FindLogEntries(TestHelpers.GetCompanyRequest()).Count().Should().Be(0);
        await postgres.DisposeAsync().AsTask();
    }

    [TestMethod]
    public async Task RequestOfExistingCompanyWorksOk()
    {
        // Arrange
        var postgres = TestHelpers.GetPostgres();
        await postgres.StartAsync();
        var repository = postgres.GetRepository();
        var (server, api) = TestHelpers.GetReputationApi(repository);
        var tin = 7704414297;
        var request = new LegalEntityInfoRequest() { Tin = tin };
        var sut = new LegalEntityChecker(api);

        // Act
        var actual = await sut.Get(request, Mock.Of<ServerCallContext>());

        // Assert
        actual.Should().Be(TestHelpers.ExpectedLegalEntityInfoReponse);
        server.FindLogEntries(TestHelpers.GetIdRequest($"{tin}")).Count().Should().Be(1);
        server.FindLogEntries(TestHelpers.GetCompanyRequest()).Count().Should().Be(1);
        await postgres.DisposeAsync().AsTask();
    }

    [TestMethod]
    public async Task RequestOfExistingCompanyAfterRebootWorksOk()
    {
        // Arrange
        var postgres = TestHelpers.GetPostgres();
        await postgres.StartAsync();
        var repository = postgres.GetRepository();
        var (serverBefore, apiBefore) = TestHelpers.GetReputationApi(repository);
        var (serverAfter, apiAfter) = TestHelpers.GetReputationApi(repository);
        var tin = 7704414297;
        var request = new LegalEntityInfoRequest() { Tin = tin };
        var sutBefore = new LegalEntityChecker(apiBefore);
        var actualBefore = await sutBefore.Get(request, Mock.Of<ServerCallContext>());
        var sutAfter = new LegalEntityChecker(apiAfter); // new instance to simulate reboot

        // Act
        var actualAfter = await sutAfter.Get(request, Mock.Of<ServerCallContext>());

        // Assert
        actualBefore.Should().Be(TestHelpers.ExpectedLegalEntityInfoReponse);
        actualAfter.Should().Be(TestHelpers.ExpectedLegalEntityInfoReponse);
        serverBefore.FindLogEntries(TestHelpers.GetIdRequest($"{tin}")).Count().Should().Be(1);
        serverBefore.FindLogEntries(TestHelpers.GetCompanyRequest()).Count().Should().Be(1);
        serverAfter.FindLogEntries(TestHelpers.GetIdRequest($"{tin}")).Count().Should().Be(0);
        serverAfter.FindLogEntries(TestHelpers.GetCompanyRequest()).Count().Should().Be(0);
        await postgres.DisposeAsync().AsTask();
    }

    [TestMethod]
    public async Task MultipleApiCallsCausesCachingInDatabase()
    {
        // Arrange
        var postgres = TestHelpers.GetPostgres();
        await postgres.StartAsync();
        var repository = postgres.GetRepository();
        var (server, api) = TestHelpers.GetReputationApi(repository);
        var tin = 7704414297;
        var request = new LegalEntityInfoRequest() { Tin = tin };
        var sut = new LegalEntityChecker(api);

        // Act
        var actual1 = await sut.Get(request, Mock.Of<ServerCallContext>());
        var actual2 = await sut.Get(request, Mock.Of<ServerCallContext>());
        var actual3 = await sut.Get(request, Mock.Of<ServerCallContext>());
        var actual4 = await sut.Get(request, Mock.Of<ServerCallContext>());

        // Assert
        actual1.Should().Be(TestHelpers.ExpectedLegalEntityInfoReponse);
        actual2.Should().Be(TestHelpers.ExpectedLegalEntityInfoReponse);
        actual3.Should().Be(TestHelpers.ExpectedLegalEntityInfoReponse);
        actual4.Should().Be(TestHelpers.ExpectedLegalEntityInfoReponse);

        // Only one call to API due to caching
        server.FindLogEntries(TestHelpers.GetIdRequest($"{tin}")).Count().Should().Be(1);
        server.FindLogEntries(TestHelpers.GetCompanyRequest()).Count().Should().Be(1);
        await postgres.DisposeAsync().AsTask();
    }
}
