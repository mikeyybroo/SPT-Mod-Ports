﻿using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using System.Text;
using System.Threading.Tasks;
using Comfort.Common;
using EFT.Interactive;
using EFT.InventoryLogic;
using EFT;
using SPTQuestingBots.Controllers;

namespace SPTQuestingBots.Helpers
{
    public static class ItemHelpers
    {
        public static InventoryController GetInventoryController(this BotOwner bot)
        {
            Type playerType = typeof(Player);

            FieldInfo inventoryControllerField = playerType.GetField("_inventoryController", BindingFlags.NonPublic | BindingFlags.Instance);
            return (InventoryController)inventoryControllerField.GetValue(bot.GetPlayer);
        }

        public static bool TryTransferItem(this BotOwner botOwner, Item item)
        {
            try
            {
                InventoryController inventoryController = GetInventoryController(botOwner);

                // Enumerate all possible equipment slots into which the key can be transferred
                List<EquipmentSlot> possibleSlots = new List<EquipmentSlot>();
                if (Controllers.Bots.BotRegistrationManager.IsBotAPMC(botOwner))
                {
                    possibleSlots.Add(EquipmentSlot.SecuredContainer);
                }
                possibleSlots.AddRange(new EquipmentSlot[] { EquipmentSlot.Backpack, EquipmentSlot.TacticalVest, EquipmentSlot.ArmorVest, EquipmentSlot.Pockets });

                // Try to find an available grid in the equipment slots to which the key can be transferred
                ItemAddress locationForItem = botOwner.FindLocationForItem(item, possibleSlots, inventoryController);
                if (locationForItem == null)
                {
                    LoggingController.LogError("Cannot find any location to put key " + item.LocalizedName() + " for " + botOwner.GetText());
                    return false;
                }

                // Initialize the transation to transfer the key to the bot
                SOperationResult1<AddResult> moveResult = ItemMovementHandler.Add(item, locationForItem, inventoryController, true);
                if (!moveResult.Succeeded)
                {
                    LoggingController.LogError("Cannot move key " + item.LocalizedName() + " to inventory of " + botOwner.GetText());
                    return false;
                }

                Action<IResult> callbackAction = (result) => 
                {
                    if (result.Succeed)
                    {
                        LoggingController.LogInfo("Moved key to inventory of " + botOwner.GetText());
                    }

                    if (result.Failed)
                    {
                        LoggingController.LogError("Could not move key to inventory of " + botOwner.GetText());
                    }
                };

                // Execute the transation to transfer the key to the bot
                Callback callback = new Callback(callbackAction);
                inventoryController.TryRunNetworkTransaction(moveResult, callback);

                return true;
            }
            catch (Exception e)
            {
                LoggingController.LogError(e.Message);
                LoggingController.LogError(e.StackTrace);

                throw;
            }
        }

        public static ItemAddress FindLocationForItem(this BotOwner botOwner, Item item, IEnumerable<EquipmentSlot> possibleSlots, InventoryController botInventoryController)
        {
            foreach (EquipmentSlot slot in possibleSlots)
            {
                //LoggingController.LogInfo("Checking " + slot.ToString() + " for " + BotOwner.GetText() + "...");

                // Search through all grids in the equipment slot
                GItem2 equipmentSlot = botInventoryController.Inventory.Equipment.GetSlot(slot).ContainedItem as GItem2;
                foreach (StashGrid grid in (equipmentSlot?.Grids ?? (new StashGrid[0])))
                {
                    //LoggingController.LogInfo("Checking grid " + grid.ID + " (" + grid.GridWidth.Value + "x" + grid.GridHeight.Value + ") in " + slot.ToString() + " for " + BotOwner.GetText() + "...");

                    // Check if the grid has enough free space to fit the item
                    LocationInGrid locationInGrid = grid.FindFreeSpace(item);
                    if (locationInGrid != null)
                    {
                        LoggingController.LogInfo(botOwner.GetText() + " will receive " + item.LocalizedName() + " in its " + slot.ToString() + "...");

                        return new GridItemAddress(grid, locationInGrid);
                    }
                }
            }

            return null;
        }

        public static bool IsBundleLoaded(this Item item)
        {
            try
            {
                IEasyBundle data = Singleton<IEasyAssets>.Instance.System.GetNode(item.Prefab.path).Data;

                if (data.LoadState.Value == Diz.DependencyManager.ELoadState.Loaded)
                {
                    return true;
                }
            }
            catch (Exception e)
            {
                LoggingController.LogError(e.Message);
                LoggingController.LogError(e.StackTrace);

                throw;
            }

            return false;
        }

        public static DependencyGraph<IEasyBundle>.Bundles LoadBundle(this Item item)
        {
            try
            {
                if (item == null)
                {
                    throw new ArgumentNullException(nameof(item));
                }

                if (item.Prefab.path.Length == 0)
                {
                    throw new InvalidOperationException("The prefab path for " + item.LocalizedName() + " is empty");
                }

                return Singleton<IEasyAssets>.Instance.Retain(new string[] { item.Prefab.path }, null, default);
            }
            catch (Exception e)
            {
                LoggingController.LogError(e.Message);
                LoggingController.LogError(e.StackTrace);

                throw;
            }
        }

        public static KeyComponent FindKeyComponent(this BotOwner botOwner, Door door)
        {
            try
            {
                InventoryController inventoryController = GetInventoryController(botOwner);

                IEnumerable<KeyComponent> matchingKeys = inventoryController.Inventory.Equipment
                    .GetItemComponentsInChildren<KeyComponent>(false)
                    .Where(k => k.Template.KeyId == door.KeyId);

                if (!matchingKeys.Any())
                {
                    return null;
                }

                return matchingKeys.First();
            }
            catch (Exception e)
            {
                LoggingController.LogError(e.Message);
                LoggingController.LogError(e.StackTrace);

                throw;
            }
        }
    }
}
