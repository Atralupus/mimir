using System.Text.Json.Serialization;
using Bencodex.Types;
using Libplanet.Crypto;
using Microsoft.AspNetCore.Mvc;
using Nekoyume;
using Nekoyume.Action;
using Nekoyume.Arena;
using Nekoyume.Model;
using Nekoyume.Model.BattleStatus.Arena;
using Nekoyume.Model.EnumType;
using Nekoyume.Model.Item;
using Nekoyume.Model.State;
using Nekoyume.TableData;
using NineChroniclesUtilBackend.Models.Arena;
using NineChroniclesUtilBackend.Services;
using JsonOptions = Microsoft.AspNetCore.Http.Json.JsonOptions;

var builder = WebApplication.CreateBuilder(args);

builder.Configuration.AddEnvironmentVariables();

builder.Services.Configure<HeadlessStateServiceOptions>(builder.Configuration.GetRequiredSection("StateService"));
builder.Services.ConfigureHttpJsonOptions(options =>
    options.SerializerOptions.Converters.Add(new JsonStringEnumConverter()));

builder.Services.AddEndpointsApiExplorer();
builder.Services.AddSwaggerGen(options =>
{
    options.SupportNonNullableReferenceTypes();
});
builder.Services.AddSingleton<IStateService, HeadlessStateService>();

var app = builder.Build();

app.UseSwagger();
app.UseSwaggerUI();
app.UseHttpsRedirection();

// FIXME: Refactoring these logics.
app.MapPost("/arena/simulate",
        async ([FromBody] ArenaSimulateRequest arenaSimulateRequest, IStateService stateService) =>
        {
            async Task<T> GetSheet<T>()
                where T : ISheet, new()
            {
                var sheetState = await stateService.GetState(Addresses.TableSheet.Derive(typeof(T).Name));
                if (sheetState is not Text sheetValue)
                {
                    throw new ArgumentException(nameof(T));
                }

                var sheet = new T();
                sheet.Set(sheetValue.Value);
                return sheet;
            }

            async Task<AvatarState> GetAvatarState(Address avatarAddress)
            {
                var state = await stateService.GetState(avatarAddress);
                if (state is not Dictionary dictionary)
                {
                    throw new ArgumentException(nameof(avatarAddress));
                }

                var inventoryAddress = avatarAddress.Derive("inventory");
                var inventoryState = await stateService.GetState(inventoryAddress);
                if (inventoryState is not List list)
                {
                    throw new ArgumentException(nameof(avatarAddress));
                }

                var inventory = new Inventory(list);

                var avatarState = new AvatarState(dictionary)
                {
                    inventory = inventory
                };

                return avatarState;
            }

            async Task<ItemSlotState> GetItemSlotState(Address avatarAddress)
            {
                var state = await stateService.GetState(
                    ItemSlotState.DeriveAddress(avatarAddress, BattleType.Arena));
                return state switch
                {
                    List list => new ItemSlotState(list),
                    null => new ItemSlotState(BattleType.Arena),
                    _ => throw new ArgumentException(nameof(avatarAddress))
                };
            }

            async Task<List<RuneState>> GetRuneStates(Address avatarAddress)
            {
                var state = await stateService.GetState(
                    RuneSlotState.DeriveAddress(avatarAddress, BattleType.Arena));
                var runeSlotState = state switch
                {
                    List list => new RuneSlotState(list),
                    null => new RuneSlotState(BattleType.Arena),
                    _ => throw new ArgumentException(nameof(avatarAddress))
                };

                var runes = new List<RuneState>();
                foreach (var runeStateAddress in runeSlotState.GetEquippedRuneSlotInfos().Select(info => RuneState.DeriveAddress(avatarAddress, info.RuneId)))
                {
                    if (await stateService.GetState(runeStateAddress) is List list)
                    {
                        runes.Add(new RuneState(list));
                    }
                }

                return runes;
            }

            var seed = arenaSimulateRequest.Seed ?? new Random().Next();
            var myAvatarAddress = new Address(arenaSimulateRequest.MyAvatarAddress);
            var enemyAvatarAddress = new Address(arenaSimulateRequest.EnemyAvatarAddress);
            var random = new NineChroniclesUtilBackend.Random(seed);
            var simulator = new ArenaSimulator(random, 5);
            var myAvatarState = await GetAvatarState(myAvatarAddress);
            var myAvatarItemSlotState = await GetItemSlotState(myAvatarAddress);
            var myAvatarRuneStates = await GetRuneStates(myAvatarAddress);
            var enemyAvatarState = await GetAvatarState(enemyAvatarAddress);
            var enemyAvatarItemSlotState = await GetItemSlotState(enemyAvatarAddress);
            var enemyAvatarRuneStates = await GetRuneStates(enemyAvatarAddress);
            var arenaLog = simulator.Simulate(
                new ArenaPlayerDigest(myAvatarState, myAvatarItemSlotState.Equipments, myAvatarItemSlotState.Costumes, myAvatarRuneStates),
                new ArenaPlayerDigest(enemyAvatarState, enemyAvatarItemSlotState.Equipments, enemyAvatarItemSlotState.Costumes, enemyAvatarRuneStates),
                new ArenaSimulatorSheets(
                    await GetSheet<MaterialItemSheet>(),
                    await GetSheet<SkillSheet>(),
                    await GetSheet<SkillBuffSheet>(),
                    await GetSheet<StatBuffSheet>(),
                    await GetSheet<SkillActionBuffSheet>(),
                    await GetSheet<ActionBuffSheet>(),
                    await GetSheet<CharacterSheet>(),
                    await GetSheet<CharacterLevelSheet>(),
                    await GetSheet<EquipmentItemSetEffectSheet>(),
                    await GetSheet<CostumeStatSheet>(),
                    await GetSheet<WeeklyArenaRewardSheet>(),
                    await GetSheet<RuneOptionSheet>()
                ),
                true);
            return new ArenaSimulateResponse(arenaLog.Result switch
            {
                ArenaLog.ArenaResult.Lose => ArenaResult.Lose,
                ArenaLog.ArenaResult.Win => ArenaResult.Win,
                _ => throw new ArgumentOutOfRangeException()
            });
        })
    .WithOpenApi();

app.Run();
