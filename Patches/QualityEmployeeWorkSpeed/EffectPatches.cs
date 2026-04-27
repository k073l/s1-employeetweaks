using EmployeeTweaks.Helpers;
using HarmonyLib;
using MelonLoader;
using UnityEngine;
#if MONO
using ScheduleOne.Effects;
using ScheduleOne.Employees;
using ScheduleOne.ItemFramework;
using ScheduleOne.NPCs;
using ScheduleOne.Product;
#else
using Il2CppScheduleOne.Effects;
using Il2CppScheduleOne.Employees;
using Il2CppScheduleOne.ItemFramework;
using Il2CppScheduleOne.NPCs;
using Il2CppScheduleOne.Product;
#endif

namespace EmployeeTweaks.Patches.QualityEmployeeWorkSpeed;

[HarmonyPatch]
internal static class EffectPatches
{
    private static MelonLogger.Instance _logger = new("EmployeeTweaks.QualityEmployeeWorkSpeed");
    
    [HarmonyPatch(typeof(ProductItemInstance), nameof(ProductItemInstance.ApplyEffectsToNPC))]
    [HarmonyPrefix]
    private static bool ApplyEffectsToEmployee(ProductItemInstance __instance, NPC npc)
    {
        if (__instance?.Definition == null) return true;
        if (npc == null) return true;
        if (!Utils.Is<Employee>(npc, out var employee) || employee == null) return true;
        if (!Utils.Is<ProductDefinition>(__instance.Definition, out var productDefinition) ||
            productDefinition == null) return true;
        List<Effect> effects = [];
        effects.AddRange(productDefinition.Properties.AsEnumerable());
        effects = effects.OrderBy(x => x.Tier).ToList();
        foreach (var effect in effects)
        {
            effect.ApplyToEmployee(employee);
            if (employee.WorkSpeedController.TryGetEntry(effect.Name, out var entry))
            {
                var newValue = ApplyQuality(entry.Value, GetQualityMult(__instance.Quality));
                _logger.Msg($"Applying quality transformation to {effect.Name}: was {entry.Value}, is {newValue}");
                entry.Value = newValue;
                employee.WorkSpeedController.Recalculate();
            }
        }

        return false;
    }

    private static float GetQualityMult(EQuality quality)
    {
        return quality switch
        {
            EQuality.Trash => 0.4f,
            EQuality.Poor => 0.7f,
            EQuality.Standard => 1f,
            EQuality.Premium => 1.2f,
            EQuality.Heavenly => 1.5f,
            _ => 1f
        };
    }

    private static float ApplyQuality(float baseValue, float m)
    {
        var quality = 1f + (m - 1f) * 1.2f;

        var lower = baseValue * 0.9f;
        var upper = baseValue * 1.2f;

        return Mathf.Lerp(lower, upper, Mathf.InverseLerp(0.4f, 1.5f, quality));
    }
}