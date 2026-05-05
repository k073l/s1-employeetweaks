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
internal class ChemistMoreStations
{
    [HarmonyPatch(typeof(Chemist), nameof(Chemist.AssignProperty))]
    [HarmonyPostfix]
    private static void AddStations(Chemist __instance)
    {
        if (!EmployeeTweaks.EnableAssigns.Value) return;
        __instance.configuration.Stations.MaxItems = EmployeeTweaks.ChemistMaxStations.Value;
    }
    
    [HarmonyPatch(typeof(ChemistConfigPanel), nameof(ChemistConfigPanel.BindInternal))]
    [HarmonyPostfix]
    private static void AddScrollStationRect(ChemistConfigPanel __instance)
    {
        if (!EmployeeTweaks.EnableAssigns.Value) return;
        if (EmployeeTweaks.ChemistMaxStations.Value <= EmployeeTweaks.ChemistMaxStations.DefaultValue) return;
        var stationList = __instance?.StationsUI;
        var go = stationList?.Entries.AsEnumerable().FirstOrDefault()?.parent.gameObject;
        if (go == null) return;
        var rt = go.GetComponent<RectTransform>();
        if (rt == null) return;
        ClipboardUIHelper.MoveToScrollableList(rt, go.transform.parent);
        var hint = go.transform.parent.Find("Hint");
        if (hint == null) return;
        if (EmployeeTweaks.ChemistMaxStations.Value <= 9) return;
        hint.localPosition += new Vector3(10f, 0f);
    }
}