using HarmonyLib;
using MelonLoader;
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
    [HarmonyPatch(typeof(Chemist), nameof(Chemist.OnSpawnServer))]
    [HarmonyPostfix]
    private static void AddStations(Chemist __instance)
    {
        if (!Melon<EmployeeTweaks>.Instance.SettingsRegistry.EnableAssigns.Value) return;
        __instance.configuration.Stations.MaxItems = Melon<EmployeeTweaks>.Instance.SettingsRegistry.ChemistMaxStations.Value;
    }
    
    [HarmonyPatch(typeof(ChemistConfigPanel), nameof(ChemistConfigPanel.BindInternal))]
    [HarmonyPostfix]
    private static void AddScrollStationRect(ChemistConfigPanel __instance)
    {
        if (!Melon<EmployeeTweaks>.Instance.SettingsRegistry.EnableAssigns.Value) return;
        if (Melon<EmployeeTweaks>.Instance.SettingsRegistry.ChemistMaxStations.Value <= Melon<EmployeeTweaks>.Instance.SettingsRegistry.ChemistMaxStations.DefaultValue) return;
        var stationList = __instance?.StationsUI;
        var go = stationList?.Entries.AsEnumerable().FirstOrDefault()?.parent.gameObject;
        if (go == null) return;
        var rt = go.GetComponent<RectTransform>();
        if (rt == null) return;
        ClipboardUIHelper.MoveToScrollableList(rt, go.transform.parent);
        var hint = go.transform.parent.Find("Hint");
        if (hint == null) return;
        if (Melon<EmployeeTweaks>.Instance.SettingsRegistry.ChemistMaxStations.Value <= 9) return;
        hint.localPosition += new Vector3(10f, 0f);
    }
}