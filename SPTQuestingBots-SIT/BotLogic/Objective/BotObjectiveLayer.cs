﻿using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using EFT;
using SPTQuestingBots.BehaviorExtensions;
using SPTQuestingBots.BotLogic.HiveMind;
using SPTQuestingBots.Controllers;
using SPTQuestingBots.Models;

namespace SPTQuestingBots.BotLogic.Objective
{
    internal class BotObjectiveLayer : CustomLayerForQuesting
    {
        private Stopwatch followersTooFarTimer = new Stopwatch();

        public BotObjectiveLayer(BotOwner _botOwner, int _priority) : base(_botOwner, _priority, 25)
        {
            
        }

        public override string GetName()
        {
            return "BotObjectiveLayer";
        }

        public override Action GetNextAction()
        {
            return base.GetNextAction();
        }

        public override bool IsCurrentActionEnding()
        {
            return base.IsCurrentActionEnding();
        }

        public override bool IsActive()
        {
            if (!canUpdate() && QuestingBotsPluginConfig.QuestingLogicTimeGatingEnabled.Value)
            {
                return previousState;
            }

            // Check if somebody disabled questing in the F12 menu
            if (!QuestingBotsPluginConfig.QuestingEnabled.Value)
            {
                return updatePreviousState(false);
            }

            if ((BotOwner.BotState != EBotState.Active) || BotOwner.IsDead)
            {
                return updatePreviousState(false);
            }

            if (!objectiveManager.IsQuestingAllowed)
            {
                return updatePreviousState(false);
            }

            // Ensure all quests have been loaded and generated
            if (!Controllers.Bots.BotQuestBuilder.HaveQuestsBeenBuilt)
            {
                return updatePreviousState(false);
            }

            // Check if the bot has a boss that's still alive
            if (BotHiveMindMonitor.HasBoss(BotOwner))
            {
                Controllers.Bots.BotJobAssignmentFactory.InactivateAllJobAssignmentsForBot(BotOwner.Profile.Id);

                return updatePreviousState(false);
            }

            float pauseRequestTime = getPauseRequestTime();
            if (pauseRequestTime > 0)
            {
                LoggingController.LogInfo("Pausing layer for " + pauseRequestTime + "s...");
                return pauseLayer(pauseRequestTime);
            }

            // Check if the bot wants to use a mounted weapon
            if (objectiveManager.IsAllowedToTakeABreak() && objectiveManager.BotMonitor.WantsToUseStationaryWeapon())
            {
                return updatePreviousState(false);
            }
            
            // Check if the bot wants to loot
            if (objectiveManager.IsAllowedToTakeABreak() && objectiveManager.BotMonitor.ShouldCheckForLoot(objectiveManager.BotMonitor.NextLootCheckDelay))
            {
                // BotHiveMindMonitor.UpdateValueForBot(BotHiveMindSensorType.WantsToLoot, BotOwner, true);
                return updatePreviousState(pauseLayer(ConfigController.Config.Questing.BotQuestingRequirements.BreakForLooting.MaxTimeToStartLooting));
            }
            // BotHiveMindMonitor.UpdateValueForBot(BotHiveMindSensorType.WantsToLoot, BotOwner, false);
            
            // Check if the bot is currently extracting or wants to extract via SAIN
            if (objectiveManager.IsAllowedToTakeABreak() && objectiveManager.BotMonitor.IsTryingToExtract())
            {
                objectiveManager.StopQuesting();

                LoggingController.LogWarning("Bot " + BotOwner.GetText() + " wants to extract and will no longer quest.");
                return updatePreviousState(false);
            }

            // Prevent the bot from following its boss if it needs to heal, etc. 
            if (!IsAbleBodied())
            {
                return updatePreviousState(false);
            }

            if (IsInCombat())
            {
                return updatePreviousState(pauseLayer());
            }
            // // Check if any of the bot's group members are in combat
            // // NOTE: This check MUST be performed after updating this bot's combate state!
            // if (objectiveManager.IsAllowedToTakeABreak() && BotHiveMindMonitor.GetValueForGroup(BotHiveMindSensorType.InCombat, BotOwner))
            // {
            //     // WIP. Hopefully not needed with SAIN.
            //     //BotHiveMindMonitor.AssignTargetEnemyFromGroup(BotOwner);

            //     //IReadOnlyCollection<BotOwner> groupMembers = BotHiveMindMonitor.GetAllGroupMembers(BotOwner);
            //     //LoggingController.LogInfo("One of the following group members is in combat: " + string.Join(", ", groupMembers.Select(g => g.GetText())));

            //     return updatePreviousState(false);
            // }

            // Check if the bot has wandered too far from its followers.
            if (objectiveManager.IsAllowedToTakeABreak() && objectiveManager.BotMonitor.ShouldWaitForFollowers())
            {
                followersTooFarTimer.Start();
            }
            else
            {
                followersTooFarTimer.Reset();
            }

            // If the bot has wandered too far from its followers for long enough, regroup with them
            if (followersTooFarTimer.ElapsedMilliseconds > ConfigController.Config.Questing.BotQuestingRequirements.MaxFollowerDistance.MaxWaitTime * 1000)
            {
                setNextAction(BotActionType.Regroup, "Regroup");
                return updatePreviousState(true);
            }

            // Check if the bot has been stuck too many times. The counter resets whenever the bot successfully completes an objective. 
            if (objectiveManager.StuckCount >= ConfigController.Config.Questing.StuckBotDetection.MaxCount)
            {
                LoggingController.LogWarning("Bot " + BotOwner.GetText() + " was stuck " + objectiveManager.StuckCount + " times and likely is unable to quest.");
                objectiveManager.StopQuesting();
                BotOwner.Mover.Stop();
                return updatePreviousState(false);
            }

            // Check if the bot needs to complete its assignment
            if (!objectiveManager.IsJobAssignmentActive)
            {
                return updatePreviousState(pauseLayer());
            }

            // Determine what type of action is needed for the bot to complete its assignment
            switch (objectiveManager.CurrentQuestAction)
            {
                case QuestAction.MoveToPosition:
                    if (objectiveManager.MustUnlockDoor)
                    {
                        string interactiveObjectShortID = objectiveManager.GetCurrentQuestInteractiveObject().Id.Abbreviate();
                        setNextAction(BotActionType.UnlockDoor, "UnlockDoor (" + interactiveObjectShortID + ")");
                    }
                    else
                    {
                        setNextAction(BotActionType.GoToObjective, "GoToObjective");
                    }
                    return updatePreviousState(true);

                case QuestAction.PlantItem:
                    if (!objectiveManager.IsCloseToObjective())
                    {
                        setNextAction(BotActionType.GoToObjective, "GoToPlantPosition");
                    }
                    else
                    {
                        setNextAction(BotActionType.PlantItem, "PlantItem (" + objectiveManager.MinElapsedActionTime + "s)");
                    }
                    return updatePreviousState(true);

                case QuestAction.ToggleSwitch:
                    setNextAction(BotActionType.ToggleSwitch, "ToggleSwitch");
                    return updatePreviousState(true);
                
                case QuestAction.RequestExtract:
                    if (objectiveManager.BotMonitor.TryInstructBotToExtract())
                    {
                        objectiveManager.StopQuesting();
                    }
                    objectiveManager.CompleteObjective();
                    return updatePreviousState(true);
            }

            // Failsafe
            return updatePreviousState(false);
        }
    }
}
