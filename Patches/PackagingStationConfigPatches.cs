using EmployeeTweaks.Persistence;
using HarmonyLib;
using ScheduleOne;
using ScheduleOne.Management;
using ScheduleOne.ObjectScripts;
using ScheduleOne.UI.Management;
using TMPro;
using UnityEngine;

namespace EmployeeTweaks.Patches;

public static class PackagingStationConfigPatches
{
    public static Dictionary<System.Guid, StringField> UnpackageFields = new();

    public static void InitializeField(PackagingStationConfiguration config, PackagingStation station)
    {
        var guid = station.GUID;

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
    [HarmonyPatch("BindInternal")]
    public static void BindInternalPostfix(PackagingStationConfigPanel __instance, List<EntityConfiguration> configs)
    {
        if (__instance == null || __instance.DestinationUI == null)
            return;

        try
        {
            var config = configs.OfType<PackagingStationConfiguration>().FirstOrDefault();

            if (config?.Station == null)
                return;

            var stationGuid = config.Station.GUID;

            if (!PackagingStationConfigPatches.UnpackageFields.TryGetValue(stationGuid, out _))
                return;

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
            UnpackageSave.Instance.UnpackageStations.TryGetValue(stationGuid, out var isUnpackage);
            valueTmpro.text = isUnpackage ? "On" : "Off";
            valueTmpro.color = isUnpackage ? Color.green : Color.gray;

            var button = valueText.AddComponent<UnityEngine.UI.Button>();

            button.onClick.RemoveAllListeners();
            button.onClick.AddListener(() =>
            {
                MelonLoader.MelonLogger.Msg("Button clicked");
                var save = UnpackageSave.Instance;
                if (save?.UnpackageStations == null) return;
                if (!save.UnpackageStations.TryAdd(stationGuid, true))
                    save.UnpackageStations[stationGuid] = !save.UnpackageStations[stationGuid];
                var newValue = save.UnpackageStations[stationGuid];
                valueTmpro.text = newValue ? "On" : "Off";
                valueTmpro.color = newValue ? Color.green : Color.gray;
            });

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