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
internal static class BotanistMorePots
{
    [HarmonyPatch(typeof(Botanist), nameof(Botanist.AssignProperty))]
    [HarmonyPrefix]
    private static void Awake(Botanist __instance)
    {
        if (!Melon<EmployeeTweaks>.Instance.SettingsRegistry.EnableAssigns.Value) return;
        __instance.MaxAssignedPots = Melon<EmployeeTweaks>.Instance.SettingsRegistry.BotanistMaxPots.Value;
    }

    [HarmonyPatch(typeof(BotanistConfigPanel), nameof(BotanistConfigPanel.BindInternal))]
    [HarmonyPostfix]
    private static void AddScrollRect(BotanistConfigPanel __instance)
    {
        if (!Melon<EmployeeTweaks>.Instance.SettingsRegistry.EnableAssigns.Value) return;
        if (Melon<EmployeeTweaks>.Instance.SettingsRegistry.BotanistMaxPots.Value <= Melon<EmployeeTweaks>.Instance.SettingsRegistry.BotanistMaxPots.DefaultValue) return;
        var potsList = __instance?.PotsUI;
        var go = potsList?.Entries.AsEnumerable().FirstOrDefault()?.parent.gameObject;
        if (go == null) return;
        var rt = go.GetComponent<RectTransform>();
        if (rt == null) return;
        ClipboardUIHelper.MoveToScrollableList(rt, go.transform.parent);
        var hint = go.transform.parent.Find("Hint");
        if (hint == null) return;
        if (Melon<EmployeeTweaks>.Instance.SettingsRegistry.BotanistMaxPots.Value <= 9) return;
        hint.localPosition += new Vector3(10f, 0f);
    }
}