using EmployeeTweaks.Patches.EmployeeArea;
using MelonLoader;
using UnityEngine;
using Object = UnityEngine.Object;

namespace EmployeeTweaks;

internal class DebugAreaDrawer
{
    private MelonLogger.Instance Logger = new("EmployeeTweaks.DebugAreaDrawer");
    private List<GameObject> _areas = [];

    internal void Draw()
    {
        foreach (var area in _areas)
            if (area != null) Object.DestroyImmediate(area);
        _areas.Clear();
        if (!EmployeeTweaks.EnableCapacityAndDebug.Value) return;
        if (!EmployeeTweaks.DrawDebugArea.Value) return;
        var nudge = new Vector3(0f, 0.1f, 0f);
        foreach (var area in PropertyPatch._propertyIdlePointRects.Values)
        {
            Logger.Msg($"Drawing Employee Area at {area.Item1 + nudge}, {area.Item2 + nudge}");
            var go = DrawDebugArea(area.Item1 + nudge, area.Item2 + nudge, new Color(1f, 0f, 0f, 0.2f));
            _areas.Add(go);
        }
    }
    
    private static GameObject DrawDebugArea(Vector3 a, Vector3 b, Color color)
    {
        var square = GameObject.CreatePrimitive(PrimitiveType.Quad);
        square.name = "DebugArea";

        Object.Destroy(square.GetComponent<Collider>());

        var min = Vector3.Min(a, b);
        var max = Vector3.Max(a, b);

        var center = (min + max) * 0.5f;
        var size = max - min;

        square.transform.position = center + (Vector3.up * 0.01f);
        square.transform.rotation = Quaternion.Euler(90f, 0f, 0f);

        square.transform.localScale = new Vector3(size.x, size.z, 1f);

        var renderer = square.GetComponent<MeshRenderer>();

        var shader = Shader.Find("Universal Render Pipeline/Lit");
        if (shader == null)
            return square;

        var mat = new Material(shader);

        if (mat.HasProperty("_Surface"))
            mat.SetFloat("_Surface", 1f);

        if (color.a <= 0f)
            color.a = 0.2f;

        if (mat.HasProperty("_BaseColor"))
            mat.SetColor("_BaseColor", color);

        if (mat.HasProperty("_EmissionColor"))
        {
            mat.EnableKeyword("_EMISSION");
            mat.SetColor("_EmissionColor", new Color(color.r, color.g, color.b) * 1.5f);
        }

        mat.SetInt("_ZWrite", 0);
        mat.renderQueue = 3000;

        renderer.material = mat;

        return square;
    }
}