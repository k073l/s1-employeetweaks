using EmployeeTweaks.Helpers;
using HarmonyLib;
using MelonLoader;
using MelonLoader.Preferences;
using UnityEngine;
#if MONO
using ScheduleOne.Property;
#else
using Il2CppScheduleOne.Property;
#endif

namespace EmployeeTweaks.Patches.EmployeeArea;

[HarmonyPatch(typeof(Property))]
internal class PropertyPatch
{
    private static MelonLogger.Instance Logger = new("EmployeeTweaks.PropertyPatch");
    internal static Dictionary<Property, (Vector3, Vector3)> _propertyIdlePointRects = new();

    // network init early, bc Awake wasn't working on some properties of Il2Cpp bc why would it
    [HarmonyPatch(nameof(Property.NetworkInitialize___Early))]
    [HarmonyPriority(Priority.Last - 100)]
    [HarmonyPrefix]
    private static void StorePointRectAndAddCapacity(Property __instance)
    {
        if (!EmployeeTweaks.EnableCapacityAndDebug.Value) return;
        var idlePoints = __instance.EmployeeIdlePoints;
        if (idlePoints is { Length: > 0 })
        {
            var (min, max) = MinMaxPoints(idlePoints.AsEnumerable().ToList());
            if (min == max) max = min + new Vector3(0.5f, 0f, 0.5f);
            _propertyIdlePointRects[__instance] = (min, max);
        }

        if (__instance.EmployeeCapacity <= 0) return;
        var entry = EmployeeTweaks.EmployeeCapacityCategory.GetOrCreateEntry(
            $"EmployeeTweaks_{__instance.propertyCode}_EmpCap", __instance.EmployeeCapacity,
            $"{__instance.propertyName} Employee Capacity",
            "Max amount of employees you can hire for this property", validator: new ValueRange<int>(1, Mathf.CeilToInt(__instance.EmployeeCapacity * 1.5f) + 1));
        EmployeeTweaks.EmployeeCapacities.Add(entry);
        entry.OnEntryValueChanged.Subscribe((oldVal, newVal) =>
        {
            if (oldVal == newVal) return;
            AddCapacity(__instance, newVal);
        });
        AddCapacity(__instance, entry.Value);
        return;

        void AddCapacity(Property prop, int target)
        {
            var currentEmployees = prop.Employees?.AsEnumerable().Count() ?? 0;
            if (target < currentEmployees)
            {
                Logger.Warning(
                    $"Cannot set capacity of {prop.propertyName} to {target} because it currently has {currentEmployees} employees");
                entry.Value = prop.EmployeeCapacity;
                return;
            }

            if (!_propertyIdlePointRects.TryGetValue(prop, out var rect))
            {
                Logger.Warning($"Could not find idle point rect for {prop.propertyName}, cannot add capacity");
                entry.Value = prop.EmployeeCapacity;
                return;
            }

            var current = prop.EmployeeIdlePoints?.Length ?? 0;
            var diff = target - current;
            if (diff <= 0)
            {
                // guarded earlier from setting less than current employees, so we can just truncate
                if (current <= 0)
                {
                    // nothing we can do
                    entry.Value = prop.EmployeeCapacity;
                    return;
                }

                var idlePointsList = prop.EmployeeIdlePoints.AsEnumerable().ToList();
                idlePointsList = idlePointsList.GetRange(0, target);
                prop.EmployeeIdlePoints = idlePointsList.ToArray();
                prop.EmployeeCapacity = idlePointsList.Count;
                return;
            }

            var newPoints = PoissonDiskSampler2D.SampleAdaptive(
                rect.Item1, rect.Item2, (prop.EmployeeIdlePoints?.AsEnumerable() ?? []).ToList(), diff, 1f, 0.01f,
                prop.propertyCode.GetHashCode());
            if (newPoints.Count + (prop.EmployeeIdlePoints?.Length ?? 0) < target)
            {
                Logger.Warning(
                    $"Generated {newPoints.Count} new points for {prop.propertyName} but needed {diff}, cannot add capacity");
                entry.Value = prop.EmployeeCapacity;
                return;
            }

            var newTransforms = new List<Transform>();
            var point = prop.EmployeeIdlePoints?.FirstOrDefault();
            if (point == null)
            {
                Logger.Warning($"Property {prop.propertyName} has no idle points, cannot add capacity");
                entry.Value = prop.EmployeeCapacity;
                return;
            }

            foreach (var newPoint in newPoints)
            {
                var go = new GameObject($"{prop.propertyName}_EmployeeIdlePoint");
                go.transform.SetParent(point.transform.parent, false);
                go.transform.position = newPoint;
                go.transform.rotation = point.transform.rotation;
                newTransforms.Add(go.transform);
            }

            foreach (var oldTransform in prop.EmployeeIdlePoints)
                newTransforms.Add(oldTransform);
            prop.EmployeeIdlePoints = newTransforms.ToArray();
            prop.EmployeeCapacity = newTransforms.Count;
        }
    }

    private static (Vector3 min, Vector3 max) MinMaxPoints(List<Transform> transforms)
    {
        if (transforms == null || transforms.Count == 0)
            throw new ArgumentException("Empty list");

        var first = transforms[0].position;
        var min = first;
        var max = first;

        for (var i = 1; i < transforms.Count; i++)
        {
            var pos = transforms[i].position;
            min = Vector3.Min(min, pos);
            max = Vector3.Max(max, pos);
        }

        return (min, max);
    }
}

public static class PoissonDiskSampler2D
{
    private static System.Random _rng;

    public static List<Vector3> SampleAdaptive(
        Vector3 min,
        Vector3 max,
        List<Transform> blocked,
        int targetCount,
        float preferredRadius,
        float minRadius,
        int seed,
        int maxIterations = 5,
        int k = 30)
    {
        _rng = new System.Random(seed);

        var radius = preferredRadius;
        List<Vector3> best = [];

        for (var i = 0; i < maxIterations; i++)
        {
            var result = Sample(min, max, blocked, radius, k);

            if (result.Count >= targetCount)
                return result.GetRange(0, targetCount);

            if (result.Count > best.Count)
                best = result;

            radius *= 0.7f;

            if (radius < minRadius)
                break;
        }

        if (best.Count < targetCount)
            best.AddRange(Fill(min, max, blocked, best, targetCount - best.Count, minRadius));

        return best;
    }

    private static List<Vector3> Sample(
        Vector3 min,
        Vector3 max,
        List<Transform> blocked,
        float radius,
        int k)
    {
        var cellSize = radius / Mathf.Sqrt(2f);

        var width = Mathf.CeilToInt((max.x - min.x) / cellSize);
        var height = Mathf.CeilToInt((max.z - min.z) / cellSize);

        var grid = new Vector3?[width, height];
        var active = new List<Vector3>();
        var result = new List<Vector3>();

        var first = RandomPoint(min, max);
        active.Add(first);
        result.Add(first);
        Set(grid, min, cellSize, first);

        while (active.Count > 0)
        {
            var index = _rng.Next(active.Count);
            var p = active[index];

            var found = false;

            for (var i = 0; i < k; i++)
            {
                var candidate = GenerateAround(p, radius);

                if (IsValid(candidate, min, max, radius, grid, blocked, cellSize))
                {
                    active.Add(candidate);
                    result.Add(candidate);
                    Set(grid, min, cellSize, candidate);
                    found = true;
                    break;
                }
            }

            if (!found)
                active.RemoveAt(index);
        }

        return result;
    }

    private static List<Vector3> Fill(
        Vector3 min,
        Vector3 max,
        List<Transform> blocked,
        List<Vector3> current,
        int needed,
        float epsilon)
    {
        var result = new List<Vector3>();
        var epsSqr = epsilon * epsilon;

        var attempts = 0;
        var maxAttempts = needed * 20;

        while (result.Count < needed && attempts++ < maxAttempts)
        {
            var p = RandomPoint(min, max);

            if (TooClose(p, blocked, current, result, epsSqr))
                continue;

            result.Add(p);
        }

        return result;
    }

    private static Vector3 RandomPoint(Vector3 min, Vector3 max) =>
        new(
            Lerp(min.x, max.x),
            min.y,
            Lerp(min.z, max.z)
        );

    private static Vector3 GenerateAround(Vector3 p, float radius)
    {
        var angle = (float)(_rng.NextDouble() * Mathf.PI * 2f);
        var dist = Lerp(radius, radius * 2f);

        return new Vector3(
            p.x + Mathf.Cos(angle) * dist,
            p.y,
            p.z + Mathf.Sin(angle) * dist
        );
    }

    private static bool IsValid(
        Vector3 p,
        Vector3 min,
        Vector3 max,
        float radius,
        Vector3?[,] grid,
        List<Transform> blocked,
        float cellSize)
    {
        if (p.x < min.x || p.x > max.x || p.z < min.z || p.z > max.z)
            return false;

        var r2 = radius * radius;

        if (TooClose(p, blocked, null, null, r2))
            return false;

        var gx = (int)((p.x - min.x) / cellSize);
        var gz = (int)((p.z - min.z) / cellSize);

        for (var x = -2; x <= 2; x++)
        for (var z = -2; z <= 2; z++)
        {
            var nx = gx + x;
            var nz = gz + z;

            if (nx < 0 || nz < 0 || nx >= grid.GetLength(0) || nz >= grid.GetLength(1))
                continue;

            if (grid[nx, nz].HasValue &&
                (grid[nx, nz].Value - p).sqrMagnitude < r2)
                return false;
        }

        return true;
    }

    private static void Set(Vector3?[,] grid, Vector3 min, float cellSize, Vector3 p)
    {
        var x = (int)((p.x - min.x) / cellSize);
        var z = (int)((p.z - min.z) / cellSize);
        grid[x, z] = p;
    }

    private static bool TooClose(
        Vector3 p,
        List<Transform> blocked,
        List<Vector3> a,
        List<Vector3> b,
        float epsSqr)
    {
        foreach (var t in blocked)
            if ((t.position - p).sqrMagnitude < epsSqr)
                return true;

        if (a != null)
            foreach (var v in a)
                if ((v - p).sqrMagnitude < epsSqr)
                    return true;

        if (b != null)
            foreach (var v in b)
                if ((v - p).sqrMagnitude < epsSqr)
                    return true;

        return false;
    }

    private static float Lerp(float a, float b) =>
        a + (float)_rng.NextDouble() * (b - a);
}