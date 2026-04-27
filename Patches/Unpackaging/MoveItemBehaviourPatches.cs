#if MONO
using FishNet;
using FishNet.Object;
using ScheduleOne.Employees;
using ScheduleOne.GameTime;
using ScheduleOne.ItemFramework;
using ScheduleOne.Management;
using ScheduleOne.NPCs.Behaviour;
using ScheduleOne.ObjectScripts;
using Console = ScheduleOne.Console;
#else
using Il2CppFishNet;
using Console = Il2CppScheduleOne.Console;
using Il2CppInterop.Runtime;
using Il2CppScheduleOne.Employees;
using Il2CppScheduleOne.GameTime;
using Il2CppScheduleOne.ItemFramework;
using Il2CppScheduleOne.Management;
using Il2CppScheduleOne.NPCs;
using Il2CppScheduleOne.NPCs.Behaviour;
using Il2CppScheduleOne.ObjectScripts;
#endif
using System.Collections;
using EmployeeTweaks.Helpers;
using EmployeeTweaks.Persistence;
using HarmonyLib;
using MelonLoader;
using UnityEngine;

namespace EmployeeTweaks.Patches.Unpackaging;

[HarmonyPatch(typeof(MoveItemBehaviour))]
internal static class MoveItemBehaviourPatches
{
    private static Dictionary<MoveItemBehaviour, object> customGrabRunning = new();
    private static Dictionary<MoveItemBehaviour, object> customPlaceRunning = new();

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
        var destIsStation = Utils.Is2<PackagingStation>(dest, out var destStation) && destStation != null;
        var src = route.Source;
        var srcIsStation = Utils.Is2<PackagingStation>(src, out var srcStation) && srcStation != null;
        if (!destIsStation && !srcIsStation)
        {
            __result = false;
            return true;
        }

        var station = destIsStation ? destStation : srcStation;

        UnpackageSave.Instance.TryGetValue(station.GUID, out var shouldUnpackage);
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

        var capacity = destIsStation ? dest.GetOutputCapacityForItem(item, __instance.Npc) : dest.GetInputCapacityForItem(item, __instance.Npc);
        if (capacity == 0)
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
        var destIsStation = Utils.Is2<PackagingStation>(dest, out var destStation) && destStation != null;
        var src = route.Source;
        var srcIsStation = Utils.Is2<PackagingStation>(src, out var srcStation) && srcStation != null;

        if (!destIsStation && !srcIsStation) return true;
        var station = destIsStation ? destStation : srcStation;

        UnpackageSave.Instance.TryGetValue(station.GUID, out var shouldUnpackage);
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
        var destIsStation = Utils.Is2<PackagingStation>(dest, out var destStation) && destStation != null;
        var src = __instance.assignedRoute.Source;
        var srcIsStation = Utils.Is2<PackagingStation>(src, out var srcStation) && srcStation != null;

        if (!destIsStation && !srcIsStation) return true;
        var station = destIsStation ? destStation : srcStation;
        UnpackageSave.Instance.TryGetValue(station.GUID, out var shouldUnpackage);
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
        var destIsStation = Utils.Is2<PackagingStation>(dest, out var destStation) && destStation != null;
        var src = __instance.assignedRoute.Source;
        var srcIsStation = Utils.Is2<PackagingStation>(src, out var srcStation) && srcStation != null;

        if (!destIsStation && !srcIsStation) return true;
        var station = destIsStation ? destStation : srcStation;
        UnpackageSave.Instance.TryGetValue(station.GUID, out var shouldUnpackage);
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
        var capacityForItem = destIsStation
            ? dest.GetOutputCapacityForItem(itemInstance, __instance.Npc)
            : dest.GetInputCapacityForItem(itemInstance, __instance.Npc);
        __result = Mathf.Min(quantity, capacityForItem);
        return false;
    }

    [HarmonyPatch(nameof(MoveItemBehaviour.GrabItem))]
    [HarmonyPrefix]
    private static bool GrabItem(MoveItemBehaviour __instance)
    {
        if (__instance.assignedRoute?.Source == null) return true;

        var dest = __instance.assignedRoute.Destination;
        var destIsStation = Utils.Is2<PackagingStation>(dest, out var destStation) && destStation != null;
        var src = __instance.assignedRoute.Source;
        var srcIsStation = Utils.Is2<PackagingStation>(src, out var srcStation) && srcStation != null;
        if (!destIsStation && !srcIsStation) return true;
        var station = destIsStation ? destStation : srcStation;
        UnpackageSave.Instance.TryGetValue(station.GUID, out var shouldUnpackage);
        if (!shouldUnpackage) return true;
        if (destIsStation) return true;
        // srcIsStation

        if (__instance.beh.DEBUG_MODE)
            Console.Log("MoveItemBehaviour.GrabItem");
        __instance.currentState = MoveItemBehaviour.EState.Grabbing;
        __instance.grabRoutine = null;
        var obj = MelonCoroutines.Start(Routine());
        customGrabRunning[__instance] = obj;
        return false;

        IEnumerator Routine()
        {
            var sourceAccessPoint = __instance.GetSourceAccessPoint(__instance.assignedRoute);
            if (sourceAccessPoint == null)
            {
                Console.LogWarning("Could not find source access point!");
                __instance.grabRoutine = null;
                __instance.Disable_Networked(null);
                var coro = customGrabRunning[__instance];
                customGrabRunning.Remove(__instance);
                MelonCoroutines.Stop(coro);
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
                    var coro = customGrabRunning[__instance];
                    customGrabRunning.Remove(__instance);
                    MelonCoroutines.Stop(coro);
                }
                else
                {
                    __instance.TakeItem();
                    yield return new WaitForSeconds(TimeManager.TickDuration);
                    __instance.grabRoutine = null;
                    __instance.currentState = MoveItemBehaviour.EState.Idle;
                    var coro = customGrabRunning[__instance];
                    customGrabRunning.Remove(__instance);
                    MelonCoroutines.Stop(coro);
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
        var destIsStation = Utils.Is2<PackagingStation>(dest, out var destStation) && destStation != null;
        var src = __instance.assignedRoute.Source;
        var srcIsStation = Utils.Is2<PackagingStation>(src, out var srcStation) && srcStation != null;

        if (!destIsStation && !srcIsStation) return true;
        var station = destIsStation ? destStation : srcStation;
        UnpackageSave.Instance.TryGetValue(station.GUID, out var shouldUnpackage);
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
        var destIsStation = Utils.Is2<PackagingStation>(dest, out var destStation) && destStation != null;
        var src = __instance.assignedRoute.Source;
        var srcIsStation = Utils.Is2<PackagingStation>(src, out var srcStation) && srcStation != null;

        if (!destIsStation && !srcIsStation) return true;
        var station = destIsStation ? destStation : srcStation;
        UnpackageSave.Instance.TryGetValue(station.GUID, out var shouldUnpackage);
        if (!shouldUnpackage) return true;

        if (__instance.beh.DEBUG_MODE)
            Console.Log("MoveItemBehaviour.PlaceItem");
        __instance.currentState = MoveItemBehaviour.EState.Placing;
        __instance.placingRoutine = null;
        var obj = MelonCoroutines.Start(Routine());
        customPlaceRunning[__instance] = obj;
        return false;

        IEnumerator Routine()
        {
            if (__instance.GetDestinationAccessPoint(__instance.assignedRoute) != null)
                __instance.Npc.Movement.FaceDirection(__instance.GetDestinationAccessPoint(__instance.assignedRoute)
                    .forward);
            __instance.Npc.SetAnimationTrigger_Networked(null, "GrabItem");
            if (!Utils.Is<Employee>(__instance.Npc, out var employee) || employee == null)
            {
                customPlaceRunning.Remove(__instance);
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
            var coro = customPlaceRunning[__instance];
            customPlaceRunning.Remove(__instance);
            MelonCoroutines.Stop(coro);
        }
    }

    [HarmonyPatch(nameof(MoveItemBehaviour.StopCurrentActivity))]
    [HarmonyPrefix]
    private static bool StopCurrentActivity(MoveItemBehaviour __instance)
    {
        switch (__instance.currentState)
        {
            case MoveItemBehaviour.EState.Grabbing:
                if (customGrabRunning.TryGetValue(__instance, out var grabObj))
                {
                    MelonCoroutines.Stop(grabObj);
                    customGrabRunning.Remove(__instance);
                    return false;
                }

                return true;
                break;
            case MoveItemBehaviour.EState.Placing:
                if (customPlaceRunning.TryGetValue(__instance, out var placeObj))
                {
                    MelonCoroutines.Stop(placeObj);
                    customPlaceRunning.Remove(__instance);
                    return false;
                }

                return true;
                break;
        }

        return true;
    }

    [HarmonyPatch(nameof(MoveItemBehaviour.OnActiveTick))]
    [HarmonyPrefix]
    private static bool OnActiveTick(MoveItemBehaviour __instance)
    {
        if (!InstanceFinder.IsServer) return false;
        if (!__instance.assignedRoute.AreEntitiesNonNull())
        {
            Console.LogWarning("Transit route entities are null!");
            __instance.Disable_Networked(null);
            return false;
        }

        if (__instance.beh.DEBUG_MODE)
        {
            Console.Log("State: " + __instance.currentState);
            Console.Log("Moving: " + __instance.Npc.Movement.IsMoving);
        }

        if (__instance.currentState != 0) return false;
        if (__instance.Npc.Inventory.GetIdenticalItemAmount(__instance.itemToRetrieveTemplate) > 0 &&
            __instance.grabbedAmount > 0)
        {
            if (__instance.IsAtDestination())
                __instance.PlaceItem();
            else
                __instance.WalkToDestination();
        }
        else if (__instance.skipPickup)
        {
            __instance.TakeItem();
            __instance.skipPickup = false;
        }
        else if (__instance.IsAtSource())
            __instance.GrabItem();
        else
            __instance.WalkToSource();

        return false;
    }

    [HarmonyPatch(nameof(MoveItemBehaviour.StartTransit))]
    [HarmonyPrefix]
    private static bool StartTransit(MoveItemBehaviour __instance)
    {
        var dest = __instance.assignedRoute.Destination;
        var destIsStation = Utils.Is2<PackagingStation>(dest, out var destStation) && destStation != null;
        var src = __instance.assignedRoute.Source;
        var srcIsStation = Utils.Is2<PackagingStation>(src, out var srcStation) && srcStation != null;

        if (!destIsStation && !srcIsStation) return true;

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
#if MONO
            var firstIdenticalItem =
                __instance.Npc.Inventory.GetFirstIdenticalItem(__instance.itemToRetrieveTemplate,
                    __instance.IsNpcInventoryItemValid);
#else
            var converted =
                DelegateSupport.ConvertDelegate<NPCInventory.ItemFilter>(
                    __instance.IsNpcInventoryItemValid
                );
            var firstIdenticalItem =
                __instance.Npc.Inventory.GetFirstIdenticalItem(__instance.itemToRetrieveTemplate, converted);
#endif
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