using System.Diagnostics;
using MelonLoader;
using MelonLoader.Preferences;

namespace EmployeeTweaks.Helpers;

/// <summary>
/// Extension methods for <see cref="MelonPreferences_Category"/> that add network-aware
/// preference entry creation backed by <see cref="NetworkedMelonEntry{T}"/>.
/// <code>
/// var entry = category.GetOrCreateNetworkedEntry("Volume", 0.5f, client, options);
/// entry.OnEntryValueChanged.Subscribe((oldVal, newVal) => { /* react */ });
/// </code>
/// </summary>
public static class MelonExtensions
{
    /// <summary>
    /// Caches created <see cref="NetworkedMelonEntry"/> instances keyed by category identifier.
    /// </summary>
    private static readonly Dictionary<string, List<NetworkedMelonEntry>> NetworkedMelonEntries = new();

    /// <summary>
    /// Gets an existing networked entry or creates one, wrapping the underlying
    /// <see cref="MelonPreferences_Entry{T}"/> with optional network sync.
    /// </summary>
    /// <param name="category">The preferences' category.</param>
    /// <param name="identifier">Entry identifier.</param>
    /// <param name="defaultValue">Default value if the entry doesn't exist yet.</param>
    /// <param name="client">Boxed <c>SteamNetworkClient</c>, or <see langword="null"/> for local-only.</param>
    /// <param name="options">Boxed <c>NetworkSyncOptions</c>, or <see langword="null"/> for local-only.</param>
    /// <param name="announce">If <see langword="true"/> and networking is wired, reads/writes route through the sync var.</param>
    /// <param name="displayName">Optional display name for the entry. If <see langword="null"/>, the identifier is used.</param>
    /// <param name="description">Optional description for the entry. If <see langword="null"/>, no description is set.</param>
    /// <param name="isHidden">If <see langword="true"/>, the entry is hidden.</param>
    /// <param name="dontSaveDefault">If <see langword="true"/>, the default value is not saved to disk.</param>
    /// <param name="validator">Optional value validator. If <see langword="null"/>, no validation is performed.</param>
    /// <param name="oldIdentifier">Optional old identifier for migration. If <see langword="null"/>, no migration is attempted.</param>
    /// <typeparam name="T">The entry value type.</typeparam>
    public static NetworkedMelonEntry<T> GetOrCreateNetworkedEntry<T>(this MelonPreferences_Category category,
        string identifier,
        T defaultValue, object? client, object? options, bool announce = true,
        string? displayName = null, string? description = null, bool isHidden = false, bool dontSaveDefault = false,
        ValueValidator? validator = null, string? oldIdentifier = null)
    {
        var entry = category.GetOrCreateEntry(identifier, defaultValue, displayName, description, isHidden,
            dontSaveDefault, validator, oldIdentifier);
        return category.GetOrCreateNetworkedEntry(entry, client, options, announce);
    }

    private static NetworkedMelonEntry<T> GetOrCreateNetworkedEntry<T>(this MelonPreferences_Category category,
        MelonPreferences_Entry<T> entry, object? client, object? options, bool announce = true)
    {
        if (NetworkedMelonEntries.TryGetValue(category.Identifier, out var networkedMelonEntries))
        {
            if (networkedMelonEntries.FirstOrDefault(e => e.Identifier == entry.Identifier)
                is NetworkedMelonEntry<T> existing)
            {
                return existing;
            }
        }

        var networked = new NetworkedMelonEntry<T>(entry, client, options)
        {
            Announce = announce
        };
        if (!NetworkedMelonEntries.ContainsKey(category.Identifier))
            NetworkedMelonEntries.Add(category.Identifier, []);
        NetworkedMelonEntries[category.Identifier].Add(networked);
        return networked;
    }

    /// <summary>
    /// Gets an existing <see cref="MelonPreferences_Entry{T}"/> or creates one if it doesn't exist.
    /// </summary>
    public static MelonPreferences_Entry<T> GetOrCreateEntry<T>(this MelonPreferences_Category category,
        string identifier, T defaultValue, string? displayName = null, string? description = null,
        bool isHidden = false, bool dontSaveDefault = false, ValueValidator? validator = null,
        string? oldIdentifier = null)
    {
        if (category.HasEntry(identifier)) return category.GetEntry<T>(identifier);
        return category.CreateEntry(identifier, defaultValue, displayName, description, isHidden, dontSaveDefault,
            validator, oldIdentifier);
    }
    
    /// <summary>
    /// Logs a debug message to the console.
    /// Can provide caller info, otherwise it's just a convenience method to MelonDebug.
    /// This method only works when running with --melonloader.debug
    /// </summary>
    /// <param name="logger">Logger instance, isn't used.</param>
    /// <param name="message">The message to log.</param>
    /// <param name="stacktrace">Whether to include the stack trace in the log message. Defaults to true.</param>
    public static void Debug(
        this MelonLogger.Instance logger,
        string message,
        bool stacktrace = true
    )
    {
        MelonDebug.Msg(stacktrace ? $"[{GetCallerInfo()}] {message}" : message);
    }

    private static string GetCallerInfo()
    {
        var stackTrace = new StackTrace();
        for (int i = 2; i < stackTrace.FrameCount; i++)
        {
            var frame = stackTrace.GetFrame(i);
            var method = frame.GetMethod();
            if (method?.DeclaringType == null)
                continue;

            return $"{method.DeclaringType.FullName}.{method.Name}";
        }

        return "unknown";
    }
}
