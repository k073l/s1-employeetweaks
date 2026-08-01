using System.Collections;
using MelonLoader;
using MelonLoader.Utils;
using Semver;
using UnityEngine;
using UnityEngine.UI;
using Object = UnityEngine.Object;

namespace EmployeeTweaks.Helpers;

public class DependenciesChecker
{
    private static readonly Logger Logger = new("DependenciesChecker");

    private readonly List<DependencyInfo> _dependencies = [];
    private List<MissingDependencyInfo> _missingDependencies = [];

    public bool ShowMenuBanner { get; set; } = true;
    public bool ShowBannerIfOptionalOnly { get; set; } = false;
    public bool UnloadIfMissingRequired { get; set; } = false;

    public void AddDependency(DependencyInfo dependency)
    {
        _dependencies.Add(dependency);
    }

    public void ProcessAndAlert()
    {
        CheckDependencies();
        if (_missingDependencies.Count == 0)
            return;
        PrintMissing();
        if (ShowMenuBanner)
            ShowBanner();
        MelonCoroutines.Start(DelayedUnload());
    }

    private void CheckDependencies()
    {
        _missingDependencies = [];
        foreach (var dependency in _dependencies)
        {
            var missingInfo = IsPresent(dependency);
            if (missingInfo != null)
                _missingDependencies.Add(missingInfo);
        }
    }

    private void PrintMissing()
    {
        Logger.Msg("Missing dependencies:");
        foreach (var missing in _missingDependencies)
        {
            if (missing.IsRequired)
                Logger.Error($"- {missing.Name} (min version {missing.Version}): required - {missing.Reason}");
            else
                Logger.Warning($"- {missing.Name} (min version {missing.Version}): optional - {missing.Reason}");
            if (missing.Urls.Count == 0) continue;
            Logger.Msg("  Possible sources:");
            foreach (var url in missing.Urls)
            {
                Logger.Msg($"  - {url.SourceName}: {url.Url}");
            }
        }
    }

    private void ShowBanner()
    {
        // Wire events
        MelonEvents.OnSceneWasLoaded.Subscribe((_, name) =>
        {
            if (name != "Menu") return;
            MissingDepsPanelCreator.Show(this, _missingDependencies);
        });
        MelonEvents.OnSceneWasUnloaded.Subscribe((_, name) =>
        {
            if (name != "Menu") return;
            MissingDepsPanelCreator.Hide();
        });
        // Show panel (we're likely in Menu anyway, but it was loaded earlier)
        MissingDepsPanelCreator.Show(this, _missingDependencies);
    }

    private IEnumerator DelayedUnload()
    {
        yield return new WaitForSeconds(5f);
        if (UnloadIfMissingRequired && _missingDependencies.Any(d => d.IsRequired))
        {
            Logger.Warning(
                "Missing required dependencies detected and UnloadIfMissingRequired is true, unloading mod.");
            Melon<EmployeeTweaks>.Instance.Unregister("Missing required dependencies");
        }
    }

    private static MissingDependencyInfo? IsPresent(DependencyInfo dependency)
    {
        var foundInDomain = AppDomain.CurrentDomain.GetAssemblies()
            .FirstOrDefault(assembly => assembly.GetName().Name == dependency.AssemblyName);
        if (foundInDomain == null)
        {
            Logger.Debug($"{dependency.AssemblyName} not found in AppDomain, marking as missing.");
            return new MissingDependencyInfo(dependency.Name, dependency.Version, dependency.Urls,
                dependency.IsRequired, "Assembly not found");
        }

        var foundInMelons =
            EmployeeTweaks.RegisteredMelons.FirstOrDefault(m => m.MelonAssembly.Assembly == foundInDomain);
        if (foundInMelons == null)
        {
            Logger.Debug($"{foundInDomain.GetName().Name} wasn't found in RegisteredMelons.");
            var assemblyVersion = foundInDomain.GetName().Version;
            if (assemblyVersion == null)
            {
                Logger.Debug($"{dependency.AssemblyName} version is not available.");
                return new MissingDependencyInfo(dependency.Name, dependency.Version, dependency.Urls,
                    dependency.IsRequired, "Cannot verify version");
            }

            if (!Version.TryParse(dependency.Version, out var dependencyVersion))
            {
                Logger.Debug($"{dependency.AssemblyName} version is not a valid version.");
                return new MissingDependencyInfo(dependency.Name, dependency.Version, dependency.Urls,
                    dependency.IsRequired, "Invalid version format");
            }

            if (assemblyVersion < dependencyVersion)
            {
                Logger.Debug($"{dependency.AssemblyName} version is too old.");
                return new MissingDependencyInfo(dependency.Name, dependency.Version, dependency.Urls,
                    dependency.IsRequired, $"Version {assemblyVersion} found, but {dependency.Version} required");
            }

            return null;
        }

        var version = foundInMelons.Info.SemanticVersion;
        if (version == null)
        {
            Logger.Debug($"{foundInDomain.GetName().Name}'s version {version} is not SemVer.");
            // Try regular version parsing as a fallback
            if (!Version.TryParse(dependency.Version, out var dependencyVersion))
            {
                Logger.Debug($"{dependency.AssemblyName} version is not a valid version.");
                return new MissingDependencyInfo(dependency.Name, dependency.Version, dependency.Urls,
                    dependency.IsRequired, "Invalid version format");
            }

            if (!Version.TryParse(foundInMelons.Info.Version, out var melonVersion))
            {
                Logger.Debug($"{dependency.AssemblyName} version is not a valid version.");
                return new MissingDependencyInfo(dependency.Name, dependency.Version, dependency.Urls,
                    dependency.IsRequired, "Invalid version format");
            }

            if (melonVersion < dependencyVersion)
            {
                Logger.Debug($"{dependency.AssemblyName} version is too old.");
                return new MissingDependencyInfo(dependency.Name, dependency.Version, dependency.Urls,
                    dependency.IsRequired, $"Version {melonVersion} found, but {dependency.Version} required");
            }

            return null;
        }

        if (!SemVersion.TryParse(dependency.Version, out var semVersion))
        {
            Logger.Debug($"{dependency.AssemblyName} version is not a valid version.");
            return new MissingDependencyInfo(dependency.Name, dependency.Version, dependency.Urls,
                dependency.IsRequired, "Invalid version format");
        }

        if (version < semVersion)
        {
            Logger.Debug($"{dependency.AssemblyName} version is too old.");
            return new MissingDependencyInfo(dependency.Name, dependency.Version, dependency.Urls,
                dependency.IsRequired, $"Version {version} found, but {dependency.Version} required");
        }

        return null;
    }

    public class MissingDependencyInfo(
        string name,
        string version,
        List<DependencyUrl> urls,
        bool isRequired,
        string reason)
    {
        public string Name { get; set; } = name;
        public string Version { get; set; } = version;
        public List<DependencyUrl> Urls { get; set; } = urls;
        public bool IsRequired { get; set; } = isRequired;
        public string Reason { get; set; } = reason;
    }
}

public class DependencyInfo
{
    public DependencyInfo(string name,
        string version,
        string? assemblyName = null,
        List<DependencyUrl>? urls = null,
        bool isRequired = true)
    {
        Name = name;
        AssemblyName = assemblyName ?? name;
        Version = version;
        IsRequired = isRequired;
        Urls = urls ?? [];
    }

    public DependencyInfo()
    {
    }

    public string Name { get; set; }
    public string AssemblyName { get; set; }
    public string Version { get; set; }
    public bool IsRequired { get; set; }
    public List<DependencyUrl> Urls { get; set; }
}

public class DependencyUrl
{
    public DependencyUrl(string sourceName, string url)
    {
        SourceName = sourceName;
        Url = url;
    }

    public DependencyUrl()
    {
    }

    public string SourceName { get; set; }
    public string Url { get; set; }
}

internal static class MissingDepsPanelCreator
{
    private static GameObject? _canvasObject;

    public static void Show(DependenciesChecker instance, List<DependenciesChecker.MissingDependencyInfo> deps)
    {
        Hide();

        _canvasObject = new GameObject("MissingDependenciesCanvas");
        var canvas = _canvasObject.AddComponent<Canvas>();
        canvas.renderMode = RenderMode.ScreenSpaceOverlay;
        canvas.sortingOrder = 9999;
        _canvasObject.AddComponent<GraphicRaycaster>();

        var panel = new GameObject("MissingDependenciesPanel");
        panel.transform.SetParent(_canvasObject.transform, false);

        var rect = panel.AddComponent<RectTransform>();
        rect.anchorMin = new Vector2(1f, 0.5f);
        rect.anchorMax = new Vector2(1f, 0.5f);
        rect.pivot = new Vector2(1f, 0.5f);
        rect.anchoredPosition = new Vector2(-20f, 0f);
        rect.sizeDelta = new Vector2(420f, 0f);

        var bg = panel.AddComponent<Image>();
        bg.color = new Color(0.08f, 0.08f, 0.08f, 0.95f);

        var fitter = panel.AddComponent<ContentSizeFitter>();
        fitter.verticalFit = ContentSizeFitter.FitMode.PreferredSize;

        var layout = panel.AddComponent<VerticalLayoutGroup>();
        layout.padding = new RectOffset(16, 16, 12, 12);
        layout.spacing = 6f;
        layout.childAlignment = TextAnchor.UpperLeft;
        layout.childForceExpandWidth = true;

        var header = new GameObject("Header");
        header.transform.SetParent(panel.transform, false);
        var headerText = header.AddComponent<Text>();
        headerText.text = "Missing Dependencies";
        headerText.font = Resources.GetBuiltinResource<Font>("Arial.ttf");
        headerText.fontSize = 18;
        headerText.fontStyle = FontStyle.Bold;
        headerText.color = Color.white;
        headerText.alignment = TextAnchor.MiddleLeft;

        if (deps.All(d => !d.IsRequired) && !instance.ShowBannerIfOptionalOnly)
        {
            // If all dependencies are optional and the instance is set to not show the banner in that case, skip showing the panel.
            Hide();
            return;
        }

        foreach (var dep in deps)
        {
            var row = new GameObject("Dep_" + dep.Name);
            row.transform.SetParent(panel.transform, false);
            row.AddComponent<LayoutElement>().minHeight = 20f;

            var label = row.AddComponent<Text>();
            label.text = $"  {dep.Name} (min v{dep.Version}) - {dep.Reason}";
            label.font = Resources.GetBuiltinResource<Font>("Arial.ttf");
            label.fontSize = 15;
            label.color = dep.IsRequired ? new Color(1f, 0.4f, 0.4f) : new Color(1f, 0.8f, 0.3f);
            label.alignment = TextAnchor.UpperLeft;
        }

        var footer = new GameObject("Footer");
        footer.transform.SetParent(panel.transform, false);
        var footerText = footer.AddComponent<Text>();
        footerText.text =
            $"See the console (or log at {Path.Combine(MelonEnvironment.MelonLoaderLogsDirectory, "Latest.log")}) for details.";
        footerText.font = Resources.GetBuiltinResource<Font>("Arial.ttf");
        footerText.fontSize = 14;
        footerText.color = new Color(0.6f, 0.6f, 0.6f);
        footerText.alignment = TextAnchor.UpperLeft;
    }

    public static void Hide()
    {
        if (_canvasObject != null)
        {
            Object.Destroy(_canvasObject);
            _canvasObject = null;
        }
    }
}