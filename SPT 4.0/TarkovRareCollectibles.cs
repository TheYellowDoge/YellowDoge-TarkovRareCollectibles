using SPTarkov.DI.Annotations;
using SPTarkov.Server.Core.DI;
using SPTarkov.Server.Core.Helpers;
using SPTarkov.Server.Core.Models.Common;
using SPTarkov.Server.Core.Models.Eft.Common.Tables;
using SPTarkov.Server.Core.Models.Logging;
using SPTarkov.Server.Core.Models.Spt.Config;
using SPTarkov.Server.Core.Models.Spt.Mod;
using SPTarkov.Server.Core.Models.Utils;
using SPTarkov.Server.Core.Servers;
using SPTarkov.Server.Core.Services;
using SPTarkov.Server.Core.Services.Mod;
using System.Reflection;

namespace TarkovRareCollectibles
{
    public record ModMetadata : AbstractModMetadata
    {
        public override string ModGuid { get; init; } = "com.yellowdoge.tarkovrarecollectibles";
        public override string Name { get; init; } = "TarkovRareCollectibles";
        public override string Author { get; init; } = "YellowDoge";
        public override List<string>? Contributors { get; init; }
        public override SemanticVersioning.Version Version { get; init; } = new("1.1.5");
        public override SemanticVersioning.Range SptVersion { get; init; } = new("~4.0");
        public override List<string>? Incompatibilities { get; init; }
        public override Dictionary<string, SemanticVersioning.Range>? ModDependencies { get; init; }
        public override string? Url { get; init; }
        public override bool? IsBundleMod { get; init; } = true;
        public override string? License { get; init; } = "MIT";
    }

    [Injectable(TypePriority = OnLoadOrder.PostDBModLoader + 1)]
    public class TarkovRareCollectibles(
        ISptLogger<TarkovRareCollectibles> logger,
        ModHelper modHelper,
        CustomItemService customItemService,
        DatabaseService databaseService,
        ConfigServer configServer)
        : IOnLoad
    {
        public Task OnLoad()
        {
            var database = databaseService.GetTables();
            var pathToMod = modHelper.GetAbsolutePathToModFolder(Assembly.GetExecutingAssembly());

            // Load JSON files using strongly-typed deserialization
            var config = modHelper.GetJsonDataFromFile<Dictionary<string, double>>(pathToMod, System.IO.Path.Combine("config", "config.json"));
            var itemIdLookup = modHelper.GetJsonDataFromFile<Dictionary<string, string>>(pathToMod, System.IO.Path.Combine("db","Items","itemIdLookup.json"));
            var itemData = modHelper.GetJsonDataFromFile<Dictionary<string, NewItemFromCloneDetails>>(pathToMod, System.IO.Path.Combine("db", "Items", "itemData.json"));
            var staticLootData = modHelper.GetJsonDataFromFile<Dictionary<string, Dictionary<string, Dictionary<string, double>>>>(pathToMod, System.IO.Path.Combine("db", "Items", "staticLootData.json"));
            var looseLootData = modHelper.GetJsonDataFromFile<Dictionary<string, List<SpawnData>>>(pathToMod, System.IO.Path.Combine("db", "Items", "looseLootData.json"));
            var hallofFameData = modHelper.GetJsonDataFromFile<Dictionary<string, string>>(pathToMod, System.IO.Path.Combine("db", "Items", "hallofFameData.json"));
            var traderData = modHelper.GetJsonDataFromFile<Dictionary<string, Dictionary<string, bool>>>(pathToMod, System.IO.Path.Combine("db", "Items", "traderData.json"));

            logger.Info("[Tarkov Rare Collectibles] Start loading items");

            var itemService = new DogeItemService(logger, database, configServer);

            foreach (var itemId in itemIdLookup.Keys)
            {
                customItemService.CreateItemFromClone(itemData[itemId]);
            }

            itemService.AddToStaticLoot(staticLootData, config["staticLootMultiplier"]);
            itemService.AddToLooseLoot(looseLootData, config["looseLootMultiplier"]);
            itemService.AddToTraderTrades(traderData);
            itemService.AddToHallOfFame(hallofFameData);
            itemService.RemoveFromRewardPool(itemIdLookup);
            itemService.RemoveFromPMCLootPool(itemIdLookup);

            // This is where 
            logger.Success("[Tarkov Rare Collectibles] Finished loading items");
            return Task.CompletedTask;
        }
    }
}
