namespace EmployeeTweaks.Network;

internal interface INetworkManager : IDisposable
{
    bool IsInLobby { get; }
    bool IsHost { get; }
    bool IsSingleplayer { get; }
    bool IsServer { get; }
    void Update();
    void BroadcastStation(string stationGuid, bool value);
}
