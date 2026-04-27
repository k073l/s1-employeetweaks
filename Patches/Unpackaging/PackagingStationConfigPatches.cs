#if MONO
using ScheduleOne;
using ScheduleOne.Management;
using ScheduleOne.ObjectScripts;
using ScheduleOne.UI.Management;
using TMPro;
#else
using Il2CppScheduleOne;
using Il2CppScheduleOne.Management;
using Il2CppScheduleOne.ObjectScripts;
using Il2CppScheduleOne.UI.Management;
using Il2CppTMPro;
#endif
using EmployeeTweaks.Helpers;
using EmployeeTweaks.Persistence;
using HarmonyLib;
using MelonLoader;
using S1API.Internal.Abstraction;
using UnityEngine;

namespace EmployeeTweaks.Patches.Unpackaging;

public static class PackagingStationConfigPatches
{
    public static Dictionary<System.Guid, StringField> UnpackageFields = new();

    public static void InitializeField(PackagingStationConfiguration config, PackagingStation station)
    {
        var guid = Guid.Parse(station.GUID.ToString());

        if (!UnpackageFields.ContainsKey(guid))
        {
            var field = new StringField(config, "false");
            UnpackageFields[guid] = field;
        }
        else
        {
            config.Fields.Add(UnpackageFields[guid]);
        }
    }
}

[HarmonyPatch(typeof(PackagingStationConfiguration))]
public class PackagingStationConfigurationPatch
{
    [HarmonyPostfix]
    [HarmonyPatch(MethodType.Constructor,
        new[] { typeof(ConfigurationReplicator), typeof(IConfigurable), typeof(PackagingStation) })]
    public static void ConstructorPostfix(PackagingStationConfiguration __instance, PackagingStation station)
    {
        if (__instance == null || station == null)
            return;

        PackagingStationConfigPatches.InitializeField(__instance, station);
    }
}

[HarmonyPatch(typeof(PackagingStationConfigPanel))]
public class PackagingStationConfigPanelPatch
{
    [HarmonyPostfix]
    [HarmonyPatch(nameof(PackagingStationConfigPanel.BindInternal))]
    public static void BindInternalPostfix(PackagingStationConfigPanel __instance, 
#if MONO
        List<EntityConfiguration> configs)
#else
        Il2CppSystem.Collections.Generic.List<EntityConfiguration> configs)
#endif
    {
        if (__instance == null || __instance.DestinationUI == null)
            return;

        try
        {
            PackagingStationConfiguration config = null;
            foreach (var conf in configs)
            {
                if (!Utils.Is<PackagingStationConfiguration>(conf, out var result) || result == null) continue;
                config = result;
                break;
            }

            if (config?.Station == null)
                return;

            var stationGuid = Guid.Parse(config.Station.GUID.ToString());

            if (!PackagingStationConfigPatches.UnpackageFields.TryGetValue(stationGuid, out _))
            {
                // if not, request
                PackagingStationConfigPatches.InitializeField(config, config.Station);
                // return;
            }

            var parent = __instance.DestinationUI.transform.parent;

            var toggleObj = new GameObject("UnpackageToggle");

            toggleObj.transform.SetParent(parent, false);

            var rect = toggleObj.AddComponent<RectTransform>();

            var destUI = __instance.DestinationUI.GetComponent<RectTransform>();
            rect.sizeDelta = new Vector2(400f, 50f);

            var titleText = new GameObject("Title");
            titleText.transform.SetParent(toggleObj.transform, false);

            var titleRect = titleText.AddComponent<RectTransform>();
            titleRect.anchorMin = new Vector2(0f, 0.5f);
            titleRect.anchorMax = new Vector2(0f, 0.5f);
            titleRect.pivot = new Vector2(0f, 0.5f);
            titleRect.anchoredPosition = new Vector2(0f, 0f);
            titleRect.sizeDelta = new Vector2(150f, 30f);

            var tmpro = titleText.AddComponent<TextMeshProUGUI>();
            tmpro.fontSize = 24f;
            tmpro.alignment = TextAlignmentOptions.Left;
            tmpro.text = "Set Unpackage";
            tmpro.color = Color.black;

            var valueText = new GameObject("Value");
            valueText.transform.SetParent(toggleObj.transform, false);

            var valueRect = valueText.AddComponent<RectTransform>();
            valueRect.anchorMin = new Vector2(1f, 0.5f);
            valueRect.anchorMax = new Vector2(1f, 0.5f);
            valueRect.pivot = new Vector2(1f, 0.5f);
            valueRect.anchoredPosition = new Vector2(0f, 0f);
            valueRect.sizeDelta = new Vector2(80f, 30f);

            var valueTmpro = valueText.AddComponent<TextMeshProUGUI>();
            valueTmpro.fontSize = 24f;
            valueTmpro.alignment = TextAlignmentOptions.Right;
            UnpackageSave.Instance.TryGetValue(stationGuid, out var isUnpackage);
            valueTmpro.text = isUnpackage ? "On" : "Off";
            valueTmpro.color = isUnpackage ? Color.green : Color.gray;

            var button = valueText.AddComponent<UnityEngine.UI.Button>();

            button.onClick.RemoveAllListeners();
            var s = "";
            EventHelper.AddListener(() =>
            {
                s = s;
                MelonLogger.Msg("Button clicked");
                var save = UnpackageSave.Instance;
                if (save?.UnpackageStations == null) return;
                if (!save.UnpackageStations.TryAdd(stationGuid, true))
                    save.UnpackageStations[stationGuid] = !save.UnpackageStations[stationGuid];
                var newValue = save.UnpackageStations[stationGuid];
                valueTmpro.text = newValue ? "On" : "Off";
                valueTmpro.color = newValue ? Color.green : Color.gray;
            }, button.onClick);

            var contentPanel = __instance.GetComponent<UIContentPanel>();

            if (contentPanel != null)
            {
                var selectable = valueText.GetComponent<UISelectable>();
                if (selectable == null)
                    selectable = valueText.AddComponent<UISelectable>();

                contentPanel.AddSelectable(selectable);
            }
        }
        catch (System.Exception ex)
        {
            MelonLoader.MelonLogger.Error($"BindInternalPostfix failed: {ex}");
        }
    }
}