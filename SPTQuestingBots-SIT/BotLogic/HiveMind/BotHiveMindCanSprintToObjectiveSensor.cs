﻿using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using EFT;

namespace SPTQuestingBots.BotLogic.HiveMind
{
    public class BotHiveMindCanSprintToObjectiveSensor: BotHiveMindAbstractSensor
    {
        public BotHiveMindCanSprintToObjectiveSensor() : base(true)
        {

        }

        public override void Update(Action<BotOwner> additionalAction = null)
        {
            Action<BotOwner> updateFromObjectiveManager = new Action<BotOwner>((bot) =>
            {
                Objective.BotObjectiveManager objectiveManager = Objective.BotObjectiveManager.GetObjectiveManagerForBot(bot);
                if (objectiveManager != null )
                {
                    botState[bot] = objectiveManager.CanSprintToObjective();
                }
                else
                {
                    botState[bot] = defaultValue;
                }
            });

            base.Update(updateFromObjectiveManager);
        }
    }
}
