using System.Collections;
using EmployeeTweaks.Helpers;
using EmployeeTweaks.Persistence;
using FishNet;
using FishNet.Object;
using HarmonyLib;
using MelonLoader;
using ScheduleOne.Employees;
using ScheduleOne.GameTime;
using ScheduleOne.ItemFramework;
using ScheduleOne.Management;
using ScheduleOne.NPCs.Behaviour;
using ScheduleOne.ObjectScripts;
using UnityEngine;
using Console = ScheduleOne.Console;

namespace EmployeeTweaks.Patches;

[HarmonyPatch(typeof(MoveItemBehaviour))]
internal static class MoveItemBehaviourPatches
{
    internal static void ManualPatchDestinationValid(HarmonyLib.Harmony harmony)
    {
        var method = AccessTools.Method(
            typeof(MoveItemBehaviour),
            nameof(MoveItemBehaviour.IsDestinationValid),
            [
                typeof(TransitRoute),
                typeof(ItemInstance),
                typeof(string).MakeByRefType()
            ]
        );

        if (method == null)
        {
            MelonLogger.Error("Failed to find IsDestinationValid method!");
            return;
        }

        harmony.Patch(
            method,
            prefix: new HarmonyMethod(typeof(MoveItemBehaviourPatches), nameof(IsDestinationValid))
        );

        var transitRouteValidIDMeth = AccessTools.Method(
            typeof(MoveItemBehaviour),
            nameof(MoveItemBehaviour.IsTransitRouteValid),
            [
                typeof(TransitRoute),
                typeof(string),
                typeof(string).MakeByRefType()
            ]);
        if (transitRouteValidIDMeth == null)
        {
            MelonLogger.Error("Failed to find IsTransitRouteValid method!");
            return;
        }

        harmony.Patch(
            transitRouteValidIDMeth,
            prefix: new HarmonyMethod(typeof(MoveItemBehaviourPatches), nameof(IsTransitRouteValidID))
        );
    }

    private static bool IsDestinationValid(MoveItemBehaviour __instance, TransitRoute route,
        ItemInstance item, ref string invalidReason, ref bool __result)
    {
        invalidReason = string.Empty;
        if (route?.Destination == null)
        {
            __result = false;
            return true;
        }

        var dest = route.Destination;
        var destIsStation = Utils.Is<PackagingStation>(dest, out var destStation) && destStation != null;
        var src = route.Source;
        var srcIsStation = Utils.Is<PackagingStation>(src, out var srcStation) && srcStation != null;
        if (!destIsStation && !srcIsStation)
        {
            __result = false;
            return true;
        }

        var station = destIsStation ? destStation : srcStation;

        UnpackageSave.Instance.UnpackageStations.TryGetValue(station.GUID, out var shouldUnpackage);
        if (!shouldUnpackage)
        {
            __result = false;
            return true;
        }

        if (!__instance.CanGetToDestination(route))
        {
            invalidReason = "Cannot get to destination!";
            __result = false;
            return false;
        }

        if (!__instance.CanGetToSource(route))
        {
            invalidReason = "Cannot get to source!";
            __result = false;
            return false;
        }

        if (dest.GetOutputCapacityForItem(item, __instance.Npc) == 0)
        {
            invalidReason = "Destination has no capacity for item!";
            __result = false;
            return false;
        }

        __result = true;
        return false;
    }


    private static bool IsTransitRouteValidID(MoveItemBehaviour __instance, TransitRoute route, string itemID,
        ref string invalidReason, ref bool __result)
    {
        invalidReason = string.Empty;
        if (route?.Destination == null || route.Source == null)
        {
            __result = false;
            return true;
        }

        var dest = route.Destination;
        var destIsStation = Utils.Is<PackagingStation>(dest, out var destStation) && destStation != null;
        var src = route.Source;
        var srcIsStation = Utils.Is<PackagingStation>(src, out var srcStation) && srcStation != null;

        if (!destIsStation && !srcIsStation) return true;
        var station = destIsStation ? destStation : srcStation;

        UnpackageSave.Instance.UnpackageStations.TryGetValue(station.GUID, out var shouldUnpackage);
        if (!shouldUnpackage)
        {
            __result = false;
            return true;
        }


        if (!route.AreEntitiesNonNull())
        {
            invalidReason = "Entities are null!";
            __result = false;
            return false;
        }

        var itemInstance = src.GetFirstSlotContainingItem(itemID,
                srcIsStation ? ITransitEntity.ESlotType.Input : ITransitEntity.ESlotType.Output)
            ?.ItemInstance;
        if (itemInstance == null || itemInstance.Quantity <= 0)
        {
            invalidReason = "Item is null or quantity is 0!";
            __result = false;
            return false;
        }

        if (!__instance.IsDestinationValid(route, itemInstance, out invalidReason))
        {
            __result = false;
            return false;
        }

        __result = true;
        return false;
    }


    [HarmonyPatch(nameof(MoveItemBehaviour.IsNpcInventoryItemValid))]
    [HarmonyPrefix]
    private static bool CheckNpcInventoryItemValid(MoveItemBehaviour __instance, ItemInstance item, ref bool __result)
    {
        if (__instance.assignedRoute?.Destination == null) return true;

        var dest = __instance.assignedRoute.Destination;
        var destIsStation = Utils.Is<PackagingStation>(dest, out var destStation) && destStation != null;
        var src = __instance.assignedRoute.Source;
        var srcIsStation = Utils.Is<PackagingStation>(src, out var srcStation) && srcStation != null;

        if (!destIsStation && !srcIsStation) return true;
        var station = destIsStation ? destStation : srcStation;
        UnpackageSave.Instance.UnpackageStations.TryGetValue(station.GUID, out var shouldUnpackage);
        if (!shouldUnpackage) return true;

        if (destIsStation)
        {
            if (dest.GetOutputCapacityForItem(item, __instance.Npc) == 0)
            {
                __result = false;
                return false;
            }
        }
        else
        {
            if (dest.GetInputCapacityForItem(item, __instance.Npc) == 0)
            {
                __result = false;
                return false;
            }
        }

        __result = true;
        return false;
    }

    [HarmonyPatch(nameof(MoveItemBehaviour.GetAmountToGrab))]
    [HarmonyPrefix]
    private static bool GetAmountToGrab(MoveItemBehaviour __instance, ref int __result)
    {
        if (__instance.assignedRoute?.Destination == null) return true;

        var dest = __instance.assignedRoute.Destination;
        var destIsStation = Utils.Is<PackagingStation>(dest, out var destStation) && destStation != null;
        var src = __instance.assignedRoute.Source;
        var srcIsStation = Utils.Is<PackagingStation>(src, out var srcStation) && srcStation != null;

        if (!destIsStation && !srcIsStation) return true;
        var station = destIsStation ? destStation : srcStation;
        UnpackageSave.Instance.UnpackageStations.TryGetValue(station.GUID, out var shouldUnpackage);
        if (!shouldUnpackage) return true;

        var itemInstance = __instance.assignedRoute.Source
            .GetFirstSlotContainingTemplateItem(__instance.itemToRetrieveTemplate,
                destIsStation ? ITransitEntity.ESlotType.Output : ITransitEntity.ESlotType.Input)
            ?.ItemInstance;
        if (itemInstance == null)
        {
            __result = 0;
            return false;
        }

        var quantity = itemInstance.Quantity;
        if (__instance.maxMoveAmount > 0)
            quantity = Mathf.Min(__instance.maxMoveAmount, quantity);
        int capacityForItem;
        if (destIsStation)
            capacityForItem = dest.GetOutputCapacityForItem(itemInstance, __instance.Npc);
        else
            capacityForItem = dest.GetInputCapacityForItem(itemInstance, __instance.Npc);
        __result = Mathf.Min(quantity, capacityForItem);
        return false;
    }

    [HarmonyPatch(nameof(MoveItemBehaviour.GrabItem))]
    [HarmonyPrefix]
    private static bool GrabItem(MoveItemBehaviour __instance)
    {
        if (__instance.assignedRoute?.Source == null) return true;

        var dest = __instance.assignedRoute.Destination;
        var destIsStation = Utils.Is<PackagingStation>(dest, out var destStation) && destStation != null;
        var src = __instance.assignedRoute.Source;
        var srcIsStation = Utils.Is<PackagingStation>(src, out var srcStation) && srcStation != null;
        if (!destIsStation && !srcIsStation) return true;
        var station = destIsStation ? destStation : srcStation;
        UnpackageSave.Instance.UnpackageStations.TryGetValue(station.GUID, out var shouldUnpackage);
        if (!shouldUnpackage) return true;
        if (destIsStation) return true;
        // srcIsStation

        if (__instance.beh.DEBUG_MODE)
            Console.Log("MoveItemBehaviour.GrabItem");
        __instance.currentState = MoveItemBehaviour.EState.Grabbing;
        __instance.grabRoutine = __instance.StartCoroutine(Routine());
        return false;

        IEnumerator Routine()
        {
            var sourceAccessPoint = __instance.GetSourceAccessPoint(__instance.assignedRoute);
            if (sourceAccessPoint == null)
            {
                Console.LogWarning("Could not find source access point!");
                __instance.grabRoutine = null;
                __instance.Disable_Networked(null);
            }
            else
            {
                __instance.Npc.Movement.FaceDirection(sourceAccessPoint.forward);
                __instance.Npc.SetAnimationTrigger_Networked(null, "GrabItem");
                if (!Utils.Is<Employee>(__instance.Npc, out var employee) || employee == null) yield break;
                var seconds = TimeManager.TickDuration / employee.CurrentWorkSpeed;
                yield return new WaitForSeconds(seconds);
                if (__instance.itemToRetrieveTemplate?.ID == null) yield break;
                if (!__instance.IsTransitRouteValid(__instance.assignedRoute, __instance.itemToRetrieveTemplate.ID,
                        out var invalidReason))
                {
                    Console.LogWarning(
                        $"{__instance.Npc.fullName} transit route no longer valid! Reason: {invalidReason}");
                    __instance.grabRoutine = null;
                    __instance.Disable_Networked(null);
                }
                else
                {
                    __instance.TakeItem();
                    yield return new WaitForSeconds(TimeManager.TickDuration);
                    __instance.grabRoutine = null;
                    __instance.currentState = MoveItemBehaviour.EState.Idle;
                }
            }
        }
    }

    [HarmonyPatch(nameof(MoveItemBehaviour.TakeItem))]
    [HarmonyPrefix]
    private static bool TakeItem(MoveItemBehaviour __instance)
    {
        if (__instance.assignedRoute?.Destination == null) return true;

        var dest = __instance.assignedRoute.Destination;
        var destIsStation = Utils.Is<PackagingStation>(dest, out var destStation) && destStation != null;
        var src = __instance.assignedRoute.Source;
        var srcIsStation = Utils.Is<PackagingStation>(src, out var srcStation) && srcStation != null;

        if (!destIsStation && !srcIsStation) return true;
        var station = destIsStation ? destStation : srcStation;
        UnpackageSave.Instance.UnpackageStations.TryGetValue(station.GUID, out var shouldUnpackage);
        if (!shouldUnpackage) return true;

        if (__instance.beh.DEBUG_MODE)
            Console.Log("MoveItemBehaviour.TakeItem");

        var amountToGrab = __instance.GetAmountToGrab();
        if (amountToGrab == 0)
        {
            Console.LogWarning("Amount to grab is 0!");
            return false;
        }

        var firstSlotContainingTemplateItem =
            __instance.assignedRoute.Source.GetFirstSlotContainingTemplateItem(__instance.itemToRetrieveTemplate,
                destIsStation ? ITransitEntity.ESlotType.Output : ITransitEntity.ESlotType.Input);
        if (firstSlotContainingTemplateItem?.ItemInstance == null) return false;
        var copy = firstSlotContainingTemplateItem.ItemInstance.GetCopy(amountToGrab);
        __instance.grabbedAmount = amountToGrab;
        firstSlotContainingTemplateItem.ChangeQuantity(-amountToGrab);
        __instance.Npc.Inventory.InsertItem(copy);
        if (destIsStation)
            dest.ReserveOutputSlotsForItem(copy, __instance.Npc.NetworkObject);
        else
            dest.ReserveInputSlotsForItem(copy, __instance.Npc.NetworkObject);
        return false;
    }

    [HarmonyPatch(nameof(MoveItemBehaviour.PlaceItem))]
    [HarmonyPrefix]
    private static bool PlaceItem(MoveItemBehaviour __instance)
    {
        if (__instance.assignedRoute?.Destination == null) return true;

        var dest = __instance.assignedRoute.Destination;
        var destIsStation = Utils.Is<PackagingStation>(dest, out var destStation) && destStation != null;
        var src = __instance.assignedRoute.Source;
        var srcIsStation = Utils.Is<PackagingStation>(src, out var srcStation) && srcStation != null;

        if (!destIsStation && !srcIsStation) return true;
        var station = destIsStation ? destStation : srcStation;
        UnpackageSave.Instance.UnpackageStations.TryGetValue(station.GUID, out var shouldUnpackage);
        if (!shouldUnpackage) return true;

        if (__instance.beh.DEBUG_MODE)
            Console.Log("MoveItemBehaviour.PlaceItem");
        __instance.currentState = MoveItemBehaviour.EState.Placing;
        __instance.placingRoutine = __instance.StartCoroutine(Routine());
        return false;

        IEnumerator Routine()
        {
            if (__instance.GetDestinationAccessPoint(__instance.assignedRoute) != null)
                __instance.Npc.Movement.FaceDirection(__instance.GetDestinationAccessPoint(__instance.assignedRoute)
                    .forward);
            __instance.Npc.SetAnimationTrigger_Networked(null, "GrabItem");
            if (!Utils.Is<Employee>(__instance.Npc, out var employee) || employee == null)
            {
                yield break;
            }

            var seconds = TimeManager.TickDuration / employee.CurrentWorkSpeed;
            yield return new WaitForSeconds(seconds);

            if (destIsStation)
                dest.RemoveOutputSlotLocks(__instance.Npc.NetworkObject);
            else
                dest.RemoveSlotLocks(__instance.Npc.NetworkObject);
            var firstIdenticalItem = __instance.Npc.Inventory.GetFirstIdenticalItem(__instance.itemToRetrieveTemplate);
            if (firstIdenticalItem != null && __instance.grabbedAmount > 0)
            {
                var copy = firstIdenticalItem.GetCopy(__instance.grabbedAmount);
                if (destIsStation)
                {
                    if (dest.GetOutputCapacityForItem(copy, __instance.Npc) >= __instance.grabbedAmount)
                    {
                        dest.InsertItemIntoOutput(copy, __instance.Npc);
                    }
                    else
                    {
                        Console.LogWarning(
                            "Destination does not have enough capacity for item! Attempting to return item to source.");
                        if (__instance.assignedRoute.Source.GetOutputCapacityForItem(copy, __instance.Npc) >=
                            __instance.grabbedAmount)
                            __instance.assignedRoute.Source.InsertItemIntoOutput(copy, __instance.Npc);
                        else
                            Console.LogError("Source does not have enough capacity for item! Item will be lost.");
                    }
                }
                else
                {
                    if (dest.GetInputCapacityForItem(copy, __instance.Npc) >= __instance.grabbedAmount)
                    {
                        dest.InsertItemIntoInput(copy, __instance.Npc);
                    }
                    else
                    {
                        Console.LogWarning(
                            "Destination does not have enough capacity for item! Attempting to return item to source.");
                        if (__instance.assignedRoute.Source.GetOutputCapacityForItem(copy, __instance.Npc) >=
                            __instance.grabbedAmount)
                            __instance.assignedRoute.Source.InsertItemIntoOutput(copy, __instance.Npc);
                        else
                            Console.LogError("Source does not have enough capacity for item! Item will be lost.");
                    }
                }

                firstIdenticalItem.ChangeQuantity(-__instance.grabbedAmount);
            }
            else
                Console.LogWarning("Could not find carried item to place!");

            yield return new WaitForSeconds(TimeManager.TickDuration);
            __instance.placingRoutine = null;
            __instance.currentState = MoveItemBehaviour.EState.Idle;
            __instance.Disable_Networked(null);
        }
    }

    [HarmonyPatch("StartTransit")]
    [HarmonyPrefix]
    private static bool StartTransit(MoveItemBehaviour __instance)
    {
        var dest = __instance.assignedRoute.Destination;
        var destIsStation = Utils.Is<PackagingStation>(dest, out var destStation) && destStation != null;
        var src = __instance.assignedRoute.Source;
        var srcIsStation = Utils.Is<PackagingStation>(src, out var srcStation) && srcStation != null;

        if (!destIsStation && !srcIsStation) return true;

        Console.Log("StartTransit called");
        Console.Log(
            $"{__instance.assignedRoute.Source.Name} ->  {__instance.assignedRoute.Destination.Name} ({__instance.itemToRetrieveTemplate?.ID}x{__instance.itemToRetrieveTemplate?.Quantity})");
        if (!InstanceFinder.IsServer)
        {
            return false;
        }

        if (__instance.Npc.Inventory.GetIdenticalItemAmount(__instance.itemToRetrieveTemplate) == 0)
        {
            if (__instance.itemToRetrieveTemplate?.ID == null) return false;
            if (!__instance.IsTransitRouteValid(__instance.assignedRoute, __instance.itemToRetrieveTemplate.ID,
                    out var invalidReason))
            {
                Console.LogWarning(
                    "Invalid transit route for move item behaviour by checking transit route!. Reason: " +
                    invalidReason);
                __instance.Disable_Networked(null);
                return false;
            }
        }
        else
        {
            ItemInstance firstIdenticalItem =
                __instance.Npc.Inventory.GetFirstIdenticalItem(__instance.itemToRetrieveTemplate,
                    __instance.IsNpcInventoryItemValid);
            if (__instance.Npc.Behaviour.DEBUG_MODE)
            {
                Console.Log("Moving item: " + firstIdenticalItem);
            }

            if (!__instance.IsDestinationValid(__instance.assignedRoute, firstIdenticalItem, out var invalidReason))
            {
                Console.LogWarning("Invalid transit route for move item behaviour by checking destination! Reason: " +
                                   invalidReason);
                __instance.Disable_Networked(null);
                return false;
            }
        }

        __instance.currentState = MoveItemBehaviour.EState.Idle;
        return false;
    }
}

public static class TransitEntityExtensions
{
    public static List<ItemSlot> ReserveOutputSlotsForItem(this ITransitEntity entity, ItemInstance item,
        NetworkObject locker)
    {
        List<ItemSlot> list = [];
        var num = item.Quantity;
        for (var i = 0; i < entity.OutputSlots.Count; i++)
        {
            var capacityForItem = entity.OutputSlots[i].GetCapacityForItem(item);
            if (capacityForItem != 0)
            {
                var num2 = Mathf.Min(capacityForItem, num);
                num -= num2;
                entity.OutputSlots[i].ApplyLock(locker, "Employee is about to place an item here");
                list.Add(entity.OutputSlots[i]);
                if (num <= 0)
                {
                    break;
                }
            }
        }

        return list;
    }

    public static void RemoveOutputSlotLocks(this ITransitEntity entity, NetworkObject locker)
    {
        for (var i = 0; i < entity.OutputSlots.Count; i++)
        {
            if (entity.OutputSlots[i].ActiveLock != null && entity.OutputSlots[i].ActiveLock.LockOwner == locker)
            {
                entity.OutputSlots[i].RemoveLock();
            }
        }
    }
}

[HarmonyPatch(typeof(AdvancedTransitRoute))]
internal static class AdvancedTransitRoutePatch
{
    [HarmonyPatch(nameof(AdvancedTransitRoute.GetItemReadyToMove))]
    [HarmonyPrefix]
    private static bool GetItemReadyToMove(AdvancedTransitRoute __instance, ref ItemInstance __result)
    {
        if (__instance.Source == null || __instance.Destination == null) return true;
        if (__instance.Destination == null) return true;

        var dest = __instance.Destination;
        var destIsStation = Utils.Is<PackagingStation>(dest, out var destStation) && destStation != null;
        var src = __instance.Source;
        var srcIsStation = Utils.Is<PackagingStation>(src, out var srcStation) && srcStation != null;

        if (!destIsStation && !srcIsStation) return true;
        var station = destIsStation ? destStation : srcStation;
        UnpackageSave.Instance.UnpackageStations.TryGetValue(station.GUID, out var shouldUnpackage);
        if (!shouldUnpackage) return true;

        if (destIsStation)
        {
            foreach (var outputSlot in __instance.Source.OutputSlots)
            {
                if (outputSlot.ItemInstance != null && __instance.Filter.DoesItemMeetFilter(outputSlot.ItemInstance))
                {
                    var outputCapacityForItem = dest.GetOutputCapacityForItem(outputSlot.ItemInstance);
                    if (outputCapacityForItem > 0)
                    {
                        __result = outputSlot.ItemInstance.GetCopy(Mathf.Min(outputCapacityForItem,
                            outputSlot.ItemInstance.Quantity));
                        return false;
                    }
                }
            }
        }
        else
        {
            foreach (var inputSlot in __instance.Source.InputSlots)
            {
                if (inputSlot.ItemInstance != null && __instance.Filter.DoesItemMeetFilter(inputSlot.ItemInstance))
                {
                    var inputCapacityForItem = dest.GetInputCapacityForItem(inputSlot.ItemInstance);
                    if (inputCapacityForItem > 0)
                    {
                        __result = inputSlot.ItemInstance.GetCopy(Mathf.Min(inputCapacityForItem,
                            inputSlot.ItemInstance.Quantity));
                        return false;
                    }
                }
            }
        }

        __result = null;
        return false;
    }
}

[HarmonyPatch(typeof(Packager))]
internal static class PackagerPatches
{
    [HarmonyWrapSafe]
    [HarmonyPatch(nameof(Packager.GetStationMoveItems))]
    [HarmonyPrefix]
    private static bool GetStationMoveItems(Packager __instance, ref PackagingStation __result)
    {
        foreach (var assignedStation in __instance.configuration.AssignedStations)
        {
            if (assignedStation != null && Utils.Is<PackagingStation>(assignedStation, out var station) &&
                station != null &&
                UnpackageSave.Instance.UnpackageStations.TryGetValue(station.GUID, out var shouldUnpackage) &&
                shouldUnpackage)
            {
                var inputSlots = station.InputSlots;
                if (!Utils.Is<PackagingStationConfiguration>(assignedStation.Configuration, out var config) ||
                    config == null)
                    continue;
                var isAnyRouteValid = inputSlots.Where(x => x != null)
                    .Select(x => x.ItemInstance)
                    .Where(x => x?.ID != null)
                    .Any(x => __instance.MoveItemBehaviour.IsTransitRouteValid(config.DestinationRoute, x.ID));
                if (inputSlots.Sum(x => x.Quantity) != 0 && isAnyRouteValid)
                {
                    __result = assignedStation;
                    return false;
                }
            }
            else
            {
                var outputSlot = assignedStation.OutputSlot;
                if (!Utils.Is<PackagingStationConfiguration>(assignedStation.Configuration, out var config) ||
                    config == null)
                    continue;
                if (outputSlot.Quantity != 0 &&
                    __instance.MoveItemBehaviour.IsTransitRouteValid(config.DestinationRoute,
                        outputSlot.ItemInstance.ID))
                {
                    __result = assignedStation;
                    return false;
                }
            }
        }

        __result = null;
        return false;
    }

    [HarmonyPatch(nameof(Packager.StartMoveItem), typeof(PackagingStation))]
    [HarmonyPrefix]
    private static bool StartMoveItem(Packager __instance, PackagingStation station)
    {
        UnpackageSave.Instance.UnpackageStations.TryGetValue(station.GUID, out var shouldUnpackage);
        if (!shouldUnpackage) return true;
        if (!Utils.Is<PackagingStationConfiguration>(station.Configuration, out var config) ||
            config == null) return true;
        var slotWithItem = station.InputSlots.FirstOrDefault(x =>
        {
            if (x.ItemInstance == null) return false;

            string reason;
            var valid = __instance.MoveItemBehaviour.IsTransitRouteValid(
                config.DestinationRoute,
                x.ItemInstance.ID,
                out reason);

            if (!valid)
            {
                Console.Log($"[Unpacked] Rejected item {x.ItemInstance.ID}: {reason}");
            }

            return valid;
        });
        if (slotWithItem == null) return true;
        Console.Log("[Unpacked] Starting moving items from " + station.gameObject.name);
        __instance.MoveItemBehaviour.InitializeMoveItemBehaviourWithID(config.DestinationRoute,
            slotWithItem.ItemInstance);
        __instance.MoveItemBehaviour.Enable_Networked();
        return false;
    }

    private static void InitializeMoveItemBehaviourWithID(
        this MoveItemBehaviour moveItemBehaviour,
        TransitRoute route,
        ItemInstance _itemToRetrieveTemplate,
        int _maxMoveAmount = -1,
        bool _skipPickup = false)
    {
        string invalidReason;
        if (_itemToRetrieveTemplate?.ID == null) return;
        if (!moveItemBehaviour.IsTransitRouteValid(route, _itemToRetrieveTemplate.ID, out invalidReason))
        {
            Console.LogError("Invalid transit route for move item behaviour! Reason: " + invalidReason,
                moveItemBehaviour.gameObject);
        }
        else
        {
            moveItemBehaviour.assignedRoute = route;
            moveItemBehaviour.itemToRetrieveTemplate = _itemToRetrieveTemplate;
            moveItemBehaviour.maxMoveAmount = _maxMoveAmount;
            if (moveItemBehaviour.Npc.Behaviour.DEBUG_MODE)
                Console.Log(
                    $"MoveItemBehaviour initialized with route: {route.Source.Name} -> {route.Destination.Name} for item: {_itemToRetrieveTemplate.ID}");
            moveItemBehaviour.skipPickup = _skipPickup;
        }
    }
}