﻿using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using EFT;
using SPTQuestingBots.Controllers;

namespace SPTQuestingBots.BotLogic.Follow
{
    internal class FollowBossAction : BehaviorExtensions.GoToPositionAbstractAction
    {
        private bool wasStuck = false;

        public FollowBossAction(BotOwner _BotOwner) : base(_BotOwner)
        {

        }

        public override void Start()
        {
            base.Start();
        }

        public override void Stop()
        {
            base.Stop();
        }

        public override void Update()
        {
            UpdateBotMovement(CanSprint);
            UpdateBotSteering();

            // Don't allow expensive parts of this behavior (calculating a path to an objective) to run too often
            if (!canUpdate())
            {
                return;
            }

            // Only allow the bot to sprint if its boss is allowed to sprint
            BotOwner boss = HiveMind.BotHiveMindMonitor.GetBoss(BotOwner);
            // CanSprint = HiveMind.BotHiveMindMonitor.GetValueForBot(HiveMind.BotHiveMindSensorType.CanSprintToObjective, boss);

            RecalculatePath(boss.Position);

            // Check if the bot is unable to reach its boss. If so, fall back to the default EFT layer for a bit. 
            if (checkIfBotIsStuck())
            {
                if (!wasStuck)
                {
                    LoggingController.LogInfo("Follower " + BotOwner.GetText() + " is stuck and will take a break from following.");
                }
                wasStuck = true;

                ObjectiveManager.PauseRequest = ConfigController.Config.Questing.StuckBotDetection.FollowerBreakTime;
                restartStuckTimer();
            }
            else
            {
                wasStuck = false;
            }
        }
    }
}
