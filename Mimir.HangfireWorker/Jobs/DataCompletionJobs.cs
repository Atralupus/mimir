using Lib9c.Models.Extensions;
using Libplanet.Crypto;
using Microsoft.Extensions.Options;
using Mimir.HangfireWorker.Services;
using Mimir.MongoDB;
using Mimir.MongoDB.Bson;
using Mimir.Worker;
using Mimir.Worker.Client;
using Mimir.Worker.Services;
using Mimir.Worker.Util;
using Serilog;

namespace Mimir.HangfireWorker.Jobs;

public class DataCompletionJobs
{
    private readonly MongoDbService _dbService;
    private readonly IStateService _stateService;
    private readonly IHeadlessGQLClient _headlessGqlClient;
    private readonly INotFoundCacheService _notFoundCacheService;
    private readonly IOptions<Configuration> _configuration;
    private readonly ILogger _logger;

    public DataCompletionJobs(
        MongoDbService dbService,
        IStateService stateService,
        IHeadlessGQLClient headlessGqlClient,
        INotFoundCacheService notFoundCacheService,
        IOptions<Configuration> configuration
    )
    {
        _dbService = dbService;
        _stateService = stateService;
        _headlessGqlClient = headlessGqlClient;
        _notFoundCacheService = notFoundCacheService;
        _configuration = configuration;
        _logger = Log.ForContext<DataCompletionJobs>();
    }

    public async Task CompleteAgentData(string addressString)
    {
        try
        {
            var address = new Address(addressString);
            var cacheKey = $"agent:{addressString}";

            if (await _notFoundCacheService.IsNotFoundCachedAsync(cacheKey))
            {
                _logger.Information(
                    "Agent {Address} is cached as not found, skipping",
                    addressString
                );
                return;
            }

            var isExist = await _dbService.IsExistAgentAsync(address);
            if (isExist)
            {
                _logger.Information("Agent {Address} already exists in database", addressString);
                return;
            }

            var currentBlockIndex = await _stateService.GetLatestIndex(CancellationToken.None);
            var stateGetter = _stateService.At(_configuration);

            try
            {
                var agentState = await stateGetter.GetAgentStateAccount(address);
                var document = new AgentDocument(currentBlockIndex, agentState.Address, agentState);

                await _dbService.UpsertStateDataManyAsync(
                    CollectionNames.GetCollectionName<AgentDocument>(),
                    [document]
                );

                _logger.Information(
                    "Successfully completed agent data for {Address}",
                    addressString
                );
            }
            catch (Mimir.Worker.Exceptions.StateNotFoundException)
            {
                await _notFoundCacheService.CacheNotFoundAsync(cacheKey);
                _logger.Information(
                    "Agent {Address} not found in blockchain, cached for {Days} days",
                    addressString,
                    7
                );
            }
        }
        catch (Exception ex)
        {
            _logger.Error(ex, "Error completing agent data for {Address}", addressString);
            throw;
        }
    }

    public async Task CompleteAvatarData(string addressString)
    {
        try
        {
            var address = new Address(addressString);
            var cacheKey = $"avatar:{addressString}";

            if (await _notFoundCacheService.IsNotFoundCachedAsync(cacheKey))
            {
                _logger.Information(
                    "Avatar {Address} is cached as not found, skipping",
                    addressString
                );
                return;
            }

            var isExist = await _dbService.IsExistAvatarAsync(address);
            if (isExist)
            {
                _logger.Information("Avatar {Address} already exists in database", addressString);
                return;
            }

            var currentBlockIndex = await _stateService.GetLatestIndex(CancellationToken.None);
            var stateGetter = _stateService.At(_configuration);

            try
            {
                var avatarState = await stateGetter.GetAvatarStateAsync(address);
                var inventoryState = await stateGetter.GetInventoryState(
                    address,
                    CancellationToken.None
                );
                var armorId = inventoryState.GetArmorId();
                var portraitId = inventoryState.GetPortraitId();

                var document = new AvatarDocument(
                    currentBlockIndex,
                    avatarState.Address,
                    avatarState,
                    armorId,
                    portraitId
                );

                await _dbService.UpsertStateDataManyAsync(
                    CollectionNames.GetCollectionName<AvatarDocument>(),
                    [document]
                );

                _logger.Information(
                    "Successfully completed avatar data for {Address}",
                    addressString
                );
            }
            catch (Mimir.Worker.Exceptions.StateNotFoundException)
            {
                await _notFoundCacheService.CacheNotFoundAsync(cacheKey);
                _logger.Information(
                    "Avatar {Address} not found in blockchain, cached for {Days} days",
                    addressString,
                    7
                );
            }
        }
        catch (Exception ex)
        {
            _logger.Error(ex, "Error completing avatar data for {Address}", addressString);
            throw;
        }
    }

    public async Task BatchCompleteAgentData(List<string> addresses)
    {
        foreach (var address in addresses)
        {
            try
            {
                await CompleteAgentData(address);
            }
            catch (Exception ex)
            {
                _logger.Error(ex, "Error in batch agent completion for {Address}", address);
            }
        }
    }

    public async Task BatchCompleteAvatarData(List<string> addresses)
    {
        foreach (var address in addresses)
        {
            try
            {
                await CompleteAvatarData(address);
            }
            catch (Exception ex)
            {
                _logger.Error(ex, "Error in batch avatar completion for {Address}", address);
            }
        }
    }
}
