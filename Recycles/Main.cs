using Rocket.Core.Plugins;
using SDG.Unturned;
using Steamworks;
using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;

namespace DeathBox
{
    public class Main : RocketPlugin<Configuration>
    {
        public static bool DebugMode { get; set; }

        private static readonly EItemType[] PriorityItemTypes =
        {
            EItemType.SHIRT,
            EItemType.PANTS,
            EItemType.BACKPACK,
            EItemType.VEST,
            EItemType.GUN
        };

        public Dictionary<Transform, Coroutine> CooldownManager { get; private set; }

        protected override void Load()
        {
            CooldownManager = new Dictionary<Transform, Coroutine>();
            BarricadeManager.onDamageBarricadeRequested += DamageBarricadeRequestHandler;
            PlayerEquipment.OnPunch_Global += OnPunchGlobal;
            PlayerLife.OnPreDeath += OnPlayerPreDeath;

            if (Level.isLoaded)
            {
                OnLevelLoaded(1);
            }
            else
            {
                Level.onLevelLoaded += OnLevelLoaded;
            }
        }

        protected override void Unload()
        {
            Level.onLevelLoaded -= OnLevelLoaded;
            BarricadeManager.onDamageBarricadeRequested -= DamageBarricadeRequestHandler;
            PlayerEquipment.OnPunch_Global -= OnPunchGlobal;
            PlayerLife.OnPreDeath -= OnPlayerPreDeath;

            foreach (KeyValuePair<Transform, Coroutine> pair in CooldownManager)
            {
                StopCoroutine(pair.Value);
            }
        }

        private void OnLevelLoaded(int _)
        {
            foreach (BarricadeRegion region in BarricadeManager.BarricadeRegions)
            {
                foreach (BarricadeDrop drop in region.drops.Where(x => x.asset.id == Configuration.Instance.DeathBoxID))
                {
                    CooldownManager[drop.model] = StartCoroutine(DeathBoxCoroutine(drop.model, Configuration.Instance.DisappearCooldownAfterShutdown));
                }
            }
        }

        private void OnPlayerPreDeath(PlayerLife playerLife)
        {
            List<ItemJar> items = GetDeathBoxItems(playerLife);
            if (items.Count == 0)
            {
                return;
            }

            Transform barricadeTransform = BarricadeManager.dropNonPlantedBarricade(
                new Barricade(Assets.find(EAssetType.ITEM, Configuration.Instance.DeathBoxID) as ItemBarricadeAsset),
                playerLife.player.transform.position,
                Quaternion.LookRotation(LevelGround.getNormal(playerLife.player.transform.position)),
                0,
                0);

            InteractableStorage storage = BarricadeManager.FindBarricadeByRootTransform(barricadeTransform).interactable as InteractableStorage;
            storage.items.resize(Configuration.Instance.InitialDeathBoxHSize, 0);

            foreach (ItemJar itemJar in items)
            {
                while (!storage.items.tryFindSpace(itemJar.size_x, itemJar.size_y, out byte x, out byte y, out byte rot))
                {
                    storage.items.resize(storage.items.width, (byte)(storage.items.height + 1));
                }

                storage.items.addItem(x, y, rot, itemJar.item);
            }

            CooldownManager[barricadeTransform] = StartCoroutine(DeathBoxCoroutine(barricadeTransform, Configuration.Instance.NormalDisappearCooldown));
        }

        private List<ItemJar> GetDeathBoxItems(PlayerLife playerLife)
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
                    AddItemIfAllowed(items, inventoryItem.item);
                    playerLife.player.inventory.items[page].removeItem(0);
                }
            }

            if (playerLife.player.equipment.itemID != 0)
            {
                AddItemIfAllowed(items, new Item(playerLife.player.equipment.itemID, 1, playerLife.player.equipment.quality, playerLife.player.equipment.state));
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
                });

            AddClothingIfEquipped(
                items,
                playerLife.player.clothing.vest,
                playerLife.player.clothing.vestQuality,
                playerLife.player.clothing.vestState,
                () =>
                {
                    playerLife.player.clothing.thirdClothes.vest = 0;
                    playerLife.player.clothing.askWearVest(0, 0, new byte[0], true);
                });

            AddClothingIfEquipped(
                items,
                playerLife.player.clothing.shirt,
                playerLife.player.clothing.shirtQuality,
                playerLife.player.clothing.shirtState,
                () =>
                {
                    playerLife.player.clothing.thirdClothes.shirt = 0;
                    playerLife.player.clothing.askWearShirt(0, 0, new byte[0], true);
                });

            AddClothingIfEquipped(
                items,
                playerLife.player.clothing.pants,
                playerLife.player.clothing.pantsQuality,
                playerLife.player.clothing.pantsState,
                () =>
                {
                    playerLife.player.clothing.thirdClothes.pants = 0;
                    playerLife.player.clothing.askWearPants(0, 0, new byte[0], true);
                });

            AddClothingIfEquipped(
                items,
                playerLife.player.clothing.hat,
                playerLife.player.clothing.hatQuality,
                playerLife.player.clothing.hatState,
                () =>
                {
                    playerLife.player.clothing.thirdClothes.hat = 0;
                    playerLife.player.clothing.askWearHat(0, 0, new byte[0], true);
                });

            AddClothingIfEquipped(
                items,
                playerLife.player.clothing.glasses,
                playerLife.player.clothing.glassesQuality,
                playerLife.player.clothing.glassesState,
                () =>
                {
                    playerLife.player.clothing.thirdClothes.glasses = 0;
                    playerLife.player.clothing.askWearGlasses(0, 0, new byte[0], true);
                });

            AddClothingIfEquipped(
                items,
                playerLife.player.clothing.mask,
                playerLife.player.clothing.maskQuality,
                playerLife.player.clothing.maskState,
                () =>
                {
                    playerLife.player.clothing.thirdClothes.mask = 0;
                    playerLife.player.clothing.askWearMask(0, 0, new byte[0], true);
                });

            return items;
        }

        private void AddClothingIfEquipped(List<ItemJar> items, ushort itemId, byte quality, byte[] state, Action unequipAction)
        {
            if (itemId == 0)
            {
                return;
            }

            AddItemIfAllowed(items, new Item(itemId, 1, quality, state));
            unequipAction?.Invoke();
        }

        private void AddItemIfAllowed(List<ItemJar> items, Item item)
        {
            if (item == null)
            {
                return;
            }

            if (Configuration.Instance.BlacklistedItemIds?.Contains(item.id) == true)
            {
                if (DebugMode)
                {
                    Rocket.Core.Logging.Logger.Log($"Skipped blacklisted item id {item.id}");
                }

                return;
            }

            items.Add(new ItemJar(item));
        }

        private void OnPunchGlobal(PlayerEquipment playerEquipment, EPlayerPunch punchType)
        {
            RaycastInfo raycastInfo = DamageTool.raycast(new Ray(playerEquipment.player.look.aim.position, playerEquipment.player.look.aim.forward), 3, RayMasks.BARRICADE | RayMasks.BARRICADE_INTERACT, playerEquipment.player);
            BarricadeDrop drop = BarricadeManager.FindBarricadeByRootTransform(raycastInfo.transform);

            if (!Configuration.Instance.PunchUtil || drop == null || !CooldownManager.ContainsKey(drop.model))
            {
                return;
            }

            Items storageItems = (drop.interactable as InteractableStorage).items;
            IEnumerable<ItemJar> sortedItems = storageItems.items.OrderByDescending(x => PriorityItemTypes.Contains((Assets.find(EAssetType.ITEM, x.item.id) as ItemAsset).type));

            foreach (ItemJar itemJar in sortedItems)
            {
                if (DebugMode)
                {
                    Rocket.Core.Logging.Logger.Log($"Item Added: {Assets.find(EAssetType.ITEM, itemJar.item.id).name}");
                }

                if (!playerEquipment.player.inventory.tryAddItemAuto(itemJar.item, true, true, true, false))
                {
                    if (Configuration.Instance.PunchUtil_DropWhenItemsDoesntFit)
                    {
                        ItemManager.dropItem(itemJar.item, playerEquipment.transform.position, false, true, true);
                    }
                    else
                    {
                        continue;
                    }
                }

                storageItems.removeItem(storageItems.getIndex(itemJar.x, itemJar.y));
            }

            if (storageItems.getItemCount() == 0)
            {
                BarricadeManager.tryGetRegion(drop.model, out byte x, out byte y, out ushort plant, out BarricadeRegion region);
                BarricadeManager.destroyBarricade(drop, x, y, plant);
                StopCoroutine(CooldownManager[drop.model]);
                CooldownManager.Remove(drop.model);
            }
        }

        private void DamageBarricadeRequestHandler(CSteamID instigatorSteamID, Transform barricadeTransform, ref ushort pendingTotalDamage, ref bool shouldAllow, EDamageOrigin damageOrigin)
        {
            if (damageOrigin != EDamageOrigin.Unknown && CooldownManager.ContainsKey(barricadeTransform))
            {
                shouldAllow = Configuration.Instance.CanDamageDeathBox;
            }

            if (shouldAllow && BarricadeManager.FindBarricadeByRootTransform(barricadeTransform).GetServersideData().barricade.health - pendingTotalDamage <= 0)
            {
                StopCoroutine(CooldownManager[barricadeTransform]);
                CooldownManager.Remove(barricadeTransform);
            }
        }

        private IEnumerator DeathBoxCoroutine(Transform barricadeTransform, int timer)
        {
            yield return new WaitForSeconds(timer);
            BarricadeManager.tryGetRegion(barricadeTransform, out byte x, out byte y, out ushort plant, out BarricadeRegion region);
            BarricadeManager.destroyBarricade(region.drops[region.IndexOfBarricadeByRootTransform(barricadeTransform)], x, y, plant);
            CooldownManager.Remove(barricadeTransform);

            if (DebugMode)
            {
                Rocket.Core.Logging.Logger.Log("Coroutine Exited");
            }
        }
    }
}
