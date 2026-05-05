using HarmonyLib;
using UnityEngine;
#if MONO
using ScheduleOne.Employees;
using ScheduleOne.UI.Management;

#else
using Il2CppScheduleOne.Employees;
using Il2CppScheduleOne.UI.Management;
#endif

namespace EmployeeTweaks.Patches.MoreEmployeeConfigItems;

[HarmonyPatch]
internal class CleanerMoreBins
{
    [HarmonyPatch(typeof(Cleaner), nameof(Cleaner.AssignProperty))]
    [HarmonyPostfix]
    private static void AddBins(Cleaner __instance)
    {
        if (!EmployeeTweaks.EnableAssigns.Value) return;
        __instance.configuration.Bins.MaxItems = EmployeeTweaks.CleanerMaxBins.Value;
    }
    
    [HarmonyPatch(typeof(CleanerConfigPanel), nameof(CleanerConfigPanel.BindInternal))]
    [HarmonyPostfix]
    private static void AddScrollStationRect(CleanerConfigPanel __instance)
    {
        if (!EmployeeTweaks.EnableAssigns.Value) return;
        if (EmployeeTweaks.CleanerMaxBins.Value <= EmployeeTweaks.CleanerMaxBins.DefaultValue) return;
        var stationList = __instance?.BinsUI;
        var go = stationList?.Entries.AsEnumerable().FirstOrDefault()?.parent.gameObject;
        if (go == null) return;
        var rt = go.GetComponent<RectTransform>();
        if (rt == null) return;
        ClipboardUIHelper.MoveToScrollableList(rt, go.transform.parent);
    }
}