using EmployeeTweaks.Helpers;
using HarmonyLib;
using MelonLoader;
using UnityEngine;
#if MONO
using ScheduleOne.Management;
using ScheduleOne.UI.Management;

#else
using Il2CppScheduleOne.Management;
using Il2CppScheduleOne.UI.Management;
#endif

namespace EmployeeTweaks.Patches.MoreEmployeeConfigItems;

[HarmonyPatch]
public class SharedClipboardPatches
{
    [HarmonyPatch(typeof(ObjectListFieldUI), nameof(ObjectListFieldUI.Bind))]
    [HarmonyPrefix]
    private static void AddMissing(ObjectListFieldUI __instance, 
#if MONO
        List<ObjectListField> field)
#else
        Il2CppSystem.Collections.Generic.List<ObjectListField> field)
#endif
    {
        if (!Melon<EmployeeTweaks>.Instance.SettingsRegistry.EnableAssigns.Value) return;
        var maxFieldsNeeded = field.AsEnumerable().Max(olf => olf.MaxItems);
        var currentEntryCount = __instance.Entries.Length;
        if (maxFieldsNeeded <= currentEntryCount)
        {
            MelonDebug.Msg($"Not adding, current: {currentEntryCount}, maxFieldsNeeded: {maxFieldsNeeded}");
            return;
        }

        var todo = maxFieldsNeeded - currentEntryCount;
        var template = __instance.Entries.AsEnumerable().FirstOrDefault();
        if (template == null || todo <= 0)
        {
            MelonDebug.Msg($"Not adding, template: {template} or todo {todo}");
            return;
        }

        List<RectTransform> newEntries = [];
        foreach (var entry in __instance.Entries)
            newEntries.Add(entry);
        for (var i = 0; i < todo; i++)
        {
            var go = UnityEngine.Object.Instantiate(template.gameObject);
            go.transform.SetParent(template.transform.parent, false);
            go.transform.SetAsLastSibling();
            newEntries.Add(go.GetComponent<RectTransform>());
        }

        __instance.Entries = newEntries.ToArray();
        MelonDebug.Msg($"Ensured {__instance.Entries.Length} instances");
    }

    [HarmonyPatch(typeof(RouteListFieldUI), nameof(RouteListFieldUI.Bind))]
    [HarmonyPrefix]
    private static void AddMissing(RouteListFieldUI __instance)
    {
        if (!Melon<EmployeeTweaks>.Instance.SettingsRegistry.EnableAssigns.Value) return;
        var maxFieldsNeeded = SettingsConstants.HandlerBoundsMaxRoutes.Item2;
        var currentEntryCount = __instance.RouteEntries.Length;
        if (maxFieldsNeeded <= currentEntryCount)
        {
            MelonDebug.Msg($"Not adding, current: {currentEntryCount}, maxFieldsNeeded: {maxFieldsNeeded}");
            return;
        }

        var todo = maxFieldsNeeded - currentEntryCount;
        var template = __instance.RouteEntries.AsEnumerable().FirstOrDefault()?.gameObject;
        if (template == null || todo <= 0)
        {
            MelonDebug.Msg($"Not adding, template: {template} or todo {todo}");
            return;
        }

        List<RouteEntryUI> newEntries = [];
        foreach (var entry in __instance.RouteEntries)
            newEntries.Add(entry);
        for (var i = 0; i < todo; i++)
        {
            var go = UnityEngine.Object.Instantiate(template.gameObject);
            go.transform.SetParent(template.transform.parent, false);
            go.transform.SetSiblingIndex(template.transform.parent.childCount - 2);
            go.SetActive(false);
            var component = go.GetComponent<RouteEntryUI>();
            if (component == null) continue;
            newEntries.Add(component);
        }

        __instance.RouteEntries = newEntries.ToArray();
    }
}