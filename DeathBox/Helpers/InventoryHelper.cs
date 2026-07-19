using SDG.Unturned;
using System;
using System.Collections.Generic;

namespace DeathBox.Helpers
{
    public static class InventoryHelper
    {
        public static List<ItemJar> GetDeathBoxItems(PlayerLife playerLife, DeathBoxConfig config, bool debugMode)
        {
            List<ItemJar> items = new List<ItemJar>();

            for (byte page = 0; page < PlayerInventory.PAGES - 2; page++)
            {
                if (playerLife.player.inventory.items[page] == null)
                {
                    continue;
                }

                while (playerLife.player.inventory.getItemCount(page) != 0)
                {
                    ItemJar inventoryItem = playerLife.player.inventory.items[page].items[0];
                    AddItemIfAllowed(items, inventoryItem.item, config, debugMode);
                    playerLife.player.inventory.items[page].removeItem(0);
                }
            }

            // Vehicle turret guns are temporarily equipped (equipment.isTurret) and are not
            // player-owned inventory — do not put them in the death box.
            if (playerLife.player.equipment.itemID != 0 && !playerLife.player.equipment.isTurret)
            {
                AddItemIfAllowed(
                    items,
                    new Item(playerLife.player.equipment.itemID, 1, playerLife.player.equipment.quality, playerLife.player.equipment.state),
                    config,
                    debugMode);
            }
            else if (debugMode && playerLife.player.equipment.isTurret)
            {
                Rocket.Core.Logging.Logger.Log($"Skipped vehicle turret equipment id {playerLife.player.equipment.itemID}");
            }

            AddClothingIfEquipped(
                items,
                playerLife.player.clothing.backpack,
                playerLife.player.clothing.backpackQuality,
                playerLife.player.clothing.backpackState,
                () =>
                {
                    playerLife.player.clothing.thirdClothes.backpack = 0;
                    playerLife.player.clothing.askWearBackpack(0, 0, new byte[0], true);
                },
                config,
                debugMode);

            AddClothingIfEquipped(
                items,
                playerLife.player.clothing.vest,
                playerLife.player.clothing.vestQuality,
                playerLife.player.clothing.vestState,
                () =>
                {
                    playerLife.player.clothing.thirdClothes.vest = 0;
                    playerLife.player.clothing.askWearVest(0, 0, new byte[0], true);
                },
                config,
                debugMode);

            AddClothingIfEquipped(
                items,
                playerLife.player.clothing.shirt,
                playerLife.player.clothing.shirtQuality,
                playerLife.player.clothing.shirtState,
                () =>
                {
                    playerLife.player.clothing.thirdClothes.shirt = 0;
                    playerLife.player.clothing.askWearShirt(0, 0, new byte[0], true);
                },
                config,
                debugMode);

            AddClothingIfEquipped(
                items,
                playerLife.player.clothing.pants,
                playerLife.player.clothing.pantsQuality,
                playerLife.player.clothing.pantsState,
                () =>
                {
                    playerLife.player.clothing.thirdClothes.pants = 0;
                    playerLife.player.clothing.askWearPants(0, 0, new byte[0], true);
                },
                config,
                debugMode);

            AddClothingIfEquipped(
                items,
                playerLife.player.clothing.hat,
                playerLife.player.clothing.hatQuality,
                playerLife.player.clothing.hatState,
                () =>
                {
                    playerLife.player.clothing.thirdClothes.hat = 0;
                    playerLife.player.clothing.askWearHat(0, 0, new byte[0], true);
                },
                config,
                debugMode);

            AddClothingIfEquipped(
                items,
                playerLife.player.clothing.glasses,
                playerLife.player.clothing.glassesQuality,
                playerLife.player.clothing.glassesState,
                () =>
                {
                    playerLife.player.clothing.thirdClothes.glasses = 0;
                    playerLife.player.clothing.askWearGlasses(0, 0, new byte[0], true);
                },
                config,
                debugMode);

            AddClothingIfEquipped(
                items,
                playerLife.player.clothing.mask,
                playerLife.player.clothing.maskQuality,
                playerLife.player.clothing.maskState,
                () =>
                {
                    playerLife.player.clothing.thirdClothes.mask = 0;
                    playerLife.player.clothing.askWearMask(0, 0, new byte[0], true);
                },
                config,
                debugMode);

            return items;
        }

        private static void AddClothingIfEquipped(
            List<ItemJar> items,
            ushort itemId,
            byte quality,
            byte[] state,
            System.Action unequipAction,
            DeathBoxConfig config,
            bool debugMode)
        {
            if (itemId == 0)
            {
                return;
            }

            AddItemIfAllowed(items, new Item(itemId, 1, quality, state), config, debugMode);
            unequipAction?.Invoke();
        }

        private static void AddItemIfAllowed(List<ItemJar> items, Item item, DeathBoxConfig config, bool debugMode)
        {
            if (item == null)
            {
                return;
            }

            if (config.BlacklistedItemIds?.Contains(item.id) == true)
            {
                if (debugMode)
                {
                    Rocket.Core.Logging.Logger.Log($"Skipped blacklisted item id {item.id}");
                }

                return;
            }

            items.Add(new ItemJar(item));
        }
    }
}
