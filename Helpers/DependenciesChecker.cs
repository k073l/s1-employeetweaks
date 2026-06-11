using MelonLoader;
using Semver;

namespace EmployeeTweaks.Helpers;

public static class DependenciesChecker
{
    private static readonly MelonLogger.Instance Logger = new($"{nameof(EmployeeTweaks)}.{nameof(DependenciesChecker)}");
    private static readonly List<Dependency> Dependencies =
    [
        new()
        {
            Name = "S1API (Forked)",
            AssemblyName = "S1API",
            MinVersion = new SemVersion(3, 0, 2),
            IsRequired = true
        },
        new()
        {
            Name = "SteamNetworkLib",
            AssemblyName = "SteamNetworkLib",
            MinVersion = new SemVersion(1, 2, 1),
            IsRequired = false
        }
    ];

    private static bool IsPresent(Dependency dependency) {
        var foundInDomain = AppDomain.CurrentDomain.GetAssemblies().FirstOrDefault(assembly => assembly.GetName().Name == dependency.AssemblyName);
        if (foundInDomain == null) return false;
        var foundInMelons = EmployeeTweaks.RegisteredMelons.FirstOrDefault(m => m.MelonAssembly.Assembly == foundInDomain);
        if (foundInMelons == null)
        {
            Logger.Debug($"{foundInDomain.GetName().Name} wasn't found in RegisteredMelons, cannot verify version. Assuming present.");
            return true;
        }

        var version = foundInMelons.Info.SemanticVersion;
        if (version == null)
        {
            Logger.Debug($"{foundInDomain.GetName().Name}'s version {version} is not SemVer. Assuming present.");
            return true;
        }
        
        return version >= dependency.MinVersion;
    }

    private static List<Dependency> GetMissing() => Dependencies.Where(dependency => !IsPresent(dependency)).ToList();

    public static void PrintMissing()
    {
        var missing = GetMissing();
        if (!missing.Any()) return;
        foreach (var dependency in missing)
        {
            if (dependency.IsRequired)
                Logger.Error($"Critical:\nRequired dependency {dependency.Name} is missing or outdated (min version {dependency.MinVersion}). Mod will not function correctly without it.");
            else
                Logger.Warning($"Optional dependency {dependency.Name} is missing or outdated (min version {dependency.MinVersion}).");
        }
    }
}

public record Dependency
{
    public string Name { get; set; }
    public string AssemblyName { get; set; }
    public SemVersion MinVersion { get; set; }
    public bool IsRequired { get; set; }
}