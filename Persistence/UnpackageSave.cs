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
}