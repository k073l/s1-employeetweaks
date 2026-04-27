using System.Collections;
using EmployeeTweaks.Helpers;
using HarmonyLib;
using MelonLoader;
using UnityEngine;
#if MONO
using FishNet;
using ScheduleOne.Employees;
using ScheduleOne.EntityFramework;
using ScheduleOne.ItemFramework;
using ScheduleOne.NPCs.Behaviour;
using ScheduleOne.ObjectScripts;
using ScheduleOne.Tiles;
#else
using Il2CppFishNet;
using Il2CppScheduleOne.Employees;
using Il2CppScheduleOne.EntityFramework;
using Il2CppScheduleOne.ItemFramework;
using Il2CppScheduleOne.NPCs.Behaviour;
using Il2CppScheduleOne.ObjectScripts;
using Il2CppScheduleOne.Tiles;
#endif

namespace EmployeeTweaks.Patches.BotanistSprinklersPourer;

[HarmonyPatch(typeof(GrowContainerBehaviour))]
internal class GrowContainerBehaviourPatch
{
    [HarmonyPatch(nameof(GrowContainerBehaviour.OnActiveTick))]
    [HarmonyPostfix]
    private static void UseSprinklerOrPourer(GrowContainerBehaviour __instance)
    {
        if (!InstanceFinder.IsServer) return;
        if (__instance._currentState != GrowContainerBehaviour.EState.PerformingAction) return;
        if (!__instance.IsAtGrowContainer()) return;
        if (Utils.Is<WaterPotBehaviour>(__instance, out var waterPotBehaviour) && waterPotBehaviour != null)
        {
            if (!Utils.Is<Pot>(waterPotBehaviour._growContainer, out var pot)) return;

            var itemsAroundPot = GetItemsAroundItem(pot);
            MelonDebug.Msg("Found " + itemsAroundPot.Count + " items around the pot");
            foreach (var item in itemsAroundPot)
            {
                if (!Utils.Is<Sprinkler>(item, out var sprinkler) || sprinkler == null) continue;
                var sprinklerEligiblePots = sprinkler.GetPots();
                if (sprinklerEligiblePots.AsEnumerable().FirstOrDefault(x => x != null && x == pot) == null) continue;
                MelonDebug.Msg("It's an eligible sprinkler, activating it");
                if (!sprinkler.IsSprinkling)
                    sprinkler.Interacted();
                waterPotBehaviour.OnStopPerformAction();
                waterPotBehaviour.OnActionSuccess(null);
                waterPotBehaviour.Disable_Networked(null);
                if (Utils.Is<Botanist>(waterPotBehaviour.Npc, out var botanist))
                {
                    botanist?.SetIdle(true);
                }

                break;
            }
        }

        if (Utils.Is<AddSoilToGrowContainerBehaviour>(__instance, out var addSoilToGrowContainer) &&
            addSoilToGrowContainer != null)
        {
            if (!Utils.Is<Pot>(addSoilToGrowContainer._growContainer, out var pot)) return;

            var itemsAroundPot = GetItemsAroundItem(pot);
            MelonDebug.Msg("Found " + itemsAroundPot.Count + " items around the pot");
            foreach (var item in itemsAroundPot)
            {
                if (!Utils.Is<SoilPourer>(item, out var soilPourer) || soilPourer == null) continue;
                var soilPourerEligiblePots = soilPourer.GetPots();
                if (soilPourerEligiblePots.AsEnumerable().FirstOrDefault(x => x != null && x == pot) == null) continue;
                MelonDebug.Msg("It's a eligible soil pourer, activating it");

                if (!addSoilToGrowContainer.AreTaskConditionsMetForContainer(pot))
                {
                    MelonDebug.Msg("Conditions not met");
                    addSoilToGrowContainer.Disable_Networked(null);
                    return;
                }

                ItemSlot itemSlot = null;
                if (addSoilToGrowContainer.DoesTaskRequireItem(pot, out var itemIDs))
                {
#if MONO
                    itemSlot = addSoilToGrowContainer.GetItemSlotContainingRequiredItem(
                        addSoilToGrowContainer._botanist.Inventory, itemIDs);
#else
                    if (!Utils.Is2<IItemSlotOwner>(addSoilToGrowContainer._botanist.Inventory, out var inventory))
                    {
                        MelonDebug.Error("Botanist inventory does not implement IItemSlotOwner");
                        addSoilToGrowContainer.Disable_Networked(null);
                        return;
                    }

                    itemSlot = addSoilToGrowContainer.GetItemSlotContainingRequiredItem(inventory, itemIDs);
#endif
                }

                var usedItem = itemSlot?.ItemInstance.GetCopy(1);
                if (usedItem == null) return;
                if (!Utils.Is<SoilDefinition>(usedItem.Definition, out var soilDefinition) || soilDefinition == null)
                {
                    MelonDebug.Msg("No soil in inventory");
                    addSoilToGrowContainer.Disable_Networked(null);
                    return;
                }

                if (addSoilToGrowContainer.CheckSuccess(usedItem))
                {
                    MelonCoroutines.Start(PourerRoutine(addSoilToGrowContainer, soilPourer, itemSlot, soilDefinition));
                }
                else
                    addSoilToGrowContainer.Disable_Networked(null);
            }
        }
    }

    private static System.Collections.Generic.HashSet<GridItem> GetItemsAroundItem(GridItem item)
    {
        var origin = new Coordinate(item._originCoordinate);

        var offsets = new System.Collections.Generic.List<Coordinate>();

        const int minX = -1;
        const int maxX = 2;
        const int minY = -1;
        const int maxY = 2;
        for (var x = minX; x <= maxX; x++)
        {
            offsets.Add(new Coordinate(x, minY)); // bottom row
            offsets.Add(new Coordinate(x, maxY)); // top row
        }

        for (var y = minY + 1; y <= maxY - 1; y++)
        {
            offsets.Add(new Coordinate(minX, y)); // left column
            offsets.Add(new Coordinate(maxX, y)); // right column
        }

        // rotate offsets
        var coords = offsets
            .Select(offset => origin + Coordinate.RotateCoordinates(offset, item._rotation))
            .ToList();

        var items = new System.Collections.Generic.HashSet<GridItem>();
        foreach (var coord in coords)
        {
            var tile = item.OwnerGrid.GetTile(coord);
            if (tile == null) continue;
            foreach (var occupant in tile.BuildableOccupants)
            {
                items.Add(occupant);
            }
        }

        return items;
    }

    private static IEnumerator PourerRoutine(AddSoilToGrowContainerBehaviour addSoilToGrowContainerBehaviour,
        SoilPourer soilPourer, ItemSlot slot, SoilDefinition soilDefinition)
    {
        var waitTime = addSoilToGrowContainerBehaviour.GetActionDuration() /
                       addSoilToGrowContainerBehaviour._botanist.CurrentWorkSpeed;
        waitTime *= 0.25f;
        soilPourer.SetSoil(null, soilDefinition.ID);
        yield return new WaitForSeconds(waitTime);
        if (!soilPourer.isDispensing)
            soilPourer.PourSoil();
        var usedItem = slot?.ItemInstance.GetCopy(1);
        addSoilToGrowContainerBehaviour.OnStopPerformAction();
        addSoilToGrowContainerBehaviour.OnActionSuccess(usedItem);
        if (slot is { Quantity: > 0 })
            slot.ChangeQuantity(-1);
        addSoilToGrowContainerBehaviour.Disable_Networked(null);
        if (Utils.Is<Botanist>(addSoilToGrowContainerBehaviour.Npc, out var botanist))
        {
            botanist?.SetIdle(true);
        }
    }
}