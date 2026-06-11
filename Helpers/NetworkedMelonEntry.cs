using System.Reflection;
using MelonLoader;
using MelonLoader.Preferences;

namespace EmployeeTweaks.Helpers;

/// <summary>
/// Non-generic base for <see cref="NetworkedMelonEntry{T}"/>.
/// Wraps a <see cref="MelonPreferences_Entry"/> and forwards its public surface for drop-in compatibility.
/// Owns the network sync infrastructure shared across all typed instances -
/// assembly loading, sync var references, and the core callback logic.
/// </summary>
public class NetworkedMelonEntry(MelonPreferences_Entry entry)
{
    #region Shared assembly cache (once per app domain)

    /// <summary>
    /// Cached SteamNetworkLib assembly reference, loaded once and shared across all typed entries.
    /// </summary>
    protected static Assembly? SteamNetworkLibAssembly;

    /// <summary>
    /// Locates and caches the SteamNetworkLib assembly from the current app domain, if present.
    /// </summary>
    protected static void LoadSteamNetworkLib()
    {
        if (SteamNetworkLibAssembly != null) return;
        var assembly = AppDomain.CurrentDomain.GetAssemblies()
            .FirstOrDefault(a => a.GetName().Name == "SteamNetworkLib");
        if (assembly == null) return;

        SteamNetworkLibAssembly = assembly;
    }

    #endregion

    #region Instance networking state (set by typed subclass during init)

    /// <summary>
    /// The <c>HostSyncVar{T}</c> instance, boxed to <see cref="object"/>.
    /// </summary>
    protected object? SyncVar;

    /// <summary>
    /// Cached <c>HostSyncVar{T}.Value</c> <see cref="PropertyInfo"/>.
    /// </summary>
    protected PropertyInfo? SyncVarValueProperty;

    /// <summary>
    /// Cached <c>HostSyncVar{T}.CanWrite</c> <see cref="PropertyInfo"/>.
    /// </summary>
    protected PropertyInfo? SyncVarCanWriteProperty;

    #endregion

    #region Forwarded MelonPreferences_Entry members

    /// <summary>
    /// The underlying MelonLoader preference entry.
    /// </summary>
    protected readonly MelonPreferences_Entry Entry = entry;

    public string Identifier => Entry.Identifier;

    public string DisplayName
    {
        get => Entry.DisplayName;
        set => Entry.DisplayName = value;
    }

    public string Description
    {
        get => Entry.Description;
        set => Entry.Description = value;
    }

    public string Comment
    {
        get => Entry.Comment;
        set => Entry.Comment = value;
    }

    public bool IsHidden
    {
        get => Entry.IsHidden;
        set => Entry.IsHidden = value;
    }

    public bool DontSaveDefault
    {
        get => Entry.DontSaveDefault;
        set => Entry.DontSaveDefault = value;
    }

    public MelonPreferences_Category Category => Entry.Category;
    public ValueValidator Validator => Entry.Validator;

    /// <inheritdoc cref="MelonPreferences_Entry.BoxedEditedValue"/>
    public object BoxedEditedValue
    {
        get => Entry.BoxedEditedValue;
        set => Entry.BoxedEditedValue = value;
    }

    /// <inheritdoc cref="MelonPreferences_Entry.OnEntryValueChangedUntyped"/>
    public MelonEvent<object, object> OnEntryValueChangedUntyped => Entry.OnEntryValueChangedUntyped;

    public string GetEditedValueAsString() => Entry.GetEditedValueAsString();
    public string GetDefaultValueAsString() => Entry.GetDefaultValueAsString();
    public string GetValueAsString() => Entry.GetValueAsString();
    public Type GetReflectedType() => Entry.GetReflectedType();
    public string GetExceptionMessage(string submsg) => Entry.GetExceptionMessage(submsg);

    #endregion

    #region Virtual members (overridden by typed subclass)

    /// <summary>
    /// If <see langword="true"/> and networking is initialized, reads/writes route through
    /// the network sync var. Defaults to <see langword="true"/>.
    /// </summary>
    public bool Announce { get; set; } = true;

    /// <summary>
    /// Boxed access to the entry's value.
    /// When networked, routes through the <c>HostSyncVar{T}.Value</c>.
    /// Override in <see cref="NetworkedMelonEntry{T}"/> for typed convenience.
    /// </summary>
    public virtual object BoxedValue
    {
        get
        {
            if (!Announce || SyncVar == null) return Entry.BoxedValue;
            return SyncVarValueProperty.GetValue(SyncVar);
        }
        set
        {
            if (!Announce || SyncVar == null)
            {
                Entry.BoxedValue = value;
                return;
            }

            SyncVarValueProperty.SetValue(SyncVar, value);
        }
    }

    /// <summary>
    /// Resets the value to its default.
    /// On the lobby host the default is written to the sync var (propagates to clients).
    /// On clients a warning is logged and only the local entry is reset.
    /// Override in <see cref="NetworkedMelonEntry{T}"/> for typed optimization.
    /// </summary>
    public virtual void ResetToDefault()
    {
        if (!Announce || SyncVar == null)
        {
            Entry.ResetToDefault();
            return;
        }

        if ((bool)SyncVarCanWriteProperty.GetValue(SyncVar))
        {
            Entry.ResetToDefault();
            SyncVarValueProperty.SetValue(SyncVar, Entry.BoxedValue);
        }
        else
        {
            MelonLogger.Warning($"Resetting clientside entry '{Identifier}' - real value is host-controlled");
            Entry.ResetToDefault();
        }
    }

    #endregion

    #region Sync var callback infrastructure

    /// <summary>
    /// Core callback invoked when the <c>HostSyncVar{T}.OnValueChanged</c> event fires.
    /// On the host (<c>CanWrite</c> == <see langword="true"/>), validates the new value and either updates the
    /// MelonLoader entry or reverts the sync var. On clients, fires the untyped entry event
    /// and calls <see cref="OnClientSyncUpdate"/> so the typed subclass can also notify subscribers.
    /// </summary>
    protected void HandleSyncVarChanged(object oldValue, object newValue)
    {
        // on host, check if valid before writing to melonentry,
        // if invalid write old value back to syncvar (which will trigger this event again, but with a valid value)
        if ((bool)SyncVarCanWriteProperty.GetValue(SyncVar))
        {
            if (Entry.Validator?.IsValid(newValue) ?? true)
                Entry.BoxedValue = newValue;
            else
                SyncVarValueProperty.SetValue(SyncVar, oldValue);
        }
        // on client, if valid just invoke the melonentry event (but don't write to melonentry)
        else
        {
            if (Entry.Validator?.IsValid(newValue) ?? true)
            {
                OnClientSyncUpdate(oldValue, newValue);
            }
        }
    }

    /// <summary>
    /// Hook for <see cref="NetworkedMelonEntry{T}"/> to fire the typed
    /// <see cref="OnEntryValueChanged"/> event on clients.
    /// </summary>
    protected virtual void OnClientSyncUpdate(object oldValue, object newValue) =>
        Entry.OnEntryValueChangedUntyped.Invoke(oldValue, newValue);

    #endregion
}

/// <summary>
/// A transparent, drop-in replacement for <see cref="MelonPreferences_Entry{T}"/> that optionally
/// synchronizes its value over Steam lobbies via <c>SteamNetworkLib.HostSyncVar{T}</c>.
/// <code>
/// // Local-only usage (same as MelonPreferences_Entry{T}):
/// var entry = new NetworkedMelonEntry&lt;bool&gt;(melonEntry);
/// bool val = entry.Value;
///
/// // Networked usage (SteamNetworkClient passed from NetworkManager):
/// TestEntry = TestCategory.GetOrCreateNetworkedEntry("TestEntry", false, client, options);
/// TestEntry.OnEntryValueChanged.Subscribe((oldVal, newVal) => { /* react */ });
/// </code>
/// When <see cref="NetworkedMelonEntry.Announce"/> is <see langword="true"/> and networking is wired, reads/writes go through
/// the host-authoritative <c>HostSyncVar{T}</c>. Non-host writes are silently ignored by
/// SteamNetworkLib. Falls back to the local entry transparently if SteamNetworkLib is absent.
/// </summary>
/// <typeparam name="T">The type of the preference value.</typeparam>
public class NetworkedMelonEntry<T> : NetworkedMelonEntry
{
    private MelonPreferences_Entry<T> TypedEntry => (MelonPreferences_Entry<T>)Entry;

    #region Properties

    /// <summary>
    /// Gets or sets the entry value.
    /// When networked, reads from the host-synchronised <c>HostSyncVar{T}</c>
    /// and writes propagate only if the local player is the lobby host.
    /// </summary>
    public T Value
    {
        get
        {
            if (!Announce || SyncVar == null) return TypedEntry.Value;
            return (T)SyncVarValueProperty.GetValue(SyncVar);
        }
        set
        {
            if (!Announce || SyncVar == null)
            {
                TypedEntry.Value = value;
                return;
            }

            SyncVarValueProperty.SetValue(SyncVar, value);
        }
    }

    /// <inheritdoc cref="MelonPreferences_Entry{T}.EditedValue"/>
    public T EditedValue
    {
        get => TypedEntry.EditedValue;
        set => TypedEntry.EditedValue = value;
    }

    /// <inheritdoc cref="MelonPreferences_Entry{T}.DefaultValue"/>
    public T DefaultValue => TypedEntry.DefaultValue;

    /// <summary>
    /// Fires when the value changes from any source (local or remote).
    /// <code>
    /// entry.OnEntryValueChanged.Subscribe((oldVal, newVal) => MelonLogger.Msg($"{oldVal} -> {newVal}"));
    /// </code>
    /// </summary>
    public MelonEvent<T, T> OnEntryValueChanged => TypedEntry.OnEntryValueChanged;

    /// <inheritdoc/>
    public override object BoxedValue
    {
        get => Value;
        set => Value = (T)value;
    }

    /// <inheritdoc/>
    public override void ResetToDefault()
    {
        if (!Announce || SyncVar == null)
        {
            TypedEntry.ResetToDefault();
            return;
        }

        if ((bool)SyncVarCanWriteProperty.GetValue(SyncVar))
            SyncVarValueProperty.SetValue(SyncVar, TypedEntry.DefaultValue);
        else
        {
            MelonLogger.Warning($"Resetting clientside entry '{Identifier}' - real value is host-controlled");
            TypedEntry.ResetToDefault();
        }
    }

    #endregion

    #region Construction and network init

    /// <summary>
    /// Wraps a MelonLoader preference entry with optional network sync.
    /// </summary>
    /// <param name="entry">The MelonLoader preference entry to wrap.</param>
    /// <param name="client">Boxed <c>SteamNetworkClient</c> for network initialization, or <see langword="null"/> to skip networking.</param>
    /// <param name="options">Boxed <c>NetworkSyncOptions</c> for the sync var, or <see langword="null"/> to skip networking.</param>
    /// <example>
    /// <code>
    /// // Without networking:
    /// var local = new NetworkedMelonEntry&lt;bool&gt;(melonEntry);
    ///
    /// // With networking:
    /// var networked = new NetworkedMelonEntry&lt;float&gt;(melonEntry, steamClient, syncOptions);
    /// </code>
    /// </example>
    public NetworkedMelonEntry(MelonPreferences_Entry<T> entry, object? client = null, object? options = null)
        : base(entry)
    {
        LoadSteamNetworkLib();
        if (SteamNetworkLibAssembly != null && client != null && options != null)
            TryInitialize(client, options);
    }

    private void TryInitialize(object client, object options)
    {
        var syncVarType = SteamNetworkLibAssembly.GetTypes()
            .FirstOrDefault(t => t.Name == "HostSyncVar`1" && t.IsGenericType);
        if (syncVarType == null) return;

        var genericSyncVarType = syncVarType.MakeGenericType(typeof(T));

        var createMethod = client.GetType().GetMethod("CreateHostSyncVar", BindingFlags.Public | BindingFlags.Instance);
        if (createMethod == null) return;

        var genericCreate = createMethod.MakeGenericMethod(typeof(T));
        SyncVar = genericCreate.Invoke(client, [$"NME_{Category.Identifier}_{Identifier}", TypedEntry.Value, options, null]);
        if (SyncVar == null) return;

        SyncVarValueProperty = genericSyncVarType.GetProperty("Value");
        if (SyncVarValueProperty == null)
        {
            SyncVar = null;
            return;
        }

        SyncVarCanWriteProperty = genericSyncVarType.GetProperty("CanWrite");
        if (SyncVarCanWriteProperty == null)
        {
            SyncVar = null;
            return;
        }

        var onValueChangedEvent = genericSyncVarType.GetEvent("OnValueChanged");
        if (onValueChangedEvent == null)
        {
            SyncVar = null;
            return;
        }

        var handlerMethod = typeof(NetworkedMelonEntry<T>).GetMethod(nameof(OnSyncVarValueChanged),
            BindingFlags.NonPublic | BindingFlags.Instance);
        if (handlerMethod == null)
        {
            SyncVar = null;
            return;
        }

        var handlerDelegate = Delegate.CreateDelegate(onValueChangedEvent.EventHandlerType, this, handlerMethod);
        onValueChangedEvent.AddEventHandler(SyncVar, handlerDelegate);
    }

    #endregion

    #region Callbacks

    /// <summary>
    /// Fires the typed <see cref="OnEntryValueChanged"/> event on clients.
    /// Called from <see cref="NetworkedMelonEntry.HandleSyncVarChanged"/>.
    /// </summary>
    protected override void OnClientSyncUpdate(object oldValue, object newValue)
    {
        if (oldValue is T old && newValue is T @new)
            TypedEntry.OnEntryValueChanged.Invoke(old, @new);
    }

    private void OnSyncVarValueChanged(T oldValue, T newValue)
    {
        HandleSyncVarChanged(oldValue, newValue);
    }

    #endregion
}
