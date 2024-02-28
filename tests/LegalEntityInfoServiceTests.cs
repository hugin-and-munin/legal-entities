using Grpc.Core;
using LegalEntityChecker;

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
        var apiMock = TestHelpers.GetReputationApiMock();
        var sut = new LegalEntityChecker(apiMock.Object);

        // Act
        var actual = await sut.Get(new LegalEntityInfoRequest(), Mock.Of<ServerCallContext>());

        // Assert
        actual.Should().Be(null);
        apiMock.Verify(x => x.Get(It.IsAny<LegalEntityInfoRequest>(), CancellationToken.None), Times.Once);
    }

    /// <summary>
    /// New company means it doesn't exist in memory cache
    /// </summary>
    [TestMethod]
    public async Task RequestOfNewCompanyCausesApiCall()
    {
        // Arrange
        var expected = TestHelpers.ExpectedLegalEntityInfoReponse;
        var apiMock = TestHelpers.GetReputationApiMock(expected);
        var sut = new LegalEntityChecker(apiMock.Object);

        // Act
        var actual = await sut.Get(new LegalEntityInfoRequest(), Mock.Of<ServerCallContext>());

        // Assert
        actual.Should().Be(expected);
        apiMock.Verify(x => x.Get(It.IsAny<LegalEntityInfoRequest>(), CancellationToken.None), Times.Once);
    }

    [TestMethod]
    public async Task MultipleRequestOfCachedCompanyCausesOnlyOneApiCall()
    {
        // Arrange
        var expected = TestHelpers.ExpectedLegalEntityInfoReponse;
        var apiMock = TestHelpers.GetReputationApiMock(expected);
        var sut = new LegalEntityChecker(apiMock.Object);

        // Act
        var actual1 = await sut.Get(new LegalEntityInfoRequest(), Mock.Of<ServerCallContext>());
        var actual2 = await sut.Get(new LegalEntityInfoRequest(), Mock.Of<ServerCallContext>());
        var actual3 = await sut.Get(new LegalEntityInfoRequest(), Mock.Of<ServerCallContext>());

        // Assert
        actual1.Should().Be(actual2);
        actual2.Should().Be(actual3);
        actual3.Should().Be(expected);
        apiMock.Verify(x => x.Get(It.IsAny<LegalEntityInfoRequest>(), CancellationToken.None), Times.Once);
    }
}
