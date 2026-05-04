using HarmonyLib;
using UnityEngine;
using UnityEngine.UI;
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
    [HarmonyPatch(typeof(Packager), nameof(Packager.Awake))]
    [HarmonyPrefix]
    private static void AddStations(Packager __instance)
    {
        if (!EmployeeTweaks.EnableAssigns.Value) return;
        __instance.MaxAssignedStations = EmployeeTweaks.HandlerMaxStations.Value;
    }

    [HarmonyPatch(typeof(Packager), nameof(Packager.AssignProperty))]
    [HarmonyPostfix]
    private static void AddRoutes(Packager __instance)
    {
        if (!EmployeeTweaks.EnableAssigns.Value) return;
        __instance.configuration.Routes.MaxRoutes = EmployeeTweaks.HandlerMaxRoutes.Value;
    }

    [HarmonyPatch(typeof(PackagerConfigPanel), nameof(PackagerConfigPanel.BindInternal))]
    [HarmonyPostfix]
    private static void AddScrollStationRect(PackagerConfigPanel __instance)
    {
        if (!EmployeeTweaks.EnableAssigns.Value) return;
        if (EmployeeTweaks.HandlerMaxStations.Value <= EmployeeTweaks.HandlerMaxStations.DefaultValue) return;
        var stationList = __instance?.StationsUI;
        var go = stationList?.Entries.FirstOrDefault()?.parent.gameObject;
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
        if (!EmployeeTweaks.EnableAssigns.Value) return;
        if (EmployeeTweaks.HandlerMaxRoutes.Value <= EmployeeTweaks.HandlerMaxRoutes.DefaultValue) return;
        var routeList = __instance.RoutesUI;
        var go = routeList?.RouteEntries.FirstOrDefault()?.transform.parent.gameObject;
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
    }
}