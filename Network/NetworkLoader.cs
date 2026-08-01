using System.Runtime.CompilerServices;
using EmployeeTweaks.Helpers;

namespace EmployeeTweaks.Network;

internal static class NetworkLoader
{
    private static readonly Logger Logger = new Logger("NetworkLoader");

    internal static INetworkManager? Create()
    {
        if (AppDomain.CurrentDomain.GetAssemblies()
            .All(a => a.GetName().Name != "SteamNetworkLib"))
            return null;

        return CreateInternal();
    }

    [MethodImpl(MethodImplOptions.NoInlining)]
    private static INetworkManager? CreateInternal()
    {
        try
        {
            var nm = new NetworkManager();
            nm.Initialize();
            return nm;
        }
        catch (Exception e)
        {
            Logger.Warning(
                $"Failed to initialize network manager: {e}. If you're playing singleplayer, you may disregard this message.");
            return null;
        }
    }
}
