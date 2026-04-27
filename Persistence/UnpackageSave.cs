using S1API.Internal.Abstraction;
using S1API.Saveables;

namespace EmployeeTweaks.Persistence;

public class UnpackageSave: Saveable
{
    [SaveableField("UnpackageStations")]
    public Dictionary<Guid, bool> UnpackageStations = [];
    
    private static UnpackageSave _instance;

    public static UnpackageSave Instance => _instance ??= new UnpackageSave();

    public UnpackageSave()
    {
        _instance = this;
    }
    
#if MONO
    public bool TryGetValue(Guid guid, out bool value)
#else
    public bool TryGetValue(Il2CppSystem.Guid guid, out bool value)
#endif
    {
        #if MONO
        return UnpackageStations.TryGetValue(guid, out value);
        #else
        var guidKey = Guid.Parse(guid.ToString());
        return UnpackageStations.TryGetValue(guidKey, out value);
        #endif
    }
#if !MONO
    public bool TryGetValue(Guid guid, out bool value) => UnpackageStations.TryGetValue(guid, out value);
#endif
}