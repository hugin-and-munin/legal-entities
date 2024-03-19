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
        repositoryMock.Verify(x => x.GetBasicInfo(tin, CancellationToken.None), Times.Once);
        repositoryMock.Verify(x => x.UpsertBasicInfo(It.IsAny<ReputationApiResponse>(), CancellationToken.None), Times.Never);
    }

    [DataTestMethod]
    [DynamicData(nameof(LegalEntitiesInfos))]
    public async Task ApiReturnsLegalEntitiesInfoWhenGettingExistingCompany(LegalEntityInfoReponse expected)
    {
        // Arrange
        var repositoryMock = TestHelpers.GetRepositoryMock();
        var (server, sut) = TestHelpers.GetReputationApi(repositoryMock.Object);
        var tin = expected.Tin;
        var request = new LegalEntityInfoRequest() { Tin = tin };

        // Act
        var actual = await sut.Get(request, CancellationToken.None);

        // Assert
        actual.Should().Be(expected);
        repositoryMock.Verify(x => x.GetBasicInfo(tin, CancellationToken.None), Times.Once);
        repositoryMock.Verify(x => x.UpsertBasicInfo(It.IsAny<ReputationApiResponse>(), CancellationToken.None), Times.Once);
    }

    public static IEnumerable<object[]> LegalEntitiesInfos =>
    [
        [ TestHelpers.YandexInfo ],
        [ TestHelpers.SvyaznoyInfo ]
    ];
}
