using System.Collections.Generic;
using System.Linq;

using Comfort.Common;

using EFT;
using EFT.Interactive;
using EFT.InventoryLogic;

using UnityEngine;

using GridClass = StashGrid;
using GridClassEx = GridContainer;
using GridManagerClass = OperationResult19;
using SortResultStruct = SOperationResult2<OperationResult19>;
using GridItemClass = GItem13;
using ItemAddressExClass = GridItemAddress;
using SortErrorClass = GInventoryError18;
using GridCacheClass = GClass1385;

namespace LootingBots.Patch.Util
{
    public static class LootUtils
    {
        public static LayerMask LowPolyMask = LayerMask.GetMask(new string[] { "LowPolyCollider" });
        public static LayerMask LootMask = LayerMask.GetMask(
            new string[] { "Interactive", "Loot", "Deadbody" }
        );
        public static int RESERVED_SLOT_COUNT = 2;

        /** Calculate the size of a container */
        public static int GetContainerSize(GItem1 container)
        {
            GridClass[] grids = container.Grids;
            int gridSize = 0;

            foreach (GridClass grid in grids)
            {
                gridSize += grid.GridHeight.Value * grid.GridWidth.Value;
            }

            return gridSize;
        }

        // Prevents bots from looting single use quest keys like "Unknown Key"
        public static bool IsSingleUseKey(Item item)
        {
            KeyComponent key = item.GetItemComponent<KeyComponent>();
            return key != null && key.Template.MaximumNumberOfUsage == 1;
        }

        /** Triggers a container to open/close */
        public static void InteractContainer(LootableContainer container, EInteractionType action)
        {
            InteractionResult result = new InteractionResult(action);
            container.Interact(result);
        }

        /**
        * Reference fn: GClass2584.Sort
        * Sorts the items in a container and places them in grid spaces that match their exact size before moving on to a bigger slot size. This helps make more room in the container for items to be placed in
        */
        public static SortResultStruct SortContainer(
            GItem1 container,
            InventoryController controller
        )
        {
            if (container != null)
            {
                List<object> newLocations = new List<object>();
                GridManagerClass gridManager = new GridManagerClass(container, controller);
                List<Item> itemsInContainer = new List<Item>();

                // Remove positions of all loot
                foreach (var grid in container.Grids)
                {
                    gridManager.SetOldPositions(grid, grid.ItemCollection.ToListOfLocations());
                    itemsInContainer.AddRange(grid.Items);
                    grid.RemoveAll();
                    Singleton<GridCacheClass>.Instance.Set(
                        container.Owner.ID,
                        grid as GridClassEx,
                        new string[] { }
                    );
                    controller.RaiseEvent(new GEventArgs23(grid));
                }

                // Sort items in container from largest to smallest
                itemsInContainer.Sort(
                    (item1, item2) => item2.GetItemSize().CompareTo(item1.GetItemSize())
                );

                // Sort grids in the container from smallest to largest
                var sortedGrids = SortGrids(container.Grids);

                // Go through each item and try to find a spot in the container for it. Since items are sorted largest to smallest and grids sorted from smallest to largest,
                // this should ensure that items prefer to be in slots that match their size, instead of being placed in a larger grid spots
                foreach (Item item in itemsInContainer)
                {
                    bool foundPlace = false;

                    // Go through each grid slot and try to add the item
                    foreach (var grid in sortedGrids)
                    {
                        if (!grid.Add(item).Failed)
                        {
                            foundPlace = true;
                            gridManager.AddItemToGrid(
                                grid,
                                new GridItemClass(
                                    item,
                                    ((ItemAddressExClass)item.CurrentAddress).LocationInGrid
                                )
                            );
                            Singleton<GridCacheClass>.Instance.Add(
                                container.Owner.ID,
                                grid as GridClassEx,
                                item
                            );
                            break;
                        }
                    }

                    if (!foundPlace)
                    {
                        // Sorting has failed! Rollback state of rig
                        gridManager.RollBack();
                        LootingBots.LootLog.LogError("Sort Failed");
                        return new SortErrorClass(item);
                    }
                }
                return gridManager;
            }

            LootingBots.LootLog.LogError("No container!");
            return new SortErrorClass(null);
        }

        // Sort grids in the container from smallest to largest
        public static GridClass[] SortGrids(GridClass[] grids)
        {
            // Sort grids in the container from smallest to largest
            var containerGrids = grids.ToList();
            containerGrids.Sort(
                (grid1, grid2) =>
                {
                    var grid1Size = grid1.GridHeight.Value * grid1.GridWidth.Value;
                    var grid2Size = grid2.GridHeight.Value * grid2.GridWidth.Value;
                    return grid1Size.CompareTo(grid2Size);
                }
            );
            return containerGrids.ToArray();
        }

        /**
        * Calculates the amount of empty grid slots in the container
        */
        public static int GetAvailableGridSlots(GridClass[] grids)
        {
            if (grids == null)
            {
                grids = new GridClass[] { };
            }

            List<GridClass> gridList = grids.ToList();
            return gridList.Aggregate(
                0,
                (freeSpaces, grid) =>
                {
                    int gridSize = grid.GridHeight.Value * grid.GridWidth.Value;
                    int containedItemSize = grid.GetSizeOfContainedItems();
                    freeSpaces += gridSize - containedItemSize;
                    return freeSpaces;
                }
            );
        }

        /**
        * Returns an array of available grid slots, omitting 1 free 1x2 slot. This is to ensure no loot is placed in this slot and the grid space is only used for reloaded mags
        */
        public static GridClass[] Reserve2x1Slot(GridClass[] grids)
        {
            List<GridClass> gridList = grids.ToList();
            foreach (var grid in gridList)
            {
                int gridSize = grid.GridHeight.Value * grid.GridWidth.Value;
                bool isLargeEnough = gridSize >= RESERVED_SLOT_COUNT;

                // If the grid is larger than 2 spaces, and the amount of free space in the grid is greater or equal to 2
                // reserve the grid as a place where the bot can place reloaded mags
                if (isLargeEnough && gridSize - grid.GetSizeOfContainedItems() >= 2)
                {
                    gridList.Remove(grid);
                    return gridList.ToArray();
                }
            }

            return gridList.ToArray();
        }

        /** Return the amount of spaces taken up by all the items in a given grid slot */
        public static int GetSizeOfContainedItems(this GridClass grid)
        {
            return grid.Items.Aggregate(0, (sum, item2) => sum + item2.GetItemSize());
        }

        /** Gets the size of an item in a grid */
        public static int GetItemSize(this Item item)
        {
            var dimensions = item.CalculateCellSize();
            return dimensions.X * dimensions.Y;
        }

        // Custom extension for EFT InventoryController.FindGridToPickUp that uses a custom method for choosing the grid slot to place a loot item
        public static ItemAddressExClass FindGridToPickUp(
            this InventoryController controller,
            Item item,
            IEnumerable<GridClass> grids = null
        )
        {
            var prioritzedGrids =
                grids ?? controller.Inventory.Equipment.GetPrioritizedGridsForLoot(item);
            foreach (var grid in prioritzedGrids)
            {
                var address = grid.FindFreeSpace(item);
                if (address != null)
                {
                    return new ItemAddressExClass(grid, address);
                }
            }

            return null;
        }

        // Custom extension for EFT Equipment.GetPrioritizedGridsForLoot which sorts the tacVest/backpack and reserves a 1x2 grid slot in the tacvest before finding an available grid space for loot
        public static IEnumerable<GridClass> GetPrioritizedGridsForLoot(
            this Equipment equipment,
            Item item
        )
        {
            GItem1 tacVest = (GItem1)
                equipment.GetSlot(EquipmentSlot.TacticalVest).ContainedItem;
            GItem1 backpack = (GItem1)
                equipment.GetSlot(EquipmentSlot.Backpack).ContainedItem;
            GItem1 pockets = (GItem1)
                equipment.GetSlot(EquipmentSlot.Pockets).ContainedItem;
            GItem1 secureContainer = (GItem1)
                equipment.GetSlot(EquipmentSlot.SecuredContainer).ContainedItem;

            GridClass[] tacVestGrids = new GridClass[0];
            if (tacVest != null)
            {
                var sortedGrids = SortGrids(tacVest.Grids);
                tacVestGrids = Reserve2x1Slot(sortedGrids);
            }

            GridClass[] backpackGrids =
                (backpack != null) ? SortGrids(backpack.Grids) : new GridClass[0];
            GridClass[] pocketGrids = (pockets != null) ? pockets.Grids : new GridClass[0];
            GridClass[] secureContainerGrids =
                (secureContainer != null) ? secureContainer.Grids : new GridClass[0];

            if (item is BulletClass || item is MagazineClass)
            {
                return tacVestGrids
                    .Concat(pocketGrids)
                    .Concat(backpackGrids)
                    .Concat(secureContainerGrids);
            }
            else if (item is ThrowWeap)
            {
                return pocketGrids
                    .Concat(tacVestGrids)
                    .Concat(backpackGrids)
                    .Concat(secureContainerGrids);
            }
            else
            {
                return backpackGrids
                    .Concat(tacVestGrids)
                    .Concat(pocketGrids)
                    .Concat(secureContainerGrids);
            }
        }
    }
}