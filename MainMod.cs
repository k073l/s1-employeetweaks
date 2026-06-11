using EmployeeTweaks.Helpers;
using EmployeeTweaks.Network;
using EmployeeTweaks.Patches.EmployeeArea;
using EmployeeTweaks.Patches.FilterItemApply;
using EmployeeTweaks.Patches.Unpackaging;
using MelonLoader;
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
    public const string Version = "1.0.4";
}

public class EmployeeTweaks : MelonMod
{
    private static MelonLogger.Instance Logger;
    private DebugAreaDrawer debugAreaDrawer;
    private bool _lastShift;
    private bool _lastCtrl;

    internal SettingsRegistry SettingsRegistry;
    internal INetworkManager? NetworkManager;

    public override void OnInitializeMelon()
    {
        Logger = LoggerInstance;
        DependenciesChecker.PrintMissing();
        MoveItemBehaviourPatches.ManualPatchDestinationValid(HarmonyInstance);
    }

    public override void OnLateInitializeMelon()
    {
        SettingsRegistry = new SettingsRegistry();
        NetworkManager = NetworkLoader.Create();
        if (NetworkManager == null)
            Logger.Warning("NetworkManager is null, multiplayer features will be unavailable.");
        SettingsRegistry.InitializeCategories();
        debugAreaDrawer = new DebugAreaDrawer();
        DebugAreaDrawer.WireDebugAreaDrawer(debugAreaDrawer);
        Logger.Msg("EmployeeTweaks initialized");
    }

    public override void OnUpdate()
    {
        NetworkManager?.Update();
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
    
    public override void OnDeinitializeMelon()
    {
        NetworkManager?.Dispose();
    }
}