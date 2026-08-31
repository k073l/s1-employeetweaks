using System.Reflection;
using EmployeeTweaks.Helpers;
using HarmonyLib;
using MelonLoader;
using MelonLoader.Preferences;
using UnityEngine;
using Logger = EmployeeTweaks.Helpers.Logger;
#if MONO
using ScheduleOne.Property;
#else
using Il2CppScheduleOne.Property;
#endif

namespace EmployeeTweaks.Patches.EmployeeArea;

[HarmonyPatch]
internal class PropertyPatch
{
    private static Logger Logger = new("PropertyPatch");
    internal static Dictionary<Property, (Vector3, Vector3)> _propertyIdlePointRects = new();

    // network init early, bc Awake wasn't working on some properties of Il2Cpp bc why would it
    [HarmonyPatch(typeof(Property), nameof(Property.NetworkInitialize___Early))]
    [HarmonyPriority(Priority.Last - 100)]
    [HarmonyPrefix]
    private static void StorePointRectAndAddCapacity(Property __instance)
    {
        if (!Melon<EmployeeTweaks>.Instance.SettingsRegistry.EnableCapacityAndDebug.Value) return;
        var idlePoints = __instance.EmployeeIdlePoints;
        if (idlePoints is { Length: > 0 })
        {
            var (min, max) = MinMaxPoints(idlePoints.AsEnumerable().ToList());
            if (min == max) max = min + new Vector3(0.5f, 0f, 0.5f);
            min -= new Vector3(1f, 0f, 1f);
            max += new Vector3(1f, 0f, 1f);
            _propertyIdlePointRects[__instance] = (min, max);
        }

        if (__instance.EmployeeCapacity <= 0) return;
        var entry = Melon<EmployeeTweaks>.Instance.SettingsRegistry.EmployeeCapacityCategory.GetOrCreateNetworkedEntry(
            $"EmployeeTweaks_{__instance.propertyCode}_EmpCap", __instance.EmployeeCapacity,
            Melon<EmployeeTweaks>.Instance.SettingsRegistry._boxedClient,
            Melon<EmployeeTweaks>.Instance.SettingsRegistry._boxedOptions, true,
            $"{__instance.propertyName} Employee Capacity",
            "Max amount of employees you can hire for this property",
            validator: new ValueRange<int>(1, Mathf.CeilToInt(__instance.EmployeeCapacity * 1.5f) + 2));
        Melon<EmployeeTweaks>.Instance.SettingsRegistry.EmployeeCapacities.Add(entry);
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
                rect.Item1, rect.Item2, (prop.EmployeeIdlePoints?.AsEnumerable() ?? []).ToList(), diff, 1f, 0.1f,
                PoissonDiskSampler2D.DeterministicHash(prop.propertyCode), minDistance: 0.6f);
            if (newPoints.Count < diff)
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

    // Catch late updates to idle points - compatibility with other mods
    private static void CatchLate(Property __instance)
    {
        Logger.Debug($"Catching late update for {__instance?.name}");
        if (__instance == null) return;
        if (_propertyIdlePointRects.ContainsKey(__instance)) return;
        StorePointRectAndAddCapacity(__instance);
    }

    public static void ManualPatchProperties(HarmonyLib.Harmony harmony)
    {
        // Bungalow also seems to patch RV and MotelRoom? Alright
        List<Type> types = [typeof(Bungalow), typeof(Manor), typeof(SewerOffice), typeof(Business), typeof(Property)];
        foreach (var type in types)
        {
            var method = type.GetMethod(
                "Awake",
                BindingFlags.Instance |
                BindingFlags.Public |
                BindingFlags.NonPublic |
                BindingFlags.DeclaredOnly);
            if (method == null)
            {
                Logger.Warning($"Could not find Awake method for {type.FullName}");
                continue;
            }
            Logger.Debug($"Patching {type.FullName}");

            harmony.Patch(method,
                postfix: new HarmonyMethod(typeof(PropertyPatch).GetMethod(nameof(CatchLate),
                    System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Static)));
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
    private const uint FnvOffsetBias = 2166136261;
    private const uint FnvPrime = 16777619;

    private static System.Random _rng;

    public static int DeterministicHash(string s)
    {
        var hash = FnvOffsetBias;
        foreach (var c in s)
        {
            hash ^= (byte)c;
            hash *= FnvPrime;
        }
        return unchecked((int)hash);
    }

    public static List<Vector3> SampleAdaptive(
        Vector3 min,
        Vector3 max,
        List<Transform> blocked,
        int targetCount,
        float preferredRadius,
        float minRadius,
        int seed,
        float minDistance = 0.5f,
        int maxIterations = 5,
        int k = 30)
    {
        _rng = new System.Random(seed);

        var centroid = ComputeCentroid(blocked);
        var minDistSqr = minDistance * minDistance;

        // min distance + line-of-sight
        var result = SampleWithConstraints(min, max, blocked, targetCount, preferredRadius, minRadius,
            centroid, minDistSqr, requireLos: true, maxIterations: maxIterations, k: k);
        if (result.Count >= targetCount)
            return result.GetRange(0, targetCount);

        // min-distance, halve it and retry
        var relaxedDist = minDistance * 0.5f;
        var relaxedDistSqr = relaxedDist * relaxedDist;
        result = SampleWithConstraints(min, max, blocked, targetCount, preferredRadius, minRadius,
            centroid, relaxedDistSqr, requireLos: true, maxIterations: maxIterations, k: k);
        if (result.Count >= targetCount)
            return result.GetRange(0, targetCount);

        // try LOS first per candidate, accept no-LOS as fallback
        result = SampleWithConstraints(min, max, blocked, targetCount, preferredRadius, minRadius,
            centroid, minDistSqr, requireLos: false, maxIterations: maxIterations, k: k);
        if (result.Count >= targetCount)
            return result.GetRange(0, targetCount);

        // min distance only, no LOS
        var radius = preferredRadius;
        List<Vector3> best = [];
        for (var i = 0; i < maxIterations; i++)
        {
            var sample = Sample(min, max, blocked, radius, minDistSqr, centroid, requireLos: false, k);
            if (sample.Count >= targetCount)
            {
                best = sample;
                break;
            }
            if (sample.Count > best.Count)
                best = sample;
            radius *= 0.7f;
            if (radius < minRadius) break;
        }

        if (best.Count < targetCount)
            best.AddRange(Fill(min, max, blocked, best, targetCount - best.Count, minDistSqr));

        return best.GetRange(0, Mathf.Min(targetCount, best.Count));
    }

    private static List<Vector3> SampleWithConstraints(
        Vector3 min,
        Vector3 max,
        List<Transform> blocked,
        int targetCount,
        float preferredRadius,
        float minRadius,
        Vector3 centroid,
        float minDistSqr,
        bool requireLos,
        int maxIterations,
        int k)
    {
        var radius = preferredRadius;
        List<Vector3> best = [];

        for (var i = 0; i < maxIterations; i++)
        {
            var result = Sample(min, max, blocked, radius, minDistSqr, centroid, requireLos, k);

            if (result.Count >= targetCount)
                return result.GetRange(0, targetCount);

            if (result.Count > best.Count)
                best = result;

            radius *= 0.7f;
            if (radius < minRadius) break;
        }

        return best;
    }

    private static List<Vector3> Sample(
        Vector3 min,
        Vector3 max,
        List<Transform> blocked,
        float radius,
        float minDistSqr,
        Vector3 centroid,
        bool requireLos,
        int k)
    {
        var cellSize = radius / Mathf.Sqrt(2f);

        var width = Mathf.Max(1, Mathf.CeilToInt((max.x - min.x) / cellSize));
        var height = Mathf.Max(1, Mathf.CeilToInt((max.z - min.z) / cellSize));

        var grid = new Vector3?[width, height];
        var active = new List<Vector3>();
        var result = new List<Vector3>();

        var first = RandomPoint(min, max);
        if (IsAcceptable(first, blocked, null, null, minDistSqr, centroid, requireLos))
        {
            active.Add(first);
            result.Add(first);
            Set(grid, min, cellSize, first);
        }
        else if (requireLos)
        {
            for (var i = 0; i < k; i++)
            {
                var candidate = GenerateAround(centroid, radius);
                candidate.x = Mathf.Clamp(candidate.x, min.x, max.x);
                candidate.z = Mathf.Clamp(candidate.z, min.z, max.z);
                if (TooClose(candidate, blocked, null, null, minDistSqr))
                    continue;
                if (!HasLineOfSight(candidate, centroid, blocked))
                    continue;
                active.Add(candidate);
                result.Add(candidate);
                Set(grid, min, cellSize, candidate);
                break;
            }
        }

        var maxIterations = width * height * 2;
        var iterations = 0;
        while (active.Count > 0 && iterations++ < maxIterations)
        {
            var index = _rng.Next(active.Count);
            var p = active[index];

            var found = false;

            if (requireLos)
            {
                for (var i = 0; i < k; i++)
                {
                    var candidate = GenerateAround(p, radius);

                    if (TooClose(candidate, blocked, null, null, minDistSqr) ||
                        TooCloseAny(candidate, result, minDistSqr))
                        continue;

                    if (!HasLineOfSight(candidate, centroid, blocked))
                        continue;

                    if (!InGrid(candidate, min, max, radius, grid, cellSize))
                        continue;

                    active.Add(candidate);
                    result.Add(candidate);
                    Set(grid, min, cellSize, candidate);
                    found = true;
                    break;
                }

                if (!found)
                {
                    for (var i = 0; i < k; i++)
                    {
                        var candidate = GenerateAround(centroid, radius);
                        candidate.x = Mathf.Clamp(candidate.x, min.x, max.x);
                        candidate.z = Mathf.Clamp(candidate.z, min.z, max.z);

                        if (TooClose(candidate, blocked, null, null, minDistSqr) ||
                            TooCloseAny(candidate, result, minDistSqr))
                            continue;

                        if (!HasLineOfSight(candidate, centroid, blocked))
                            continue;

                        if (!InGrid(candidate, min, max, radius, grid, cellSize))
                            continue;

                        active.Add(candidate);
                        result.Add(candidate);
                        Set(grid, min, cellSize, candidate);
                        found = true;
                        break;
                    }
                }
            }
            else
            {
                Vector3? bestCandidate = null;
                var bestHadLos = false;

                for (var i = 0; i < k; i++)
                {
                    var candidate = GenerateAround(p, radius);

                    if (TooClose(candidate, blocked, null, null, minDistSqr) ||
                        TooCloseAny(candidate, result, minDistSqr))
                        continue;

                    if (!InGrid(candidate, min, max, radius, grid, cellSize))
                        continue;

                    var hasLos = HasLineOfSight(candidate, centroid, blocked);

                    if (!bestCandidate.HasValue || (hasLos && !bestHadLos))
                    {
                        bestCandidate = candidate;
                        bestHadLos = hasLos;
                    }

                    if (hasLos)
                        break;
                }

                if (bestCandidate.HasValue)
                {
                    active.Add(bestCandidate.Value);
                    result.Add(bestCandidate.Value);
                    Set(grid, min, cellSize, bestCandidate.Value);
                    found = true;
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
        float minDistSqr)
    {
        var result = new List<Vector3>();

        var attempts = 0;
        var maxAttempts = needed * 20;

        while (result.Count < needed && attempts++ < maxAttempts)
        {
            var p = RandomPoint(min, max);

            if (TooClose(p, blocked, current, result, minDistSqr))
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

    private static bool IsAcceptable(
        Vector3 p,
        List<Transform> blocked,
        List<Vector3>? a,
        List<Vector3>? b,
        float minDistSqr,
        Vector3 centroid,
        bool requireLos)
    {
        if (TooClose(p, blocked, a, b, minDistSqr))
            return false;

        if (requireLos && !HasLineOfSight(p, centroid, blocked))
            return false;

        return true;
    }

    private static bool InGrid(
        Vector3 p,
        Vector3 min,
        Vector3 max,
        float radius,
        Vector3?[,] grid,
        float cellSize)
    {
        if (p.x < min.x || p.x > max.x || p.z < min.z || p.z > max.z)
            return false;

        var r2 = radius * radius;
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
                (grid[nx, nz]!.Value - p).sqrMagnitude < r2)
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
        List<Vector3>? a,
        List<Vector3>? b,
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

    private static bool TooCloseAny(Vector3 p, List<Vector3> points, float minDistSqr)
    {
        foreach (var v in points)
            if ((v - p).sqrMagnitude < minDistSqr)
                return true;
        return false;
    }

    private static float Lerp(float a, float b) =>
        a + (float)_rng.NextDouble() * (b - a);

    private static Vector3 ComputeCentroid(List<Transform> points)
    {
        if (points.Count == 0) return Vector3.zero;
        var sum = Vector3.zero;
        foreach (var t in points)
            sum += t.position;
        return sum / points.Count;
    }

    private static Vector3 FindNearestPoint(Vector3 p, List<Transform> points)
    {
        var best = points[0].position;
        var bestDist = (best - p).sqrMagnitude;
        for (var i = 1; i < points.Count; i++)
        {
            var pos = points[i].position;
            var dist = (pos - p).sqrMagnitude;
            if (dist < bestDist)
            {
                best = pos;
                bestDist = dist;
            }
        }
        return best;
    }

    private static bool HasLineOfSight(Vector3 from, Vector3 to)
    {
        var elevated = new Vector3(0f, 1f, 0f);
        return !Physics.Linecast(from + elevated, to + elevated);
    }

    private static bool HasLineOfSight(Vector3 candidate, Vector3 centroid, List<Transform> blocked)
    {
        if (HasLineOfSight(centroid, candidate))
            return true;
        if (blocked.Count > 1)
        {
            var nearest = FindNearestPoint(candidate, blocked);
            if (nearest != centroid && HasLineOfSight(nearest, candidate))
                return true;
        }
        return false;
    }
}