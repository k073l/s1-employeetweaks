using HarmonyLib;
using MelonLoader;
using S1API.Internal.Abstraction;
using ScheduleOne.ItemFramework;
using UnityEngine.UI;
using Object = UnityEngine.Object;
using ScheduleOne.UI.Items;
using TMPro;
using UnityEngine;

namespace EmployeeTweaks.Patches.FilterItemApply;

[HarmonyPatch(typeof(FilterConfigPanel))]
internal class FilterConfigPanelPatches
{
    private static MelonLogger.Instance _logger = new("EmployeeTweaks.FilterItemApply");

    internal const string Filter1 = "Apply Item As Filter";
    internal const string Filter2 = "Apply All As Filters";
    internal static bool AllSlots;
    internal static bool DenyListMode;

    internal static TextMeshProUGUI ApplyItemAsFilterButtonText;

    private static Button _applyItemAsFilterButton;

    [HarmonyPatch(nameof(FilterConfigPanel.Awake))]
    [HarmonyPostfix]
    private static void AddButton(FilterConfigPanel __instance)
    {
        var applyToSiblings = __instance.ApplyToSiblingsButton;
        if (applyToSiblings == null) return;
        // instantiate to copy the button
        var buttonGo = Object.Instantiate(applyToSiblings.gameObject, applyToSiblings.transform.parent);
        _applyItemAsFilterButton = buttonGo.GetComponent<Button>();
        ApplyItemAsFilterButtonText = _applyItemAsFilterButton.GetComponentInChildren<TextMeshProUGUI>();
        ApplyItemAsFilterButtonText.text = Filter1;
        _applyItemAsFilterButton.onClick.RemoveAllListeners();
        var s = "";
        EventHelper.AddListener(() =>
        {
            s = s;
            if (AllSlots)
                ApplyItemsAsFilters(__instance.OpenSlot);
            else
                ApplyItemAsFilter(__instance.OpenSlot);
        }, _applyItemAsFilterButton.onClick);
    }

    private static void ApplyItemAsFilter(ItemSlot itemSlot)
    {
        if (!AllSlots)
            _logger.Msg("Applying item as filter");
        var item = itemSlot.ItemInstance;
        if (item?.ID == null)
        {
            _logger.Warning("No item in slot to apply as filter.");
            return;
        }

        var playerFilter = itemSlot.PlayerFilter;
        if (playerFilter?.ItemIDs == null)
        {
            _logger.Warning("Player filter is null or does not have an ItemIDs list.");
            return;
        }

        playerFilter.ItemIDs.Clear();
        playerFilter.ItemIDs.Add(item.ID);
        playerFilter.Type = DenyListMode ? SlotFilter.EType.Blacklist : SlotFilter.EType.Whitelist;
        itemSlot.SetPlayerFilter(playerFilter);
    }

    private static void ApplyItemsAsFilters(ItemSlot itemSlot)
    {
        _logger.Msg("Applying all items in sibling slots as filters.");
        foreach (var slot in itemSlot.SiblingSet.Slots)
            ApplyItemAsFilter(slot);
    }
}