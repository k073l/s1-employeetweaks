using EmployeeTweaks.Helpers;
using MelonLoader;
using S1API.UI;
using UnityEngine;
using UnityEngine.UI;
using Logger = EmployeeTweaks.Helpers.Logger;
using Object = UnityEngine.Object;

namespace EmployeeTweaks.Patches.MoreEmployeeConfigItems;

public static class ClipboardUIHelper
{
    internal static readonly Logger Logger = new(nameof(MoreEmployeeConfigItems));

    public static ScrollRect MoveToScrollableList(RectTransform source, Transform parent)
    {
        if (source == null)
        {
            Logger.Error("Source is null");
            return null;
        }

        var oldVLG = source.GetComponent<VerticalLayoutGroup>();
        if (oldVLG == null)
        {
            Logger.Error("Source does not have VerticalLayoutGroup");
            return null;
        }

        var content = UIFactory.ScrollableVerticalList(source.name + "_Scroll", parent, out var scrollRect);

        var newVLG = content.GetComponent<VerticalLayoutGroup>();

        newVLG.spacing = oldVLG.spacing;
        newVLG.padding = new RectOffset(
            oldVLG.padding.left,
            oldVLG.padding.right,
            8,
            oldVLG.padding.bottom
        );


        newVLG.childAlignment = oldVLG.childAlignment;

        newVLG.childControlWidth = true;
        newVLG.childControlHeight = oldVLG.childControlHeight;

        newVLG.childForceExpandWidth = true;
        newVLG.childForceExpandHeight = oldVLG.childForceExpandHeight;

        newVLG.childScaleWidth = oldVLG.childScaleWidth;
        newVLG.childScaleHeight = oldVLG.childScaleHeight;

        while (source.childCount > 0)
        {
            var child = source.GetChild(0);
            child.SetParent(content, false);
            
            if (!Utils.Is<RectTransform>(child, out var rt) || rt == null) continue;

            // Fix vertical anchoring
            rt.anchorMin = new Vector2(rt.anchorMin.x, 1);
            rt.anchorMax = new Vector2(rt.anchorMax.x, 1);
            rt.pivot = new Vector2(rt.pivot.x, 1);
        }

        var scrollRT = scrollRect.gameObject.GetComponent<RectTransform>();

        scrollRT.anchorMin = new Vector2(0, 1);
        scrollRT.anchorMax = new Vector2(1, 1);
        scrollRT.pivot = new Vector2(0.5f, 1);
        scrollRT.anchoredPosition = source.anchoredPosition + new Vector2(0, 150);
        scrollRT.sizeDelta = new Vector2(0, source.sizeDelta.y);

        content.sizeDelta = new Vector2(0, content.sizeDelta.y);

        var le = content.gameObject.GetOrAddComponent<LayoutElement>();
        le.flexibleWidth = 1;
        var fitter = content.gameObject.GetOrAddComponent<ContentSizeFitter>();
        fitter.horizontalFit = ContentSizeFitter.FitMode.Unconstrained;
        AddVerticalScrollbar(scrollRect, 8f, 8f);

        Object.Destroy(source.gameObject);
        LayoutRebuilder.ForceRebuildLayoutImmediate(content);

        return scrollRect;
    }

    public static Scrollbar AddVerticalScrollbar(
        ScrollRect scrollRect,
        float width = 12f,
        float spacing = -2f,
        ScrollRect.ScrollbarVisibility visibility =
            ScrollRect.ScrollbarVisibility.AutoHide)
    {
        if (scrollRect == null)
        {
            Logger.Error("AddVerticalScrollbar: scrollRect is null");
            return null;
        }

        var root = scrollRect.GetComponent<RectTransform>();

        if (scrollRect.verticalScrollbar != null)
        {
            var old = scrollRect.verticalScrollbar.gameObject;
            scrollRect.verticalScrollbar = null;
            Object.Destroy(old);
        }

        var sbGO = new GameObject("Scrollbar");
        sbGO.GetOrAddComponent<RectTransform>();
        sbGO.transform.SetParent(root, false);

        var sbRT = sbGO.GetComponent<RectTransform>();
        sbRT.anchorMin = new Vector2(1, 0);
        sbRT.anchorMax = new Vector2(1, 1);
        sbRT.pivot = new Vector2(1, 1);
        sbRT.sizeDelta = new Vector2(width, 0);
        sbRT.anchoredPosition = Vector2.zero;

        var bg = sbGO.AddComponent<Image>();
        bg.color = new Color(189f / 255f, 189f / 255f, 191f / 255f, 0.25f);
        bg.raycastTarget = true;

        var handleGO = new GameObject("Handle");
        handleGO.GetOrAddComponent<RectTransform>();
        handleGO.transform.SetParent(sbGO.transform, false);

        var handleRT = handleGO.GetComponent<RectTransform>();
        handleRT.anchorMin = new Vector2(0, 0);
        handleRT.anchorMax = new Vector2(1, 1);
        handleRT.pivot = new Vector2(0.5f, 0.5f);
        handleRT.sizeDelta = Vector2.zero;

        var handleImg = handleGO.AddComponent<Image>();
        handleImg.color = new Color(172f / 255f, 172f / 255f, 173f / 255f, 0.65f);
        handleImg.raycastTarget = true;

        var scrollbar = sbGO.AddComponent<Scrollbar>();
        scrollbar.direction = Scrollbar.Direction.BottomToTop;
        scrollbar.handleRect = handleRT;
        scrollbar.targetGraphic = handleImg;

        scrollRect.vertical = true;
        scrollRect.horizontal = false;

        scrollRect.verticalScrollbar = scrollbar;
        scrollRect.verticalScrollbarVisibility = visibility;
        scrollRect.verticalScrollbarSpacing = spacing;

        var content = scrollRect.content;
        if (content != null)
            LayoutRebuilder.ForceRebuildLayoutImmediate(content);

        Canvas.ForceUpdateCanvases();

        return scrollbar;
    }
}