using FluentAssertions;
using Libplanet.Crypto;
using Microsoft.Extensions.Options;
using Moq;
using Mimir.HangfireWorker.Configuration;
using Mimir.HangfireWorker.Jobs;
using Mimir.HangfireWorker.Services;
using Mimir.MongoDB;
using Mimir.Worker.Client;
using Mimir.Worker.Services;
using Xunit;

namespace Mimir.HangfireWorker.Tests.Jobs;

public class DataCompletionJobsTests
{
    private readonly Mock<MongoDbService> _mockDbService;
    private readonly Mock<IStateService> _mockStateService;
    private readonly Mock<IHeadlessGQLClient> _mockHeadlessGqlClient;
    private readonly Mock<INotFoundCacheService> _mockNotFoundCacheService;
    private readonly Configuration _configuration;
    private readonly DataCompletionJobs _jobs;

    public DataCompletionJobsTests()
    {
        _mockDbService = new Mock<MongoDbService>();
        _mockStateService = new Mock<IStateService>();
        _mockHeadlessGqlClient = new Mock<IHeadlessGQLClient>();
        _mockNotFoundCacheService = new Mock<INotFoundCacheService>();
        _configuration = new Configuration
        {
            NotFoundCacheExpirationDays = 7
        };

        _jobs = new DataCompletionJobs(
            _mockDbService.Object,
            _mockStateService.Object,
            _mockHeadlessGqlClient.Object,
            _mockNotFoundCacheService.Object,
            Options.Create(_configuration)
        );
    }

    [Fact]
    public async Task CompleteAgentData_WhenNotCachedAndNotExists_ShouldCompleteData()
    {
        var address = new Address("0x0000000031000000000200000000030000000004");
        var addressString = address.ToString();

        _mockNotFoundCacheService
            .Setup(x => x.IsNotFoundCachedAsync($"agent:{addressString}"))
            .ReturnsAsync(false);

        _mockDbService
            .Setup(x => x.IsExistAgentAsync(address))
            .ReturnsAsync(false);

        _mockStateService
            .Setup(x => x.GetLatestIndex(It.IsAny<CancellationToken>()))
            .ReturnsAsync(1000L);

        var mockStateGetter = new Mock<Mimir.Worker.Util.StateGetter>();
        _mockStateService
            .Setup(x => x.At(_configuration))
            .Returns(mockStateGetter.Object);

        var agentState = new Lib9c.Models.States.AgentState
        {
            Address = address,
            AvatarAddresses = new Dictionary<int, Address>()
        };

        mockStateGetter
            .Setup(x => x.GetAgentStateAccount(address))
            .ReturnsAsync(agentState);

        await _jobs.CompleteAgentData(addressString);

        _mockDbService.Verify(x => x.UpsertStateDataManyAsync(
            It.IsAny<string>(),
            It.IsAny<Mimir.MongoDB.Bson.AgentDocument[]>()
        ), Times.Once);
    }

    [Fact]
    public async Task CompleteAgentData_WhenCachedAsNotFound_ShouldSkip()
    {
        var address = new Address("0x0000000031000000000200000000030000000004");
        var addressString = address.ToString();

        _mockNotFoundCacheService
            .Setup(x => x.IsNotFoundCachedAsync($"agent:{addressString}"))
            .ReturnsAsync(true);

        await _jobs.CompleteAgentData(addressString);

        _mockDbService.Verify(x => x.IsExistAgentAsync(It.IsAny<Address>()), Times.Never);
        _mockStateService.Verify(x => x.GetLatestIndex(It.IsAny<CancellationToken>()), Times.Never);
    }

    [Fact]
    public async Task CompleteAgentData_WhenAlreadyExists_ShouldSkip()
    {
        var address = new Address("0x0000000031000000000200000000030000000004");
        var addressString = address.ToString();

        _mockNotFoundCacheService
            .Setup(x => x.IsNotFoundCachedAsync($"agent:{addressString}"))
            .ReturnsAsync(false);

        _mockDbService
            .Setup(x => x.IsExistAgentAsync(address))
            .ReturnsAsync(true);

        await _jobs.CompleteAgentData(addressString);

        _mockStateService.Verify(x => x.GetLatestIndex(It.IsAny<CancellationToken>()), Times.Never);
    }

    [Fact]
    public async Task CompleteAvatarData_WhenNotCachedAndNotExists_ShouldCompleteData()
    {
        var address = new Address("0x0000005001000000000200000000030000000004");
        var addressString = address.ToString();

        _mockNotFoundCacheService
            .Setup(x => x.IsNotFoundCachedAsync($"avatar:{addressString}"))
            .ReturnsAsync(false);

        _mockDbService
            .Setup(x => x.IsExistAvatarAsync(address))
            .ReturnsAsync(false);

        _mockStateService
            .Setup(x => x.GetLatestIndex(It.IsAny<CancellationToken>()))
            .ReturnsAsync(1000L);

        var mockStateGetter = new Mock<Mimir.Worker.Util.StateGetter>();
        _mockStateService
            .Setup(x => x.At(_configuration))
            .Returns(mockStateGetter.Object);

        var avatarState = new Lib9c.Models.States.AvatarState
        {
            Address = address,
            Name = "TestAvatar",
            CharacterId = 1,
            Level = 1,
            Exp = 0,
            AgentAddress = new Address("0x0000000031000000000200000000030000000004")
        };

        var inventoryState = new Lib9c.Models.States.Inventory
        {
            Items = new List<Lib9c.Models.Items.Item>()
        };

        mockStateGetter
            .Setup(x => x.GetAvatarStateAsync(address))
            .ReturnsAsync(avatarState);

        mockStateGetter
            .Setup(x => x.GetInventoryState(address, It.IsAny<CancellationToken>()))
            .ReturnsAsync(inventoryState);

        await _jobs.CompleteAvatarData(addressString);

        _mockDbService.Verify(x => x.UpsertStateDataManyAsync(
            It.IsAny<string>(),
            It.IsAny<Mimir.MongoDB.Bson.AvatarDocument[]>()
        ), Times.Once);
    }

    [Fact]
    public async Task CompleteAvatarData_WhenCachedAsNotFound_ShouldSkip()
    {
        var address = new Address("0x0000005001000000000200000000030000000004");
        var addressString = address.ToString();

        _mockNotFoundCacheService
            .Setup(x => x.IsNotFoundCachedAsync($"avatar:{addressString}"))
            .ReturnsAsync(true);

        await _jobs.CompleteAvatarData(addressString);

        _mockDbService.Verify(x => x.IsExistAvatarAsync(It.IsAny<Address>()), Times.Never);
        _mockStateService.Verify(x => x.GetLatestIndex(It.IsAny<CancellationToken>()), Times.Never);
    }
} 