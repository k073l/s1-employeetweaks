using System.Collections;
using System.Reflection;
using HarmonyLib;
using MelonLoader;
using EmployeeTweaks.Helpers;
using EmployeeTweaks.Patches;
using EmployeeTweaks.Patches.Unpackaging;
using UnityEngine;
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
    public const string Description = "does stuff i guess";
    public const string Author = "me";
    public const string Version = "1.0.0";
}

public class EmployeeTweaks : MelonMod
{
    private static MelonLogger.Instance Logger;

    public override void OnInitializeMelon()
    {
        Logger = LoggerInstance;
        Logger.Msg("EmployeeTweaks initialized");
        MoveItemBehaviourPatches.ManualPatchDestinationValid(HarmonyInstance);
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