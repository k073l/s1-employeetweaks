#if MONO
using ScheduleOne.ItemFramework;
using ScheduleOne.Management;
using ScheduleOne.ObjectScripts;
#else
using Il2CppScheduleOne.ItemFramework;
using Il2CppScheduleOne.ObjectScripts;
using Il2CppScheduleOne.Management;
#endif
using EmployeeTweaks.Helpers;
using EmployeeTweaks.Persistence;
using HarmonyLib;
using UnityEngine;

namespace EmployeeTweaks.Patches.Unpackaging;


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
        var destIsStation = Utils.Is2<PackagingStation>(dest, out var destStation) && destStation != null;
        var src = __instance.Source;
        var srcIsStation = Utils.Is2<PackagingStation>(src, out var srcStation) && srcStation != null;

        if (!destIsStation && !srcIsStation) return true;
        var station = destIsStation ? destStation : srcStation;
        UnpackageSave.Instance.TryGetValue(station.GUID, out var shouldUnpackage);
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