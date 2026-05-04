using System.Collections;
using MelonLoader;
using EmployeeTweaks.Helpers;
using EmployeeTweaks.Patches.EmployeeArea;
using EmployeeTweaks.Patches.FilterItemApply;
using EmployeeTweaks.Patches.Unpackaging;
using MelonLoader.Preferences;
using S1API.Entities;
using UnityEngine;

[assembly: MelonInfo(
    typeof(EmployeeTweaks.EmployeeTweaks),
    EmployeeTweaks.BuildInfo.Name,
    EmployeeTweaks.BuildInfo.Version,
    EmployeeTweaks.BuildInfo.Author
)]
[assembly: MelonColor(1, 217, 131, 36)]
[assembly: MelonGame("TVGS", "Schedule I")]

// Specify platform domain based on build target (remove this if your mod supports both via S1API)
#if MONO
[assembly: MelonPlatformDomain(MelonPlatformDomainAttribute.CompatibleDomains.MONO)]
#else
[assembly: MelonPlatformDomain(MelonPlatformDomainAttribute.CompatibleDomains.IL2CPP)]
#endif

namespace EmployeeTweaks;

public static class BuildInfo
{
    public const string Name = "EmployeeTweaks";
    public const string Description = "Various employee tweaks - unpackaging, sprinkler/pourer use and more";
    public const string Author = "k073l";
    public const string Version = "1.0.1";
}

public class EmployeeTweaks : MelonMod
{
    private static MelonLogger.Instance Logger;
    private DebugAreaDrawer debugAreaDrawer;
    private bool _lastShift;
    private bool _lastCtrl;

    internal static MelonPreferences_Category EmployeeCapacityCategory =
        MelonPreferences.CreateCategory("EmployeeTweaksEmployeeCapacity", "Employee Capacities");

    internal static MelonPreferences_Entry<bool> EnableCapacityAndDebug =
        EmployeeCapacityCategory.CreateEntry("EmployeeTweaksEnableCapacityAndDebug", true, "Enable Category",
            "Enables employee capacity tweaks and drawing employee idle points area");

    internal static MelonPreferences_Entry<bool> DrawDebugArea =
        EmployeeCapacityCategory.CreateEntry("EmployeeTweaksDrawDebugArea", false, "Draw Debug Area",
            "Draws a debug area where employee idle points are contained");

    internal static HashSet<MelonPreferences_Entry<int>> EmployeeCapacities = [];

    internal static MelonPreferences_Category EmployeeAssignsCategory =
        MelonPreferences.CreateCategory("EmployeeTweaksEmployeeAssignsCategory", "Employee Assigns Capacities");

    internal static MelonPreferences_Entry<bool> EnableAssigns =
        EmployeeAssignsCategory.CreateEntry("EmployeeTweaksEnableAssigns", true, "Enable Category",
            "Enables employee assigns capacity modifications");

    internal static MelonPreferences_Entry<int> BotanistMaxPots =
        EmployeeAssignsCategory.CreateEntry("EmployeeTweaksBotanistMaxPots", 8, "Botanist Max Pots",
            "Maximum number of pots a botanist can be assigned to. Changes require a restart.",
            validator: new ValueRange<int>(1, 50));

    internal static MelonPreferences_Entry<int> HandlerMaxStations =
        EmployeeAssignsCategory.CreateEntry("EmployeeTweaksHandlerMaxStations", 3, "Handler Max Stations",
            "Maximum number of stations a packager can be assigned to. Changes require a restart.",
            validator: new ValueRange<int>(1, 8));

    internal static MelonPreferences_Entry<int> HandlerMaxRoutes =
        EmployeeAssignsCategory.CreateEntry("EmployeeTweaksHandlerMaxRoutes", 5, "Handler Max Routes",
            "Maximum number of routes a packager can be assigned to. Changes require a restart.",
            validator: new ValueRange<int>(1, 12));


    public override void OnInitializeMelon()
    {
        Logger = LoggerInstance;
        Logger.Msg("EmployeeTweaks initialized");
        MoveItemBehaviourPatches.ManualPatchDestinationValid(HarmonyInstance);
        debugAreaDrawer = new DebugAreaDrawer();
        Player.LocalPlayerSpawned += _ => debugAreaDrawer.Draw();
        DrawDebugArea.OnEntryValueChanged.Subscribe((_, _) => debugAreaDrawer.Draw());
    }

    public override void OnLateUpdate()
    {
        var text = FilterConfigPanelPatches.ApplyItemAsFilterButtonText;
        if (text == null)
            return;

        var shift = Input.GetKey(KeyCode.LeftShift) || Input.GetKey(KeyCode.RightShift);
        var ctrl = Input.GetKey(KeyCode.LeftControl) || Input.GetKey(KeyCode.RightControl);

        if (shift == _lastShift && ctrl == _lastCtrl)
            return;

        _lastShift = shift;
        _lastCtrl = ctrl;

        FilterConfigPanelPatches.AllSlots = shift;
        FilterConfigPanelPatches.DenyListMode = ctrl;

        text.text = shift ? FilterConfigPanelPatches.Filter2 : FilterConfigPanelPatches.Filter1;
    }
}