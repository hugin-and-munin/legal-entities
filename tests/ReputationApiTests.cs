using System.Text.Json;
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
        var actual = await sut.GetBasicInfo(request, CancellationToken.None);

        // Assert
        actual.Should().BeNull();
        repositoryMock.Verify(x => x.GetBasicInfo(tin, CancellationToken.None), Times.Once);
        repositoryMock.Verify(x => x.UpsertBasicInfo(It.IsAny<ReputationApiResponse>(), CancellationToken.None), Times.Never);
    }

    [DataTestMethod]
    [DynamicData(nameof(LegalEntitiesBasicInfos))]
    public async Task OnRequestExistingCompanyReturnsBasicInfo(BasicInfo expected)
    {
        // Arrange
        var repositoryMock = TestHelpers.GetRepositoryMock();
        var (server, sut) = TestHelpers.GetReputationApi(repositoryMock.Object);
        var tin = expected.Tin;
        var request = new LegalEntityInfoRequest() { Tin = tin };

        // Act
        var actual = await sut.GetBasicInfo(request, CancellationToken.None);

        // Assert
        actual.Should().BeEquivalentTo(expected);
        repositoryMock.Verify(x => x.GetBasicInfo(tin, CancellationToken.None), Times.Once);
        repositoryMock.Verify(x => x.GetProceedingsInfo(tin, CancellationToken.None), Times.Once);
        repositoryMock.Verify(x => x.UpsertBasicInfo(It.IsAny<ReputationApiResponse>(), CancellationToken.None), Times.Once);
        repositoryMock.Verify(x => x.UpsertProceedingsInfo(It.IsAny<ReputationApiResponse>(), CancellationToken.None), Times.Once);
    }

    [DataTestMethod]
    [DynamicData(nameof(LegalEntitiesExtendedInfos))]
    public async Task OnRequestExistingCompanyReturnsExtendedInfo(ExtendedInfo expected)
    {
        // Arrange
        var repositoryMock = TestHelpers.GetRepositoryMock();
        var (server, sut) = TestHelpers.GetReputationApi(repositoryMock.Object);
        var tin = expected.BasicInfo.Tin;
        var request = new LegalEntityInfoRequest() { Tin = tin };

        // Act
        var actual = await sut.GetExtendedInfo(request, CancellationToken.None);

        // Assert
        actual.Should().BeEquivalentTo(expected);
        repositoryMock.Verify(x => x.GetBasicInfo(tin, CancellationToken.None), Times.Once);
        repositoryMock.Verify(x => x.GetProceedingsInfo(tin, CancellationToken.None), Times.Once);
        repositoryMock.Verify(x => x.UpsertBasicInfo(It.IsAny<ReputationApiResponse>(), CancellationToken.None), Times.Once);
        repositoryMock.Verify(x => x.UpsertProceedingsInfo(It.IsAny<ReputationApiResponse>(), CancellationToken.None), Times.Once);
    }

    public static IEnumerable<object[]> LegalEntitiesBasicInfos =>
    [
        [ TestHelpers.YandexBasicInfo ],
        [ TestHelpers.SvyaznoyBasicInfo ]
    ];

    public static IEnumerable<object[]> LegalEntitiesExtendedInfos =>
    [
        [ TestHelpers.YandexExtendedInfo ],
        [ TestHelpers.SvyaznoyExtendedInfo ]
    ];
}
