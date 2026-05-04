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
internal static class BotanistMorePots
{
    [HarmonyPatch(typeof(Botanist), nameof(Botanist.Awake))]
    [HarmonyPrefix]
    private static void Awake(Botanist __instance)
    {
        if (!EmployeeTweaks.EnableAssigns.Value) return;
        __instance.MaxAssignedPots = EmployeeTweaks.BotanistMaxPots.Value;
    }

    [HarmonyPatch(typeof(BotanistConfigPanel), nameof(BotanistConfigPanel.BindInternal))]
    [HarmonyPostfix]
    private static void AddScrollRect(BotanistConfigPanel __instance)
    {
        if (!EmployeeTweaks.EnableAssigns.Value) return;
        if (EmployeeTweaks.BotanistMaxPots.Value <= EmployeeTweaks.BotanistMaxPots.DefaultValue) return;
        var potsList = __instance?.PotsUI;
        var go = potsList?.Entries.FirstOrDefault()?.parent.gameObject;
        if (go == null) return;
        var rt = go.GetComponent<RectTransform>();
        if (rt == null) return;
        ClipboardUIHelper.MoveToScrollableList(rt, go.transform.parent);
        var hint = go.transform.parent.Find("Hint");
        if (hint == null) return;
        if (EmployeeTweaks.BotanistMaxPots.Value <= 9) return;
        hint.localPosition += new Vector3(10f, 0f);
    }
}