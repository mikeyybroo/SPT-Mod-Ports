﻿using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Comfort.Common;
using StayInTarkov;
using EFT;
using EFT.HealthSystem;
using SPTQuestingBots.Controllers;
using SPTQuestingBots.Controllers.Bots;
using UnityEngine;

namespace SPTQuestingBots.BotLogic
{
    public class BotMonitor
    {
        public float NextLootCheckDelay { get; private set; } = ConfigController.Config.Questing.BotQuestingRequirements.BreakForLooting.MinTimeBetweenLootingChecks;

        private BotOwner botOwner;
        private LogicLayerMonitor lootingLayerMonitor;
        private LogicLayerMonitor extractLayerMonitor;
        private LogicLayerMonitor stationaryWSLayerMonitor;
        private Stopwatch lootSearchTimer = new Stopwatch();
        private bool wasLooting = false;
        private bool hasFoundLoot = false;
        private bool canUseSAINInterop = false;
        private bool canUseLootingBotsInterop = false;
        private int minTotalQuestsForExtract = int.MaxValue;
        private int minEFTQuestsForExtract = int.MaxValue;

        public BotMonitor(BotOwner _botOwner)
        {
            botOwner = _botOwner;

            lootingLayerMonitor = botOwner.GetPlayer.gameObject.AddComponent<LogicLayerMonitor>();
            lootingLayerMonitor.Init(botOwner, "Looting");

            extractLayerMonitor = botOwner.GetPlayer.gameObject.AddComponent<LogicLayerMonitor>();
            extractLayerMonitor.Init(botOwner, "SAIN ExtractLayer");

            stationaryWSLayerMonitor = botOwner.GetPlayer.gameObject.AddComponent<LogicLayerMonitor>();
            stationaryWSLayerMonitor.Init(botOwner, "StationaryWS");
            
            
            if (SAIN.Plugin.SAINInterop.Init())
            {
                canUseSAINInterop = true;
            }
            else
            {
                LoggingController.LogWarning("SAIN Interop not detected. Cannot instruct " + botOwner.GetText() + " to extract.");
            }
            
            if (LootingBots.LootingBotsInterop.Init())
            {
                canUseLootingBotsInterop = true;
            }
            else
            {
                LoggingController.LogWarning("Looting Bots Interop not detected. Cannot instruct " + botOwner.GetText() + " to loot.");
            }
        }

        public void InstructBotToExtract()
        {
            if (!canUseSAINInterop)
            {
                LoggingController.LogWarning("SAIN Interop not detected");
                return;
            }

            if (SAIN.Plugin.SAINInterop.TryExtractBot(botOwner))
            {
                LoggingController.LogInfo("Instructing " + botOwner.GetText() + " to extract now");
            }
            else
            {
                LoggingController.LogError("Cannot instruct " + botOwner.GetText() + " to extract. SAIN Interop not initialized properly.");
            }
        }

        public bool ShouldSearchForEnemy(double maxTimeSinceCombatEnded)
        {
            bool hasCloseDanger = botOwner.Memory.DangerData.HaveCloseDanger;

            bool wasInCombat = (Time.time - botOwner.Memory.LastTimeHit) < maxTimeSinceCombatEnded;
            wasInCombat |= (Time.time - botOwner.Memory.EnemySetTime) < maxTimeSinceCombatEnded;
            wasInCombat |= (Time.time - botOwner.Memory.LastEnemyTimeSeen) < maxTimeSinceCombatEnded;
            wasInCombat |= (Time.time - botOwner.Memory.UnderFireTime) < maxTimeSinceCombatEnded;

            return wasInCombat || hasCloseDanger;
        }

        public int UpdateSearchTimeAfterCombat()
        {
            System.Random random = new System.Random();
            return random.Next((int)ConfigController.Config.Questing.SearchTimeAfterCombat.Min, (int)ConfigController.Config.Questing.SearchTimeAfterCombat.Max);
        }

        public bool WantsToUseStationaryWeapon()
        {
            if (stationaryWSLayerMonitor.CanLayerBeUsed && stationaryWSLayerMonitor.IsLayerRequested())
            {
                return true;
            }

            return false;
        }

        public bool IsTryingToExtract()
        {
            if (!extractLayerMonitor.CanLayerBeUsed)
            {
                return false;
            }

            string layerName = botOwner.Brain.ActiveLayerName() ?? "null";
            if (layerName.Contains(extractLayerMonitor.LayerName) || extractLayerMonitor.IsLayerRequested())
            {
                return true;
            }

            return false;
        }
        public bool TryPreventBotFromLooting(float duration)
        {
            if (!canUseLootingBotsInterop)
            {
                //LoggingController.LogWarning("Looting Bots Interop not detected");
                return false;
            }

            if (LootingBots.LootingBotsInterop.TryPreventBotFromLooting(botOwner, duration))
            {
                LoggingController.LogInfo("Preventing " + botOwner.GetText() + " from looting");

                return true;
            }
            else
            {
                LoggingController.LogError("Cannot prevent " + botOwner.GetText() + " from looting. Looting Bots Interop not initialized properly.");
            }

            return false;
        }

        public bool TryForceBotToLootNow(float duration)
        {
            if (!canUseLootingBotsInterop)
            {
                //LoggingController.LogWarning("Looting Bots Interop not detected");
                return false;
            }

            if (canUseSAINInterop && !SAIN.Plugin.SAINInterop.TryResetDecisionsForBot(botOwner))
            {
                LoggingController.LogError("Cannot instruct " + botOwner.GetText() + " to reset its decisions. SAIN Interop not initialized properly.");
            }

            if (LootingBots.LootingBotsInterop.TryForceBotToLootNow(botOwner, duration))
            {
                LoggingController.LogInfo("Instructing " + botOwner.GetText() + " to loot now");

                // Need to reset these or bots may crouch-walk for a long distance
                // TODO: This doesn't seem to work. Maybe Looting Bots is forcing this behavior?
                botOwner.SetPose(1f);
                botOwner.SetTargetMoveSpeed(1f);

                return true;
            }
            else
            {
                LoggingController.LogError("Cannot instruct " + botOwner.GetText() + " to loot. Looting Bots Interop not initialized properly.");
            }

            return false;
        }
        
        public bool TryInstructBotToExtract()
        {
            if (!canUseSAINInterop)
            {
                //LoggingController.LogWarning("SAIN Interop not detected");
                return false;
            }

            if (SAIN.Plugin.SAINInterop.TryExtractBot(botOwner))
            {
                LoggingController.LogInfo("Instructing " + botOwner.GetText() + " to extract now");

                foreach(BotOwner follower in HiveMind.BotHiveMindMonitor.GetFollowers(botOwner))
                {
                    if (SAIN.Plugin.SAINInterop.TryExtractBot(follower))
                    {
                        LoggingController.LogInfo("Instructing follower " + follower.GetText() + " to extract now");
                    }
                    else
                    {
                        LoggingController.LogWarning("Could not instruct follower " + follower.GetText() + " to extract now");
                    }
                }

                return true;
            }
            else
            {
                LoggingController.LogError("Cannot instruct " + botOwner.GetText() + " to extract. SAIN Interop not initialized properly.");
            }

            return false;
        }

        public bool IsBotReadyToExtract()
        {
            int minRaidET = GClass1416.SessionSeconds(Singleton<AbstractGame>.Instance.GameTimer);
            float remainingRaidTime = GClass1416.EscapeTimeSeconds(Singleton<AbstractGame>.Instance.GameTimer);

            if (GClass1416.PastTimeSeconds(Singleton<AbstractGame>.Instance.GameTimer) < (minRaidET - (GClass1416.SessionSeconds(Singleton<AbstractGame>.Instance.GameTimer) - remainingRaidTime)))
            {
                return false;
            }

            if (remainingRaidTime < ConfigController.Config.Questing.ExtractionRequirements.MustExtractTimeRemaining)
            {
                LoggingController.LogInfo(botOwner.GetText() + " is ready to extract because the raid will be over in " + remainingRaidTime + " seconds.");
                return true;
            }

            System.Random random = new System.Random();
            float initialRaidTimeFraction = (float)remainingRaidTime / GClass1416.SessionSeconds(Singleton<AbstractGame>.Instance.GameTimer);

            if (minTotalQuestsForExtract == int.MaxValue)
            {
                Configuration.MinMaxConfig minMax = ConfigController.Config.Questing.ExtractionRequirements.TotalQuests * initialRaidTimeFraction;
                minTotalQuestsForExtract = random.Next((int)minMax.Min, (int)minMax.Max);
            }

            int totalQuestsCompleted = botOwner.NumberOfCompletedOrAchivedQuests();
            if (totalQuestsCompleted >= minTotalQuestsForExtract)
            {
                LoggingController.LogInfo(botOwner.GetText() + " has completed " + totalQuestsCompleted + " quests and is ready to extact.");
                return true;
            }
            //LoggingController.LogInfo(botOwner.GetText() + " has completed " + totalQuestsCompleted + "/" + minTotalQuestsForExtract + " quests");

            if (minEFTQuestsForExtract == int.MaxValue)
            {
                Configuration.MinMaxConfig minMax = ConfigController.Config.Questing.ExtractionRequirements.EFTQuests * initialRaidTimeFraction;
                minEFTQuestsForExtract = random.Next((int)minMax.Min, (int)minMax.Max);
            }

            int EFTQuestsCompleted = botOwner.NumberOfCompletedOrAchivedEFTQuests();
            if (EFTQuestsCompleted >= minEFTQuestsForExtract)
            {
                LoggingController.LogInfo(botOwner.GetText() + " has completed " + EFTQuestsCompleted + " EFT quests and is ready to extact.");
                return true;
            }
            //LoggingController.LogInfo(botOwner.GetText() + " has completed " + EFTQuestsCompleted + "/" + minEFTQuestsForExtract + " EFT quests");

            return false;
        }


        public bool ShouldWaitForFollowers()
        {
            // Check if the bot has any followers
            IReadOnlyCollection<BotOwner> followers = HiveMind.BotHiveMindMonitor.GetFollowers(botOwner);
            if (followers.Count == 0)
            {
                return false;
            }

            // Check if the bot is too far from any of its followers
            IEnumerable<float> followerDistances = followers.Select(f => Vector3.Distance(botOwner.Position, f.Position));
            if
            (
                followerDistances.Any(d => d > ConfigController.Config.Questing.BotQuestingRequirements.MaxFollowerDistance.Furthest)
                || followerDistances.All(d => d > ConfigController.Config.Questing.BotQuestingRequirements.MaxFollowerDistance.Nearest)
            )
            {
                return true;
            }

            return false;
        }

        public bool IsAbleBodied(bool writeToLog)
        {
            // Check if the bot needs to heal or perform surgery
            if (botOwner.Medecine.FirstAid.Have2Do || botOwner.Medecine.SurgicalKit.HaveWork)
            {
                if (writeToLog)
                {
                    LoggingController.LogInfo("Bot " + botOwner.GetText() + " needs to heal");
                }
                return false;
            }

            // Check if the bot needs to drink something
            if (100f * botOwner.HealthController.Hydration.Current / botOwner.HealthController.Hydration.Maximum < ConfigController.Config.Questing.BotQuestingRequirements.MinHydration)
            {
                if (writeToLog)
                {
                    LoggingController.LogInfo("Bot " + botOwner.GetText() + " needs to drink");
                }
                return false;
            }

            // Check if the bot needs to eat something
            if (100f * botOwner.HealthController.Energy.Current / botOwner.HealthController.Energy.Maximum < ConfigController.Config.Questing.BotQuestingRequirements.MinEnergy)
            {
                if (writeToLog)
                {
                    LoggingController.LogInfo("Bot " + botOwner.GetText() + " needs to eat");
                }
                return false;
            }

            // Get the health of all of the bot's body parts
            ValueStruct healthHead = botOwner.HealthController.GetBodyPartHealth(EBodyPart.Head);
            ValueStruct healthChest = botOwner.HealthController.GetBodyPartHealth(EBodyPart.Chest);
            ValueStruct healthStomach = botOwner.HealthController.GetBodyPartHealth(EBodyPart.Stomach);
            ValueStruct healthLeftLeg = botOwner.HealthController.GetBodyPartHealth(EBodyPart.LeftLeg);
            ValueStruct healthRightLeg = botOwner.HealthController.GetBodyPartHealth(EBodyPart.RightLeg);

            // Check if any of the bot's body parts need to be healed
            if
            (
                (100f * healthHead.Current / healthHead.Maximum < ConfigController.Config.Questing.BotQuestingRequirements.MinHealthHead)
                || (100f * healthChest.Current / healthChest.Maximum < ConfigController.Config.Questing.BotQuestingRequirements.MinHealthChest)
                || (100f * healthStomach.Current / healthStomach.Maximum < ConfigController.Config.Questing.BotQuestingRequirements.MinHealthStomach)
                || (100f * healthLeftLeg.Current / healthLeftLeg.Maximum < ConfigController.Config.Questing.BotQuestingRequirements.MinHealthLegs)
                || (100f * healthRightLeg.Current / healthRightLeg.Maximum < ConfigController.Config.Questing.BotQuestingRequirements.MinHealthLegs)
            )
            {
                if (writeToLog)
                {
                    LoggingController.LogInfo("Bot " + botOwner.GetText() + " cannot heal");
                }
                return false;
            }

            // Check if the bot is too overweight
            if (100f * botOwner.GetPlayer.Physical.Overweight > ConfigController.Config.Questing.BotQuestingRequirements.MaxOverweightPercentage)
            {
                if (writeToLog)
                {
                    LoggingController.LogInfo("Bot " + botOwner.GetText() + " is overweight");
                }
                return false;
            }

            return true;
        }

        public bool IsLooting()
        {
            string activeLogicName = DrakiaXYZ.BigBrain.Brains.BrainManager.GetActiveLogic(botOwner)?.GetType()?.Name ?? "null";
            return activeLogicName.Contains("Looting");
        }

        // TODO: something broken in here
        public bool ShouldCheckForLoot(float minTimeBetweenLooting)
        {
            if (!ConfigController.Config.Questing.BotQuestingRequirements.BreakForLooting.Enabled)
            {
                return false;
            }

            // Check if LootingBots is loaded
            if (!lootingLayerMonitor.CanLayerBeUsed)
            {
                return false;
            }

            NextLootCheckDelay = ConfigController.Config.Questing.BotQuestingRequirements.BreakForLooting.MinTimeBetweenLootingChecks;

            string activeLayerName = botOwner.Brain.ActiveLayerName() ?? "null";
            bool isSearchingForLoot = activeLayerName.Contains(lootingLayerMonitor.LayerName);
            bool isLooting = IsLooting();

            // The following logic is used to determine if a bot is allowed to search for loot:
            //      - If LootingBots has instructed the bot to check a lootable container, allow it
            //      - If the bot hasn't serached for loot for a minimum amount of time, allow it
            //      - After the minimum amount of time, the bot will only be allowed to search for a certain amount of time. If it doesn't find any loot
            //        in that time, it will be forced to continue questing
            //      - The minimum amount of time between loot checks depends on whether the bot successfully found loot during the previous check

            if(lootingLayerMonitor.CanUseLayer(minTimeBetweenLooting)){
                return false;
            }

            if
            (
                (isLooting || (lootSearchTimer.ElapsedMilliseconds < 1000 * ConfigController.Config.Questing.BotQuestingRequirements.BreakForLooting.MaxLootScanTime))
                && (isLooting || isSearchingForLoot || lootingLayerMonitor.CanUseLayer(minTimeBetweenLooting))
            )
            {
                // LoggingController.LogInfo("Layer for bot " + BotOwner.GetText() + ": " + activeLayerName + ". Logic: " + activeLogicName);

                if (isLooting)
                {
                    if (!hasFoundLoot)
                    {
                        LoggingController.LogInfo("Bot " + botOwner.GetText() + " has found loot");
                    }

                    NextLootCheckDelay = ConfigController.Config.Questing.BotQuestingRequirements.BreakForLooting.MinTimeBetweenLootingEvents;
                    lootSearchTimer.Reset();
                    hasFoundLoot = true;
                }
                else
                {
                    if (!wasLooting)
                    {
                        //LoggingController.LogInfo("Bot " + BotOwner.GetText() + " is searching for loot...");
                    }

                    lootSearchTimer.Start();
                }

                if (isSearchingForLoot || isLooting)
                {
                    wasLooting = true;
                }

                lootingLayerMonitor.RestartCanUseTimer();
                return true;
            }

            if (wasLooting || hasFoundLoot)
            {
                lootingLayerMonitor.RestartCanUseTimer();
                LoggingController.LogInfo("Bot " + botOwner.GetText() + " is done looting (Loot searching time: " + (lootSearchTimer.ElapsedMilliseconds / 1000.0) + ").");
            }

            lootSearchTimer.Reset();
            wasLooting = false;
            hasFoundLoot = false;
            return false;
        }
    }
}
