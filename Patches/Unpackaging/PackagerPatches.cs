#if MONO
using FishNet;
using ScheduleOne.Employees;
using ScheduleOne.ItemFramework;
using ScheduleOne.Management;
using ScheduleOne.NPCs.Behaviour;
using ScheduleOne.ObjectScripts;
using Console = ScheduleOne.Console;
#else
using Il2CppFishNet;
using Console = Il2CppScheduleOne.Console;
using Il2CppScheduleOne.Employees;
using Il2CppScheduleOne.ItemFramework;
using Il2CppScheduleOne.Management;
using Il2CppScheduleOne.NPCs.Behaviour;
using Il2CppScheduleOne.ObjectScripts;
#endif
using EmployeeTweaks.Helpers;
using EmployeeTweaks.Persistence;
using HarmonyLib;

namespace EmployeeTweaks.Patches.Unpackaging;

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
                UnpackageSave.Instance.TryGetValue(station.GUID, out var shouldUnpackage) &&
                shouldUnpackage)
            {
                var inputSlots = station.InputSlots;
                if (!Utils.Is<PackagingStationConfiguration>(assignedStation.Configuration, out var config) ||
                    config == null)
                    continue;
                var isAnyRouteValid = inputSlots.AsEnumerable().Where(x => x != null)
                    .Select(x => x.ItemInstance)
                    .Where(x => x?.ID != null)
                    .Any(x => __instance.MoveItemBehaviour.IsTransitRouteValid(config.DestinationRoute, x.ID));
                if (inputSlots.AsEnumerable().Sum(x => x.Quantity) != 0 && isAnyRouteValid)
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
        UnpackageSave.Instance.TryGetValue(station.GUID, out var shouldUnpackage);
        if (!shouldUnpackage) return true;
        if (!Utils.Is<PackagingStationConfiguration>(station.Configuration, out var config) ||
            config == null) return true;
        var slotWithItem = station.InputSlots.AsEnumerable().FirstOrDefault(x =>
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

    [HarmonyReversePatch]
    [HarmonyPatch(typeof(Employee), nameof(Employee.UpdateBehaviour))]
    private static void UpdateEmployeeBehaviour(Employee __instance) => throw new NotImplementedException();

    [HarmonyPatch(nameof(Packager.UpdateBehaviour))]
    [HarmonyPrefix]
    private static bool UpdateBehaviour(Packager __instance)
    {
        UpdateEmployeeBehaviour(__instance);
        if (__instance.PackagingBehaviour.Active)
        {
            __instance.MarkIsWorking();
        }
        else if (__instance.MoveItemBehaviour.Active)
        {
            __instance.MarkIsWorking();
        }
        else
        {
            if (!InstanceFinder.IsServer)
            {
                return false;
            }

            if (__instance.Fired)
            {
                __instance.LeavePropertyAndDespawn();
            }
            else
            {
                if (!__instance.CanWork())
                {
                    return false;
                }

                if (__instance.configuration.AssignedStationCount + __instance.configuration.Routes.Routes.Count == 0)
                {
                    __instance.SubmitNoWorkReason("I haven't been assigned to any stations or routes.",
                        "You can use your management clipboards to assign stations or routes to me.");
                    __instance.SetIdle(idle: true);
                }
                else
                {
                    if (!InstanceFinder.IsServer)
                    {
                        return false;
                    }

                    var stationToAttend = __instance.GetStationToAttend();
                    if (stationToAttend != null)
                    {
                        __instance.StartPackaging(stationToAttend);
                        return false;
                    }

                    var brickPress = __instance.GetBrickPress();
                    if (brickPress != null)
                    {
                        __instance.StartPress(brickPress);
                        return false;
                    }

                    var stationMoveItems = __instance.GetStationMoveItems();
                    if (stationMoveItems != null)
                    {
                        __instance.StartMoveItem(stationMoveItems);
                        return false;
                    }

                    var brickPressMoveItems = __instance.GetBrickPressMoveItems();
                    if (brickPressMoveItems != null)
                    {
                        __instance.StartMoveItem(brickPressMoveItems);
                        return false;
                    }

                    ItemInstance item;
                    var transitRouteReady = __instance.GetTransitRouteReady(out item);
                    if (transitRouteReady != null)
                    {
                        __instance.MoveItemBehaviour.Initialize(transitRouteReady, item, item.Quantity);
                        __instance.MoveItemBehaviour.Enable_Networked();
                    }
                    else
                    {
                        __instance.SubmitNoWorkReason("There's nothing for me to do right now.",
                            "I need one of my assigned stations to have enough product and packaging to get to work.");
                        __instance.SetIdle(idle: true);
                    }
                }
            }
        }

        return false;
    }
}