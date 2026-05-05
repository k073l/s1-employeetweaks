using MelonLoader;
using MelonLoader.Preferences;

namespace EmployeeTweaks;

internal class SettingsRegistry
{
    internal MelonPreferences_Category EmployeeCapacityCategory;

    internal MelonPreferences_Entry<bool> EnableCapacityAndDebug;

    internal MelonPreferences_Entry<bool> DrawDebugArea;

    internal HashSet<MelonPreferences_Entry<int>> EmployeeCapacities = [];

    internal MelonPreferences_Category EmployeeAssignsCategory;

    internal MelonPreferences_Entry<bool> EnableAssigns;

    internal MelonPreferences_Entry<int> BotanistMaxPots;

    internal MelonPreferences_Entry<int> HandlerMaxStations;

    internal MelonPreferences_Entry<int> HandlerMaxRoutes;

    internal MelonPreferences_Entry<int> ChemistMaxStations;

    internal MelonPreferences_Entry<int> CleanerMaxBins;

    public SettingsRegistry()
    {
        InitializeCapacityCategory();
        InitializeAssignsCategory();
    }

    private void InitializeCapacityCategory()
    {
        EmployeeCapacityCategory =
            MelonPreferences.CreateCategory("EmployeeTweaksEmployeeCapacity", "Employee Capacities");
        EnableCapacityAndDebug =
            EmployeeCapacityCategory.CreateEntry("EmployeeTweaksEnableCapacityAndDebug", true, "Enable Category",
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
            EmployeeAssignsCategory.CreateEntry("EmployeeTweaksEnableAssigns", true, "Enable Category",
                "Enables employee assigns capacity modifications");
        BotanistMaxPots =
            EmployeeAssignsCategory.CreateEntry("EmployeeTweaksBotanistMaxPots",
                SettingsConstants.BotanistDefaultMaxPots, "Botanist Max Pots",
                $"Maximum number of pots a botanist can be assigned to (allowed values from {SettingsConstants.BotanistBoundsMaxPots.Item1} to {SettingsConstants.BotanistBoundsMaxPots.Item2}). Changes require a restart.",
                validator: new ValueRange<int>(SettingsConstants.BotanistBoundsMaxPots.Item1,
                    SettingsConstants.BotanistBoundsMaxPots.Item2));
        HandlerMaxStations =
            EmployeeAssignsCategory.CreateEntry("EmployeeTweaksHandlerMaxStations",
                SettingsConstants.HandlerDefaultMaxStations, "Handler Max Stations",
                $"Maximum number of stations a packager can be assigned to (allowed values from {SettingsConstants.HandlerBoundsMaxStations.Item1} to {SettingsConstants.HandlerBoundsMaxStations.Item2}). Changes require a restart.",
                validator: new ValueRange<int>(SettingsConstants.HandlerBoundsMaxStations.Item1,
                    SettingsConstants.HandlerBoundsMaxStations.Item2));
        HandlerMaxRoutes =
            EmployeeAssignsCategory.CreateEntry("EmployeeTweaksHandlerMaxRoutes",
                SettingsConstants.HandlerDefaultMaxRoutes, "Handler Max Routes",
                $"Maximum number of routes a packager can be assigned to (allowed values from {SettingsConstants.HandlerBoundsMaxRoutes.Item1} to {SettingsConstants.HandlerBoundsMaxRoutes.Item2}). Changes require a restart.",
                validator: new ValueRange<int>(SettingsConstants.HandlerBoundsMaxRoutes.Item1,
                    SettingsConstants.HandlerBoundsMaxRoutes.Item2));
        ChemistMaxStations =
            EmployeeAssignsCategory.CreateEntry("EmployeeTweaksChemistMaxStations",
                SettingsConstants.ChemistDefaultMaxStations, "Chemist Max Stations",
                $"Maximum number of stations a chemist can be assigned to (allowed values from {SettingsConstants.ChemistBoundsMaxStations.Item1} to {SettingsConstants.ChemistBoundsMaxStations.Item2}). Changes require a restart.",
                validator: new ValueRange<int>(SettingsConstants.ChemistBoundsMaxStations.Item1,
                    SettingsConstants.ChemistBoundsMaxStations.Item2));
        CleanerMaxBins =
            EmployeeAssignsCategory.CreateEntry("EmployeeTweaksCleanerMaxBins", SettingsConstants.CleanerDefaultMaxBins,
                "Cleaner Max Trash Cans",
                $"Maximum number of trash cans a cleaner can be assigned to (allowed values from {SettingsConstants.CleanerBoundsMaxBins.Item1} to {SettingsConstants.CleanerBoundsMaxBins.Item2}). Changes require a restart.",
                validator: new ValueRange<int>(SettingsConstants.CleanerBoundsMaxBins.Item1,
                    SettingsConstants.CleanerBoundsMaxBins.Item2));
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