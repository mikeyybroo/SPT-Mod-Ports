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
    public class BotOwnerCreatePatch : ModulePatch
    {
        protected override MethodBase GetTargetMethod()
        {
            return typeof(BotOwner).GetMethod("Create", BindingFlags.Public | BindingFlags.Static);
        }

        [PatchPostfix]
        private static void PatchPostfix(BotOwner __result)
        {
            Controllers.Bots.BotRegistrationManager.WriteMessageForNewBotSpawn(__result);

            // TO DO: Is the code below even needed?
            IReadOnlyCollection<BotOwner> friends = Controllers.Bots.BotGenerator.GetSpawnGroupMembers(__result);
            foreach (BotOwner friend in friends)
            {
                Player player = friend.GetPlayer;
                if (!__result.EnemiesController.IsEnemy(player))
                {
                    continue;
                }

                Controllers.LoggingController.LogInfo(friend.GetText() + " is now friends with " + __result.GetText());
                __result.EnemiesController.Remove(player);
            }
        }
    }
}
