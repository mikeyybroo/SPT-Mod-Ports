﻿using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using System.Text;
using System.Threading.Tasks;
using StayInTarkov;
using EFT;
using SPTQuestingBots.Controllers;

namespace SPTQuestingBots.Patches
{
    public class OnBeenKilledByAggressorPatch : ModulePatch
    {
        protected override MethodBase GetTargetMethod()
        {
            return typeof(Player).GetMethod("OnBeenKilledByAggressor", BindingFlags.Public | BindingFlags.Instance);
        }

        [PatchPostfix]
        private static void PatchPostfix(Player __instance, Player aggressor)
        {
            string message = __instance.GetText();
            message += " (" + (__instance.Side == EPlayerSide.Savage ? "Scav" : "PMC") + ")";

            message += " was killed by ";

            message += aggressor.GetText();
            message += " (" + (aggressor.Side == EPlayerSide.Savage ? "Scav" : "PMC") + ")";

            if (Controllers.Bots.BotGenerator.CanSpawnPMCs)
            {
                BotOwner[] aliveInitialPMCs = Controllers.Bots.BotGenerator.RemainingAliveInitialPMCs().ToArray();
                message += ". Initial PMC's remaining: " + (aliveInitialPMCs.Length - (aliveInitialPMCs.Any(p => p.Id == __instance.Id) ? 1 : 0));
            }

            LoggingController.LogInfo(message);

            // Make sure the bot doesn't have any active quests if it's dead
            Controllers.Bots.BotJobAssignmentFactory.FailAllJobAssignmentsForBot(__instance.Profile.Id);
        }
    }
}
