using System.Collections;
using EmployeeTweaks.Helpers;
using EmployeeTweaks.Persistence;
using MelonLoader;
using SteamNetworkLib;
using SteamNetworkLib.Events;
using UnityEngine;
using Logger = EmployeeTweaks.Helpers.Logger;
#if MONO
using Steamworks;
#else
using Il2CppSteamworks;
#endif

namespace EmployeeTweaks.Network;

internal class NetworkManager: INetworkManager
{
    private SteamNetworkClient? _client;
    private Logger _logger;

    public bool IsInLobby => _client?.IsInLobby ?? false;
    public bool IsHost => _client?.IsHost ?? false;
    public bool IsSingleplayer => !IsInLobby;
    public bool IsServer => IsHost || IsSingleplayer;

    public NetworkManager()
    {
        _logger = new Logger("NetworkManager");
    }

    public bool Initialize()
    {
        try
        {
            _client = new SteamNetworkClient();
            if (!_client.Initialize())
            {
                _logger.Error("Failed to initialize SteamNetworkClient.");
                return false;
            }

            // Initialize preferences network sync here
            if (!Melon<EmployeeTweaks>.Instance.SettingsRegistry.InitializeNetwork(_client))
            {
                _logger.Error("Failed to initialize networked preferences. Preferences will be local-only.");
                return false;
            }
            RegisterMessageHandlers();
            RegisterLobbyHandlers();

            _logger.Msg("SteamNetworkClient initialized successfully.");
            return true;
        }
        catch (Exception e)
        {
            _logger.Warning($"Exception during SteamNetworkClient initialization: {e}");
            _client = null;
            return false;
        }
    }

    public void RegisterLoggerSettings()
    {
        _logger.RaiseDebug = Melon<EmployeeTweaks>.Instance.SettingsRegistry.EnableNetworkDebug?.Value ?? false;
        Melon<EmployeeTweaks>.Instance.SettingsRegistry.EnableNetworkDebug?
            .OnEntryValueChanged.Subscribe((_, newValue) => _logger.RaiseDebug = newValue);
    }

    private void RegisterMessageHandlers()
    {
        _client?.RegisterMessageHandler<SetUnpackageStation>(OnSetUnpackageStation);
    }
    
    private void RegisterLobbyHandlers()
    {
        if (_client == null) return;
        _client.OnMemberJoined += OnMemberJoined;
    }
    
    public void Update()
    {
        _client?.ProcessIncomingMessages();
    }

    public void Dispose()
    {
        _client?.Dispose();
    }

    public void BroadcastStation(string stationGuid, bool value)
    {
        var message = new SetUnpackageStation()
        {
            StationGuid = stationGuid,
            IsUnpackageStation = value
        };
        _logger.Debug($"Broadcasting station change: StationGUID={stationGuid}, value={value}");
        _client?.BroadcastMessage(message);
    }

    private async void OnMemberJoined(object sender, MemberJoinedEventArgs e)
    {
        if (!IsHost) return;
        if (_client == null) return;
        _logger.Msg($"Member joined: {e.Member.DisplayName}");
        // if we don't have unpackage save data, we're in menu and initial sync
        // will be performed when host starts the save.
        if (UnpackageSave.Instance?.UnpackageStations == null) return;
        // we're already in game and someone joined late
        foreach (var key in UnpackageSave.Instance.UnpackageStations.Keys)
        {
            var message = new SetUnpackageStation()
            {
                StationGuid = key.ToString(),
                IsUnpackageStation = UnpackageSave.Instance.UnpackageStations[key]
            };
            await _client.SendMessageToPlayerAsync(e.Member.SteamId, message);
        }
    }

    private void OnSetUnpackageStation(SetUnpackageStation message, CSteamID cSteamID)
    {
        // don't send to yourself
        if (cSteamID == _client?.LocalPlayerId) return;
        _logger.Debug(
            $"Received OnSetUnpackageStation: StationGUID={message.StationGuid}, value={message.IsUnpackageStation}");
        MelonCoroutines.Start(ProcessMessage());

        IEnumerator ProcessMessage()
        {
            yield return ExponentialBackoff(
                () => UnityEngine.SceneManagement.SceneManager.GetSceneByName("Main").isLoaded, 1f, 30f, 300f);
            try
            {
                var save = UnpackageSave.Instance;
                var guid = Guid.Parse(message.StationGuid);
                if (save?.UnpackageStations == null) yield break;
                if (!save.UnpackageStations.TryAdd(guid, true))
                    save.UnpackageStations[guid] = message.IsUnpackageStation;
                _logger.Msg($"Set unpackage station {message.StationGuid} to {message.IsUnpackageStation}");
            }
            catch (Exception ex)
            {
                _logger.Error($"Failed to set unpackage station from network: {ex}");
            }
        }
    }

    private static IEnumerator ExponentialBackoff(Func<bool> predicate, float initialDelay, float finalDelay,
        float timeout)
    {
        var delay = initialDelay;
        var elapsed = 0f;

        while (!predicate() && elapsed < timeout)
        {
            yield return new WaitForSeconds(delay);

            elapsed += delay;
            delay = Mathf.Min(delay * 2f, finalDelay);
        }
    }
}