using System.Reflection;
using EmployeeTweaks.Helpers;
using EmployeeTweaks.Patches.BotanistSprinklersPourer;
using EmployeeTweaks.Patches.MoreEmployeeConfigItems;
using EmployeeTweaks.Patches.Unpackaging;
using MelonLoader;
using MelonLoader.Preferences;

namespace EmployeeTweaks;

internal class SettingsRegistry
{
    internal MelonPreferences_Category EmployeeCapacityCategory;

    internal NetworkedMelonEntry<bool> EnableCapacityAndDebug;

    internal MelonPreferences_Entry<bool> DrawDebugArea;

    internal HashSet<NetworkedMelonEntry<int>> EmployeeCapacities = [];

    internal MelonPreferences_Category EmployeeAssignsCategory;

    internal NetworkedMelonEntry<bool> EnableAssigns;

    internal NetworkedMelonEntry<int> BotanistMaxPots;

    internal NetworkedMelonEntry<int> HandlerMaxStations;

    internal NetworkedMelonEntry<int> HandlerMaxRoutes;

    internal NetworkedMelonEntry<int> ChemistMaxStations;

    internal NetworkedMelonEntry<int> CleanerMaxBins;
    
    internal MelonPreferences_Category EmployeeTweaksDebugCategory;
    
    internal MelonPreferences_Entry<bool> EnableNetworkDebug;
    
    internal MelonPreferences_Entry<bool> EnableConfigItemsDebug;
    
    internal MelonPreferences_Entry<bool> EnableUnpackagingDebug;

    internal MelonPreferences_Entry<bool> EnablePourerDebug;
    
    internal object? _boxedClient = null;
    internal object? _boxedOptions = null;

    /// <summary>
    /// Initializes the network synchronization for preferences entries by loading the necessary types
    /// from the SteamNetworkLib assembly and creating instances of the client and options. This method must be called
    /// before any networked entries are created to ensure they are properly configured for synchronization.
    /// </summary>
    /// <param name="client">
    /// A boxed instance of the SteamNetworkClient. This is required for network synchronization.
    /// If <see langword="null"/>, entries will be local-only.
    /// </param>
    /// <returns>
    /// <see langword="true"/> if initialization succeeded and networked entries can be created;
    /// <see langword="false"/> if initialization failed and entries will be local-only.
    /// </returns>
    public bool InitializeNetwork(object? client)
    {
        if (client == null) return false;
        _boxedClient = client;
        var assembly = AppDomain.CurrentDomain.GetAssemblies()
            .FirstOrDefault(a => a.GetName().Name == "SteamNetworkLib");
        if (assembly == null) return false;

        var type = assembly.GetType("SteamNetworkLib.Sync.NetworkSyncOptions");
        if (type == null) return false;

        _boxedOptions = Activator.CreateInstance(type);
        if (_boxedOptions == null) return false;

        var keyPrefixProperty = type.GetProperty("KeyPrefix");
        if (keyPrefixProperty == null) return false;

        keyPrefixProperty.SetValue(_boxedOptions, $"{Assembly.GetExecutingAssembly().GetName().Name}_");
        return true;
    }

    public void InitializeCategories()
    {
        InitializeCapacityCategory();
        InitializeAssignsCategory();
        InitializeModDebugCategory();
    }

    private void InitializeCapacityCategory()
    {
        EmployeeCapacityCategory =
            MelonPreferences.CreateCategory("EmployeeTweaksEmployeeCapacity", "Employee Capacities");
        EnableCapacityAndDebug =
            EmployeeCapacityCategory.GetOrCreateNetworkedEntry("EmployeeTweaksEnableCapacityAndDebug", true, _boxedClient, _boxedOptions, true, "Enable Category",
                "Enables employee capacity tweaks and drawing employee idle points area");
        DrawDebugArea =
            EmployeeCapacityCategory.CreateEntry("EmployeeTweaksDrawDebugArea", false, "Draw Debug Area",
                "Draws a debug area where employee idle points are contained");
    }

    private void InitializeAssignsCategory()
    {
        EmployeeAssignsCategory =
            MelonPreferences.CreateCategory("EmployeeTweaksEmployeeAssignsCategory", "Employee Assigns Capacities");
        EnableAssigns =
            EmployeeAssignsCategory.GetOrCreateNetworkedEntry("EmployeeTweaksEnableAssigns", true, _boxedClient, _boxedOptions, true, "Enable Category",
                "Enables employee assigns capacity modifications");
        BotanistMaxPots =
            EmployeeAssignsCategory.GetOrCreateNetworkedEntry("EmployeeTweaksBotanistMaxPots",
                SettingsConstants.BotanistDefaultMaxPots, _boxedClient, _boxedOptions, true, "Botanist Max Pots",
                $"Maximum number of pots a botanist can be assigned to (allowed values from {SettingsConstants.BotanistBoundsMaxPots.Item1} to {SettingsConstants.BotanistBoundsMaxPots.Item2}). Changes require a restart.",
                validator: new ValueRange<int>(SettingsConstants.BotanistBoundsMaxPots.Item1,
                    SettingsConstants.BotanistBoundsMaxPots.Item2));
        HandlerMaxStations =
            EmployeeAssignsCategory.GetOrCreateNetworkedEntry("EmployeeTweaksHandlerMaxStations",
                SettingsConstants.HandlerDefaultMaxStations, _boxedClient, _boxedOptions, true, "Handler Max Stations",
                $"Maximum number of stations a packager can be assigned to (allowed values from {SettingsConstants.HandlerBoundsMaxStations.Item1} to {SettingsConstants.HandlerBoundsMaxStations.Item2}). Changes require a restart.",
                validator: new ValueRange<int>(SettingsConstants.HandlerBoundsMaxStations.Item1,
                    SettingsConstants.HandlerBoundsMaxStations.Item2));
        HandlerMaxRoutes =
            EmployeeAssignsCategory.GetOrCreateNetworkedEntry("EmployeeTweaksHandlerMaxRoutes",
                SettingsConstants.HandlerDefaultMaxRoutes, _boxedClient, _boxedOptions, true, "Handler Max Routes",
                $"Maximum number of routes a packager can be assigned to (allowed values from {SettingsConstants.HandlerBoundsMaxRoutes.Item1} to {SettingsConstants.HandlerBoundsMaxRoutes.Item2}). Changes require a restart.",
                validator: new ValueRange<int>(SettingsConstants.HandlerBoundsMaxRoutes.Item1,
                    SettingsConstants.HandlerBoundsMaxRoutes.Item2));
        ChemistMaxStations =
            EmployeeAssignsCategory.GetOrCreateNetworkedEntry("EmployeeTweaksChemistMaxStations",
                SettingsConstants.ChemistDefaultMaxStations, _boxedClient, _boxedOptions, true, "Chemist Max Stations",
                $"Maximum number of stations a chemist can be assigned to (allowed values from {SettingsConstants.ChemistBoundsMaxStations.Item1} to {SettingsConstants.ChemistBoundsMaxStations.Item2}). Changes require a restart.",
                validator: new ValueRange<int>(SettingsConstants.ChemistBoundsMaxStations.Item1,
                    SettingsConstants.ChemistBoundsMaxStations.Item2));
        CleanerMaxBins =
            EmployeeAssignsCategory.GetOrCreateNetworkedEntry("EmployeeTweaksCleanerMaxBins", SettingsConstants.CleanerDefaultMaxBins, _boxedClient, _boxedOptions, true,
                "Cleaner Max Trash Cans",
                $"Maximum number of trash cans a cleaner can be assigned to (allowed values from {SettingsConstants.CleanerBoundsMaxBins.Item1} to {SettingsConstants.CleanerBoundsMaxBins.Item2}). Changes require a restart.",
                validator: new ValueRange<int>(SettingsConstants.CleanerBoundsMaxBins.Item1,
                    SettingsConstants.CleanerBoundsMaxBins.Item2));
    }

    private void InitializeModDebugCategory()
    {
        EmployeeTweaksDebugCategory =
            MelonPreferences.CreateCategory("EmployeeTweaksDebug", "Employee Tweaks Debug&Troubleshooting");
        EnableNetworkDebug =
            EmployeeTweaksDebugCategory.CreateEntry("EmployeeTweaksEnableNetworkDebug", false, "Enable Network Debug",
                "Enables debug logging for networked preferences");
        EnableConfigItemsDebug =
            EmployeeTweaksDebugCategory.CreateEntry("EmployeeTweaksEnableConfigItemsDebug", false, "Enable Config Items Debug",
                "Enables debug logging for employee config items");
        EnableUnpackagingDebug =
            EmployeeTweaksDebugCategory.CreateEntry("EmployeeTweaksEnableUnpackagingDebug", false, "Enable Unpackaging Debug",
                "Enables debug logging for unpackaging");
        EnablePourerDebug =
            EmployeeTweaksDebugCategory.CreateEntry("EmployeeTweaksEnablePourerDebug", false, "Enable Pourer/Sprinkler Debug",
                "Enables debug logging for automatic use of Soil Pourers and Sprinklers");
    }

    private void PushLoggerSettings()
    {
        // NetworkDebug is handled by NetworkManager

        SetAndRegister(ClipboardUIHelper.Logger, EnableConfigItemsDebug);
        SetAndRegister(SharedClipboardPatches.Logger, EnableConfigItemsDebug);

        SetAndRegister(PackagingStationConfigPanelPatch.Logger, EnableUnpackagingDebug);
        SetAndRegister(MoveItemBehaviourPatches.Logger, EnableUnpackagingDebug);
        SetAndRegister(PackagerPatches.Logger, EnableUnpackagingDebug);

        SetAndRegister(GrowContainerBehaviourPatch.Logger, EnablePourerDebug);
    }

    private static void SetAndRegister(Logger logger, MelonPreferences_Entry<bool>? entry)
    {
        logger.RaiseDebug = entry?.Value ?? false;
        entry?.OnEntryValueChanged.Subscribe((_, newValue) => logger.RaiseDebug = newValue);
    }
}

internal static class SettingsConstants
{
    public const int BotanistDefaultMaxPots = 8;
    public static (int, int) BotanistBoundsMaxPots = (1, 32);
    public const int HandlerDefaultMaxStations = 3;
    public static (int, int) HandlerBoundsMaxStations = (1, 8);
    public const int HandlerDefaultMaxRoutes = 5;
    public static (int, int) HandlerBoundsMaxRoutes = (1, 16);
    public const int ChemistDefaultMaxStations = 4;
    public static (int, int) ChemistBoundsMaxStations = (1, 32);
    public const int CleanerDefaultMaxBins = 6;
    public static (int, int) CleanerBoundsMaxBins = (1, 32);
}