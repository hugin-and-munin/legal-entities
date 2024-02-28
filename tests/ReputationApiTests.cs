using LegalEntities.Reputation;
using LegalEntityChecker;

namespace LegalEntities.Tests;

[TestClass]
public class ReputationApiTests
{
    [TestMethod]
    public async Task ApiReturnsNullWhenGettingUnexistingCompany()
    {
        // Arrange
        var repositoryMock = TestHelpers.GetRepositoryMock();
        var (server, sut) = TestHelpers.GetReputationApi(repositoryMock.Object);
        var tin = 123;
        var request = new LegalEntityInfoRequest() { Tin = tin };

        // Act
        var actual = await sut.Get(request, CancellationToken.None);

        // Assert
        actual.Should().BeNull();
        repositoryMock.Verify(x => x.GetReputationResponse(tin, CancellationToken.None), Times.Once);
        repositoryMock.Verify(x => x.UpsertReputationResponse(It.IsAny<ReputationApiResponse>(), CancellationToken.None), Times.Never);
    }

    [TestMethod]
    public async Task ApiReturnsLegalEntitiesInfoWhenGettingExistingCompany()
    {
        // Arrange
        var repositoryMock = TestHelpers.GetRepositoryMock();
        var (server, sut) = TestHelpers.GetReputationApi(repositoryMock.Object);
        var tin = 7704414297;
        var request = new LegalEntityInfoRequest() { Tin = tin };

        // Act
        var actual = await sut.Get(request, CancellationToken.None);

        // Assert
        actual.Should().Be(TestHelpers.ExpectedLegalEntityInfoReponse);
        repositoryMock.Verify(x => x.GetReputationResponse(tin, CancellationToken.None), Times.Once);
        repositoryMock.Verify(x => x.UpsertReputationResponse(It.IsAny<ReputationApiResponse>(), CancellationToken.None), Times.Once);
    }
}
