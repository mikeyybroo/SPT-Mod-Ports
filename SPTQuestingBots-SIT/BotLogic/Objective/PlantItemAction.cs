﻿using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using EFT;

namespace SPTQuestingBots.BotLogic.Objective
{
    public class PlantItemAction : BehaviorExtensions.GoToPositionAbstractAction
    {
        public PlantItemAction(BotOwner _BotOwner) : base(_BotOwner, 100)
        {
            SetBaseAction(AIActionNodeAssigner.CreateNode(BotLogicDecision.lay, BotOwner));
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
            UpdateBaseAction();
            
            // Don't allow expensive parts of this behavior to run too often
            if (!canUpdate())
            {
                return;
            }

            if (!ObjectiveManager.Position.HasValue)
            {
                throw new InvalidOperationException("Cannot go to a null position");
            }

            ObjectiveManager.StartJobAssigment();
            
            // This doesn't really need to be updated every frame
            CanSprint = IsAllowedToSprint();

            if (!ObjectiveManager.IsCloseToObjective())
            {
                RecalculatePath(ObjectiveManager.Position.Value);
                UpdateBotSteering();
                RestartActionElapsedTime();

                return;
            }
            restartStuckTimer();
            CheckMinElapsedActionTime();

            TryLookToLastCorner();
        }
    }
}
