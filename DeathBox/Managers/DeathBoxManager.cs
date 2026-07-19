using DeathBox.Helpers;
using SDG.Unturned;
using Steamworks;
using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;

namespace DeathBox.Managers
{
    public class DeathBoxManager
    {
        private static readonly EItemType[] PriorityItemTypes =
        {
            EItemType.SHIRT,
            EItemType.PANTS,
            EItemType.BACKPACK,
            EItemType.VEST,
            EItemType.GUN
        };

        private readonly DeathBoxPlugin _plugin;
        private readonly Dictionary<Transform, Coroutine> _cooldowns = new Dictionary<Transform, Coroutine>();

        public DeathBoxManager(DeathBoxPlugin plugin)
        {
            _plugin = plugin;
        }

        private DeathBoxConfig Config => _plugin.Configuration.Instance;

        public void Load()
        {
            BarricadeManager.onDamageBarricadeRequested += OnDamageBarricadeRequested;
            PlayerEquipment.OnPunch_Global += OnPunchGlobal;
            PlayerLife.OnPreDeath += OnPlayerPreDeath;
        }

        public void Unload()
        {
            BarricadeManager.onDamageBarricadeRequested -= OnDamageBarricadeRequested;
            PlayerEquipment.OnPunch_Global -= OnPunchGlobal;
            PlayerLife.OnPreDeath -= OnPlayerPreDeath;

            foreach (KeyValuePair<Transform, Coroutine> pair in _cooldowns)
            {
                _plugin.StopCoroutine(pair.Value);
            }

            _cooldowns.Clear();
        }

        public void OnLevelLoaded(int _)
        {
            foreach (BarricadeRegion region in BarricadeManager.BarricadeRegions)
            {
                foreach (BarricadeDrop drop in region.drops.Where(x => x.asset.id == Config.DeathBoxID))
                {
                    _cooldowns[drop.model] = _plugin.StartCoroutine(DeathBoxCoroutine(drop.model, Config.DisappearCooldownAfterShutdown));
                }
            }
        }

        private void OnPlayerPreDeath(PlayerLife playerLife)
        {
            // Leave inventory alone so vanilla death drop can scatter loot.
            if (Config.SkipDeathBoxInVehicle && playerLife.player.movement.getVehicle() != null)
            {
                if (DeathBoxPlugin.DebugMode)
                {
                    Rocket.Core.Logging.Logger.Log("Skipped death box: player died in a vehicle");
                }

                return;
            }

            if (Config.SkipDeathBoxInSafezone && playerLife.player.movement.isSafe)
            {
                if (DeathBoxPlugin.DebugMode)
                {
                    Rocket.Core.Logging.Logger.Log("Skipped death box: player died in a safezone");
                }

                return;
            }

            if (BedProximityHelper.IsNearBed(playerLife.player.transform.position))
            {
                if (DeathBoxPlugin.DebugMode)
                {
                    Rocket.Core.Logging.Logger.Log("Skipped death box: player died near a bed");
                }

                return;
            }

            List<ItemJar> items = InventoryHelper.GetDeathBoxItems(playerLife, Config, DeathBoxPlugin.DebugMode);
            if (items.Count == 0)
            {
                return;
            }

            Transform barricadeTransform = BarricadeManager.dropNonPlantedBarricade(
                new Barricade(Assets.find(EAssetType.ITEM, Config.DeathBoxID) as ItemBarricadeAsset),
                playerLife.player.transform.position,
                Quaternion.LookRotation(LevelGround.getNormal(playerLife.player.transform.position)),
                0,
                0);

            InteractableStorage? storage = BarricadeManager.FindBarricadeByRootTransform(barricadeTransform)?.interactable as InteractableStorage;
            if (storage == null)
            {
                return;
            }

            storage.items.resize(Config.InitialDeathBoxHSize, 0);

            foreach (ItemJar itemJar in items)
            {
                byte x;
                byte y;
                byte rot;

                while (!storage.items.tryFindSpace(itemJar.size_x, itemJar.size_y, out x, out y, out rot))
                {
                    storage.items.resize(storage.items.width, (byte)(storage.items.height + 1));
                }

                storage.items.addItem(x, y, rot, itemJar.item);
            }

            _cooldowns[barricadeTransform] = _plugin.StartCoroutine(DeathBoxCoroutine(barricadeTransform, Config.NormalDisappearCooldown));
        }

        private void OnPunchGlobal(PlayerEquipment playerEquipment, EPlayerPunch punchType)
        {
            RaycastInfo raycastInfo = DamageTool.raycast(
                new Ray(playerEquipment.player.look.aim.position, playerEquipment.player.look.aim.forward),
                3,
                RayMasks.BARRICADE | RayMasks.BARRICADE_INTERACT,
                playerEquipment.player);
            BarricadeDrop drop = BarricadeManager.FindBarricadeByRootTransform(raycastInfo.transform);

            if (!Config.PunchUtil || drop == null || !_cooldowns.ContainsKey(drop.model))
            {
                return;
            }

            if (!(drop.interactable is InteractableStorage storage))
            {
                return;
            }

            Items storageItems = storage.items;
            IEnumerable<ItemJar> sortedItems = storageItems.items.OrderByDescending(x =>
            {
                ItemAsset? asset = Assets.find(EAssetType.ITEM, x.item.id) as ItemAsset;
                return asset != null && PriorityItemTypes.Contains(asset.type);
            });

            foreach (ItemJar itemJar in sortedItems)
            {
                if (DeathBoxPlugin.DebugMode)
                {
                    Rocket.Core.Logging.Logger.Log($"Item Added: {Assets.find(EAssetType.ITEM, itemJar.item.id).name}");
                }

                if (!playerEquipment.player.inventory.tryAddItemAuto(itemJar.item, true, true, true, false))
                {
                    if (Config.PunchUtil_DropWhenItemsDoesntFit)
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
                if (_cooldowns.TryGetValue(drop.model, out Coroutine cooldown))
                {
                    _plugin.StopCoroutine(cooldown);
                    _cooldowns.Remove(drop.model);
                }
            }
        }

        private void OnDamageBarricadeRequested(
            CSteamID instigatorSteamID,
            Transform barricadeTransform,
            ref ushort pendingTotalDamage,
            ref bool shouldAllow,
            EDamageOrigin damageOrigin)
        {
            try
            {
                if (!_cooldowns.TryGetValue(barricadeTransform, out Coroutine cooldown))
                {
                    return;
                }

                if (damageOrigin != EDamageOrigin.Unknown)
                {
                    shouldAllow = Config.CanDamageDeathBox;
                }

                if (!shouldAllow)
                {
                    return;
                }

                BarricadeDrop drop = BarricadeManager.FindBarricadeByRootTransform(barricadeTransform);
                if (drop == null)
                {
                    return;
                }

                if (drop.GetServersideData().barricade.health - pendingTotalDamage <= 0)
                {
                    _plugin.StopCoroutine(cooldown);
                    _cooldowns.Remove(barricadeTransform);
                }
            }
            catch (Exception ex)
            {
                Rocket.Core.Logging.Logger.LogException(ex, "OnDamageBarricadeRequested failed; allowing default damage behavior");
            }
        }

        private IEnumerator DeathBoxCoroutine(Transform barricadeTransform, int timer)
        {
            yield return new WaitForSeconds(timer);
            BarricadeManager.tryGetRegion(barricadeTransform, out byte x, out byte y, out ushort plant, out BarricadeRegion region);
            BarricadeManager.destroyBarricade(region.drops[region.IndexOfBarricadeByRootTransform(barricadeTransform)], x, y, plant);
            _cooldowns.Remove(barricadeTransform);

            if (DeathBoxPlugin.DebugMode)
            {
                Rocket.Core.Logging.Logger.Log("Coroutine Exited");
            }
        }
    }
}
