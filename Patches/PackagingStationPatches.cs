using EmployeeTweaks.Helpers;
using EmployeeTweaks.Persistence;
using HarmonyLib;
using MelonLoader;
using ScheduleOne.Management;
using ScheduleOne.NPCs.Behaviour;
using ScheduleOne.ObjectScripts;

namespace EmployeeTweaks.Patches;

[HarmonyPatch(typeof(PackagingStation))]
internal static class PackagingStationPatches
{
    [HarmonyPatch(nameof(PackagingStation.PackSingleInstance))]
    [HarmonyPrefix]
    private static bool UnpackIfConfigured(PackagingStation __instance)
    {
        if (__instance?.GUID == null) return true;
        UnpackageSave.Instance.UnpackageStations.TryGetValue(__instance.GUID, out var shouldUnpackage);
        if (!shouldUnpackage) return true;
        __instance.Unpack();
        return false;
    }
}

[HarmonyPatch(typeof(PackagingStationBehaviour))]
internal static class PackagingStationBehaviourPatches
{
    [HarmonyPatch(nameof(PackagingStationBehaviour.IsStationReady))]
    [HarmonyPrefix]
    private static bool IsStationReady(PackagingStationBehaviour __instance, PackagingStation station, ref bool __result)
    {
        if (station == null)
        {
            __result = false;
            return false;
        }

        var npc = __instance.Npc;
        if (Utils.Is<IUsable>(station, out var usable) && usable != null)
        {
            if (usable.IsInUse && station.NPCUserObject != npc.NetworkObject)
            {
                __result = false;
                return false;
            }
        }

        if (!npc.Movement.CanGetTo(station.StandPoint.position))
        {
            __result = false;
            return false;
        }
        
        UnpackageSave.Instance.UnpackageStations.TryGetValue(station.GUID, out var shouldUnpackage);
        var mode = shouldUnpackage
            ? PackagingStation.EMode.Unpackage
            : PackagingStation.EMode.Package;

        if (station.GetState(mode) != 0)
        {
            __result = false;
            return false;
        }
        __result = true;
        return false;
    }
}