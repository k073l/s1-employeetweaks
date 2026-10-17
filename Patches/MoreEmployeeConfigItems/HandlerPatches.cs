using HarmonyLib;
using MelonLoader;
using UnityEngine;
using UnityEngine.UI;
using Logger = EmployeeTweaks.Helpers.Logger;
#if MONO
using ScheduleOne.Employees;
using ScheduleOne.UI.Management;

#else
using Il2CppScheduleOne.Employees;
using Il2CppScheduleOne.UI.Management;
#endif

namespace EmployeeTweaks.Patches.MoreEmployeeConfigItems;

[HarmonyPatch]
internal static class HandlerPatches
{
    private static readonly Logger Logger = new(nameof(HandlerPatches));
    
    [HarmonyPatch(typeof(Packager), nameof(Packager.AssignProperty))]
    [HarmonyPostfix]
    private static void AddRoutes(Packager __instance)
    {
        if (!Melon<EmployeeTweaks>.Instance.SettingsRegistry.EnableAssigns.Value) return;
        __instance.configuration.Routes.MaxRoutes = Melon<EmployeeTweaks>.Instance.SettingsRegistry.HandlerMaxRoutes.Value;
        __instance.MaxAssignedStations = Melon<EmployeeTweaks>.Instance.SettingsRegistry.HandlerMaxStations.Value;
        __instance.configuration.Stations.MaxItems =
            Melon<EmployeeTweaks>.Instance.SettingsRegistry.HandlerMaxStations.Value;
    }

    [HarmonyPatch(typeof(PackagerConfigPanel), nameof(PackagerConfigPanel.BindInternal))]
    [HarmonyPostfix]
    private static void AddScrollStationRect(PackagerConfigPanel __instance)
    {
        if (!Melon<EmployeeTweaks>.Instance.SettingsRegistry.EnableAssigns.Value) return;
        if (Melon<EmployeeTweaks>.Instance.SettingsRegistry.HandlerMaxStations.Value <= Melon<EmployeeTweaks>.Instance.SettingsRegistry.HandlerMaxStations.DefaultValue) return;
        var stationList = __instance?.StationsUI;
        var go = stationList?.Entries.AsEnumerable().FirstOrDefault()?.parent.gameObject;
        if (go == null) return;
        var rt = go.GetComponent<RectTransform>();
        if (rt == null) return;
        var scroll = ClipboardUIHelper.MoveToScrollableList(rt, go.transform.parent);
        var scrollRT = scroll.gameObject.GetComponent<RectTransform>();
        if (scrollRT == null) return;
        scrollRT.sizeDelta = new Vector2(0f, 165f);
        scrollRT.offsetMin = new Vector2(0f, -165f);
        Canvas.ForceUpdateCanvases();
        LayoutRebuilder.ForceRebuildLayoutImmediate(scroll.content);
    }

    [HarmonyPatch(typeof(PackagerConfigPanel), nameof(PackagerConfigPanel.BindInternal))]
    [HarmonyPostfix]
    private static void AddScrollRouteRect(PackagerConfigPanel __instance)
    {
        if (!Melon<EmployeeTweaks>.Instance.SettingsRegistry.EnableAssigns.Value) return;
        if (Melon<EmployeeTweaks>.Instance.SettingsRegistry.HandlerMaxRoutes.Value <= Melon<EmployeeTweaks>.Instance.SettingsRegistry.HandlerMaxRoutes.DefaultValue) return;
        var routeList = __instance.RoutesUI;
        var go = routeList?.RouteEntries.AsEnumerable().FirstOrDefault()?.transform.parent.gameObject;
        if (go == null) return;
        var rt = go.GetComponent<RectTransform>();
        if (rt == null) return;
        var scroll = ClipboardUIHelper.MoveToScrollableList(rt, go.transform.parent);
        var scrollRT = scroll.gameObject.GetComponent<RectTransform>();
        if (scrollRT == null) return;
        scrollRT.anchoredPosition = new Vector2(0, -55f);
        scrollRT.sizeDelta = new Vector2(0f, 165f);
        Canvas.ForceUpdateCanvases();
        LayoutRebuilder.ForceRebuildLayoutImmediate(scroll.content);
#if !MONO
        if (routeList == null)
        {
            Logger.Err("Route list is null");
            return;
        }

        SharedClipboardPatches.AddMissing(routeList);
        routeList.Refresh(routeList.Fields[0].Routes);
#endif
    }
}