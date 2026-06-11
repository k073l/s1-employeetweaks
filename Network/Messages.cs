using SteamNetworkLib.Models;

namespace EmployeeTweaks.Network;

/// <summary>
/// Message sent when packaging station is designated/undesignated as unpackaging station.
/// </summary>
public class SetUnpackageStation : P2PMessage
{
    public override string MessageType => "SetUnpackageStation";

    public string StationGuid { get; set; }
    
    public bool IsUnpackageStation { get; set; }

    public override byte[] Serialize()
    {
        var json = CreateJsonBase(
            $"\"StationGuid\":{StationGuid},\"IsUnpackageStation\":\"{IsUnpackageStation}\"");
        return System.Text.Encoding.UTF8.GetBytes(json);
    }

    public override void Deserialize(byte[] data)
    {
        var json = System.Text.Encoding.UTF8.GetString(data);
        ParseJsonBase(json);
        StationGuid = ExtractJsonValue(json, "StationGuid");
        IsUnpackageStation = bool.Parse(ExtractJsonValue(json, "IsUnpackageStation"));
    }
}