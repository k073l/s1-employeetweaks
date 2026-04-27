using System.Collections;
using System.Reflection;
using HarmonyLib;
using MelonLoader;
using EmployeeTweaks.Helpers;
using EmployeeTweaks.Patches;
using EmployeeTweaks.Patches.EmployeeArea;
using EmployeeTweaks.Patches.Unpackaging;
using S1API.Entities;
using UnityEngine;
using Object = UnityEngine.Object;
#if MONO
using FishNet;
#else
using Il2CppFishNet;
#endif

[assembly: MelonInfo(
    typeof(EmployeeTweaks.EmployeeTweaks),
    EmployeeTweaks.BuildInfo.Name,
    EmployeeTweaks.BuildInfo.Version,
    EmployeeTweaks.BuildInfo.Author
)]
[assembly: MelonColor(1, 255, 0, 0)]
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
    public const string Description = "Some employee stuff";
    public const string Author = "k073l";
    public const string Version = "1.0.0";
}

public class EmployeeTweaks : MelonMod
{
    private static MelonLogger.Instance Logger;
    private DebugAreaDrawer debugAreaDrawer;

    internal static MelonPreferences_Category EmployeeCapacityCategory =
        MelonPreferences.CreateCategory("EmployeeTweaksEmployeeCapacity", "Employee Capacities");
    
    internal static MelonPreferences_Entry<bool> EnableCapacityAndDebug =
        EmployeeCapacityCategory.CreateEntry("EmployeeTweaksEnableCapacityAndDebug", true, "Enable Category",
            "Enables employee capacity tweaks and drawing employee idle points area");
    internal static MelonPreferences_Entry<bool> DrawDebugArea =
        EmployeeCapacityCategory.CreateEntry("EmployeeTweaksDrawDebugArea", false, "Draw Debug Area",
            "Draws a debug area where employee idle points are contained");
    internal static HashSet<MelonPreferences_Entry<int>> EmployeeCapacities = [];

    public override void OnInitializeMelon()
    {
        Logger = LoggerInstance;
        Logger.Msg("EmployeeTweaks initialized");
        MoveItemBehaviourPatches.ManualPatchDestinationValid(HarmonyInstance);
        debugAreaDrawer = new DebugAreaDrawer();
        Player.LocalPlayerSpawned += _ => debugAreaDrawer.Draw();
        DrawDebugArea.OnEntryValueChanged.Subscribe((_, _) => debugAreaDrawer.Draw());
    }

    public override void OnSceneWasLoaded(int buildIndex, string sceneName)
    {
        Logger.Debug($"Scene loaded: {sceneName}");
        if (sceneName == "Main")
        {
            Logger.Debug("Main scene loaded, waiting for player");
            MelonCoroutines.Start(Utils.WaitForPlayer(DoStuff()));
        }
    }

    private IEnumerator DoStuff()
    {
        Logger.Msg("Player ready, doing stuff...");
        yield return new WaitForSeconds(2f);
        Logger.Msg("Did some stuff!");
    }
}