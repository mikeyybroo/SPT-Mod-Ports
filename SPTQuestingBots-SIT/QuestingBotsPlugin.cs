﻿using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using BepInEx;
using StayInTarkov;
using SPTQuestingBots.Controllers;
using SPTQuestingBots.Controllers.Bots;
using SPTQuestingBots.Models;

namespace SPTQuestingBots
{
    [BepInIncompatibility("com.pandahhcorp.aidisabler")]
    [BepInIncompatibility("com.dvize.AILimit")]
    //TODO: add requirement for SIT
    [BepInPlugin("com.DanW.QuestingBots", "DanW-QuestingBots", "0.3.5")]
    public class QuestingBotsPlugin : BaseUnityPlugin
    {
        public static string ModName { get; private set; } = "???";
        private void Awake()
        {
            Logger.LogInfo("Loading QuestingBots...");

            Logger.LogInfo("Loading QuestingBots...getting configuration data...");
            ConfigController.GetConfig();
            LoggingController.Logger = Logger;
            ModName = Info.Metadata.Name;

            if (ConfigController.Config.Enabled)
            {
                LoggingController.LogInfo("Loading QuestingBots...enabling patches and controllers...");

                //new Patches.CheckSPTVersionPatch().Enable();
                new Patches.GameWorldOnDestroyPatch().Enable();
                new Patches.OnGameStartedPatch().Enable();
                new Patches.BotOwnerBrainActivatePatch().Enable();
                new Patches.IsFollowerSuitableForBossPatch().Enable();
                new Patches.OnBeenKilledByAggressorPatch().Enable();
                new Patches.AirdropLandPatch().Enable();

                if (ConfigController.Config.InitialPMCSpawns.Enabled)
                {
                    new Patches.BossLocationSpawnActivatePatch().Enable();
                    new Patches.InitBossSpawnLocationPatch().Enable();
                    new Patches.BotOwnerCreatePatch().Enable();
                    new Patches.ReadyToPlayPatch().Enable();

                    Logger.LogInfo("Initial PMC spawning is enabled. Adjusting PMC conversion chances...");
                    ConfigController.AdjustPMCConversionChances(0, false);
                }

                this.GetOrAddComponent<LocationController>();
                this.GetOrAddComponent<BotGenerator>();

                if (ConfigController.Config.Questing.Enabled)
                {
                    this.GetOrAddComponent<BotQuestBuilder>();
                }

                if (ConfigController.Config.Debug.Enabled)
                {
                    if (ConfigController.Config.Debug.ShowZoneOutlines || ConfigController.Config.Debug.ShowFailedPaths)
                    {
                        this.GetOrAddComponent<PathRender>();
                    }
                }

                // Add options to the F12 menu
                QuestingBotsPluginConfig.BuildConfigOptions(Config);

                performLobotomies();
            }

            Logger.LogInfo("Loading QuestingBots...done.");
        }

        private void performLobotomies()
        {
            IEnumerable<BotBrainType> allNonSniperBrains = BotBrainHelpers.GetAllNonSniperBrains();
            IEnumerable<BotBrainType> allBrains = allNonSniperBrains.AddAllSniperBrains();

            LoggingController.LogInfo("Loading QuestingBots...changing bot brains for sleeping: " + string.Join(", ", allBrains));
            DrakiaXYZ.BigBrain.Brains.BrainManager.AddCustomLayer(typeof(BotLogic.Sleep.SleepingLayer), allBrains.ToStringList(), 99);

            if (!ConfigController.Config.Questing.Enabled)
            {
                return;
            }

            LoggingController.LogInfo("Loading QuestingBots...changing bot brains for questing: " + string.Join(", ", allNonSniperBrains));
            DrakiaXYZ.BigBrain.Brains.BrainManager.AddCustomLayer(typeof(BotLogic.Objective.BotObjectiveLayer), allNonSniperBrains.ToStringList(), ConfigController.Config.Questing.BrainLayerPriority);

            LoggingController.LogInfo("Loading QuestingBots...changing bot brains for following: " + string.Join(", ", allBrains));
            DrakiaXYZ.BigBrain.Brains.BrainManager.AddCustomLayer(typeof(BotLogic.Follow.BotFollowerLayer), allBrains.ToStringList(), ConfigController.Config.Questing.BrainLayerPriority + 1);
        }
    }
}
