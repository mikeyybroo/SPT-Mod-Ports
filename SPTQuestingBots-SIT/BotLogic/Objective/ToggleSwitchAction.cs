﻿using System;
using System.Collections.Generic;
using System.Linq;
using System.Runtime.CompilerServices;
using System.Text;
using System.Threading.Tasks;
using EFT;
using EFT.Interactive;
using SPTQuestingBots.Controllers;
using SPTQuestingBots.Helpers;
using UnityEngine;
using UnityEngine.AI;

namespace SPTQuestingBots.BotLogic.Objective
{
    public class ToggleSwitchAction : BehaviorExtensions.GoToPositionAbstractAction
    {
        public ToggleSwitchAction(BotOwner _BotOwner) : base(_BotOwner, 100)
        {
            
        }

        public override void Start()
        {
            base.Start();

            BotOwner.PatrollingData.Pause();
        }

        public override void Stop()
        {
            base.Stop();

            BotOwner.PatrollingData.Unpause();
        }

        public override void Update()
        {
            UpdateBotMovement(CanSprint);
            UpdateBotSteering();

            // Don't allow expensive parts of this behavior to run too often
            if (!canUpdate())
            {
                return;
            }

            if (ObjectiveManager.GetCurrentQuestInteractiveObject() == null)
            {
                LoggingController.LogError("Cannot toggle a null switch");

                ObjectiveManager.FailObjective();

                return;
            }

            if (!ObjectiveManager.Position.HasValue)
            {
                LoggingController.LogError("Cannot go to a null position");

                ObjectiveManager.FailObjective();

                return;
            }

            ObjectiveManager.StartJobAssigment();

            if (ObjectiveManager.GetCurrentQuestInteractiveObject().DoorState == EDoorState.Open)
            {
                LoggingController.LogWarning("Switch " + ObjectiveManager.GetCurrentQuestInteractiveObject().Id + " is already open");

                ObjectiveManager.CompleteObjective();

                return;
            }

            if (ObjectiveManager.GetCurrentQuestInteractiveObject().DoorState == EDoorState.Locked)
            {
                LoggingController.LogWarning("Switch " + ObjectiveManager.GetCurrentQuestInteractiveObject().Id + " is unavailable");

                ObjectiveManager.TryChangeObjective();

                return;
            }

            if (checkIfBotIsStuck())
            {
                LoggingController.LogWarning(BotOwner.GetText() + " got stuck while trying to toggle switch " + ObjectiveManager.GetCurrentQuestInteractiveObject().Id + ". Giving up.");

                if (ObjectiveManager.TryChangeObjective())
                {
                    restartStuckTimer();
                }

                return;
            }

            // TO DO: Can this distance be reduced?
            float distanceToTargetPosition = Vector3.Distance(BotOwner.Position, ObjectiveManager.Position.Value);
            if (distanceToTargetPosition > 0.75f)
            {
                NavMeshPathStatus? pathStatus = RecalculatePath(ObjectiveManager.Position.Value);

                if (!pathStatus.HasValue || (pathStatus.Value != NavMeshPathStatus.PathComplete))
                {
                    LoggingController.LogWarning(BotOwner.GetText() + " cannot find a complete path to switch " + ObjectiveManager.GetCurrentQuestInteractiveObject().Id);

                    ObjectiveManager.FailObjective();

                    if (ConfigController.Config.Debug.ShowFailedPaths)
                    {
                        //drawBotPath(Color.yellow);
                    }
                }

                return;
            }

            if (ObjectiveManager.GetCurrentQuestInteractiveObject().DoorState == EDoorState.Shut)
            {
                BotOwner.ToggleSwitch(ObjectiveManager.GetCurrentQuestInteractiveObject(), EInteractionType.Open);
            }
            else
            {
                LoggingController.LogWarning("Somebody is already interacting with switch " + ObjectiveManager.GetCurrentQuestInteractiveObject().Id);
            }

            ObjectiveManager.CompleteObjective();
        }
    }
}
