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
            EmployeeAssignsCategory.CreateEntry("EmployeeTweaksBotanistMaxPots", 8, "Botanist Max Pots",
                "Maximum number of pots a botanist can be assigned to. Changes require a restart.",
                validator: new ValueRange<int>(1, 50));
        HandlerMaxStations =
            EmployeeAssignsCategory.CreateEntry("EmployeeTweaksHandlerMaxStations", 3, "Handler Max Stations",
                "Maximum number of stations a packager can be assigned to. Changes require a restart.",
                validator: new ValueRange<int>(1, 8));
        HandlerMaxRoutes =
            EmployeeAssignsCategory.CreateEntry("EmployeeTweaksHandlerMaxRoutes", 5, "Handler Max Routes",
                "Maximum number of routes a packager can be assigned to. Changes require a restart.",
                validator: new ValueRange<int>(1, 12));
        ChemistMaxStations =
            EmployeeAssignsCategory.CreateEntry("EmployeeTweaksChemistMaxStations", 4, "Chemist Max Stations",
                "Maximum number of stations a chemist can be assigned to. Changes require a restart.",
                validator: new ValueRange<int>(1, 12));
        CleanerMaxBins =
            EmployeeAssignsCategory.CreateEntry("EmployeeTweaksCleanerMaxBins", 6, "Cleaner Max Trash Cans",
                "Maximum number of trash cans a cleaner can be assigned to. Changes require a restart.",
                validator: new ValueRange<int>(1, 12));
    }
}