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

    internal SettingsRegistry SettingsRegistry;


    public override void OnInitializeMelon()
    {
        Logger = LoggerInstance;
        SettingsRegistry = new SettingsRegistry();
        Logger.Msg("EmployeeTweaks initialized");
        MoveItemBehaviourPatches.ManualPatchDestinationValid(HarmonyInstance);
        debugAreaDrawer = new DebugAreaDrawer();
        Player.LocalPlayerSpawned += _ => debugAreaDrawer.Draw();
        SettingsRegistry.DrawDebugArea.OnEntryValueChanged.Subscribe((_, _) => debugAreaDrawer.Draw());
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