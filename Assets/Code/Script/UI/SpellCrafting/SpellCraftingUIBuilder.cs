using UnityEngine;
using UnityEngine.UI;
using UnityEngine.EventSystems;
using TMPro;
using Sirenix.OdinInspector;

#if UNITY_EDITOR
using UnityEditor;
#endif

public class SpellCraftingUIBuilder : MonoBehaviour
{
    [Title("References to wire up")]
    public SpellCaster        TargetCaster;
    public SpellNodeCatalogSO NodeCatalog;

    [Title("Colours")]
    public Color PanelBg     = new Color(0.08f, 0.08f, 0.12f, 0.97f);
    public Color NodeBg      = new Color(0.15f, 0.15f, 0.20f, 1.00f);
    public Color SidebarBg   = new Color(0.04f, 0.04f, 0.07f, 1.00f);
    public Color InvBarBg    = new Color(0.06f, 0.06f, 0.10f, 1.00f);

    [Title("Sizing")]
    public float DrawerWidth    = 860f;   // px at 1920×1080
    public float SidebarWidth   = 80f;
    public float InvBarHeight   = 90f;

    [Button("Build UI"), GUIColor(0.4f, 0.9f, 0.4f)]
    public void BuildUI()
    {
        EnsureEventSystem();

        var old = GameObject.Find("SpellCraftingCanvas");
        if (old != null) DestroyImmediate(old);

        // ── Root Canvas ─────────────────────────────────────────────────────
        var canvasGO = NewGO("SpellCraftingCanvas");
        var canvas   = canvasGO.AddComponent<Canvas>();
        canvas.renderMode   = RenderMode.ScreenSpaceOverlay;
        canvas.sortingOrder = 100;
        var scaler = canvasGO.AddComponent<CanvasScaler>();
        scaler.uiScaleMode         = CanvasScaler.ScaleMode.ScaleWithScreenSize;
        scaler.referenceResolution = new Vector2(1920, 1080);
        canvasGO.AddComponent<GraphicRaycaster>();
        var toggle = canvasGO.AddComponent<SpellCraftingToggle>();

        // ── Panel Root — right-side drawer ──────────────────────────────────
        // pivot (1,0.5) + anchor right edge → slide via anchoredPosition.x
        var panelRoot = NewRTChild("CraftingPanel", canvasGO.transform);
        var panelRT   = panelRoot.GetComponent<RectTransform>();
        panelRT.anchorMin        = new Vector2(1f, 0f);
        panelRT.anchorMax        = new Vector2(1f, 1f);
        panelRT.pivot            = new Vector2(1f, 0.5f);
        panelRT.sizeDelta        = new Vector2(DrawerWidth, 0f);
        panelRT.anchoredPosition = new Vector2(DrawerWidth, 0f); // hidden off-screen right
        SetImage(panelRoot, PanelBg);

        var panel = panelRoot.AddComponent<SpellCraftingPanel>();
        panel.TargetCaster = TargetCaster;

        // ── Left: Slot Sidebar ───────────────────────────────────────────────
        var sidebar   = NewRTChild("SlotSidebar", panelRoot.transform);
        var sbRT      = sidebar.GetComponent<RectTransform>();
        sbRT.anchorMin        = new Vector2(0f, 0f);
        sbRT.anchorMax        = new Vector2(0f, 1f);
        sbRT.pivot            = new Vector2(0f, 0.5f);
        sbRT.sizeDelta        = new Vector2(SidebarWidth, 0f);
        sbRT.anchoredPosition = Vector2.zero;
        SetImage(sidebar, SidebarBg);

        // VLG on sidebar: slots fill top, budget+apply sit at bottom
        var sbVLG = sidebar.AddComponent<VerticalLayoutGroup>();
        sbVLG.childControlWidth      = true;
        sbVLG.childControlHeight     = false;
        sbVLG.childForceExpandWidth  = true;
        sbVLG.childForceExpandHeight = false;
        sbVLG.childAlignment         = TextAnchor.UpperCenter;
        sbVLG.spacing                = 0;
        sbVLG.padding                = new RectOffset(0, 0, 0, 0);

        // SlotContainer — dynamic slot buttons spawned at runtime
        var slotContainerGO = NewRTChild("SlotContainer", sidebar.transform);
        var scLE            = slotContainerGO.AddComponent<LayoutElement>();
        scLE.flexibleHeight = 1f;
        var scVLG = slotContainerGO.AddComponent<VerticalLayoutGroup>();
        scVLG.childControlWidth     = true;
        scVLG.childControlHeight    = false;
        scVLG.childForceExpandWidth = true;
        scVLG.childAlignment        = TextAnchor.UpperCenter;
        scVLG.spacing               = 10;
        scVLG.padding               = new RectOffset(8, 8, 12, 8);

        // Budget label
        var budgetLabelGO = NewRTChild("BudgetLabel", sidebar.transform);
        budgetLabelGO.AddComponent<LayoutElement>().preferredHeight = 22f;
        var budgetTMP  = budgetLabelGO.AddComponent<TextMeshProUGUI>();
        budgetTMP.text      = "0 / 10";
        budgetTMP.fontSize  = 12;
        budgetTMP.alignment = TextAlignmentOptions.Center;
        budgetTMP.color     = Color.white;

        // Budget slider
        var sliderGO = NewRTChild("BudgetSlider", sidebar.transform);
        sliderGO.AddComponent<LayoutElement>().preferredHeight = 16f;
        var slider   = sliderGO.AddComponent<Slider>();

        // Apply button
        var applyGO = NewRTChild("ApplyButton", sidebar.transform);
        var applyLE = applyGO.AddComponent<LayoutElement>();
        applyLE.preferredHeight = 40f;
        applyLE.minHeight       = 40f;
        SetImage(applyGO, new Color(0.2f, 0.5f, 0.2f));
        var applyBtn = applyGO.AddComponent<Button>();
        var applyLbl = NewRTChild("Label", applyGO.transform);
        StretchFull(applyLbl);
        var applyTxt      = applyLbl.AddComponent<TextMeshProUGUI>();
        applyTxt.text     = "Apply";
        applyTxt.fontSize = 13;
        applyTxt.color    = Color.white;
        applyTxt.alignment = TextAlignmentOptions.Center;

        // Wire SlotSidebarController
        var sidebarComp          = sidebar.AddComponent<SlotSidebarController>();
        sidebarComp.SlotContainer = slotContainerGO.transform;
        sidebarComp.BudgetLabel   = budgetTMP;
        sidebarComp.BudgetSlider  = slider;
        sidebarComp.ApplyButton   = applyBtn;

        // ── Main Area (right of sidebar) ─────────────────────────────────────
        var mainArea = NewRTChild("MainArea", panelRoot.transform);
        var maRT     = mainArea.GetComponent<RectTransform>();
        maRT.anchorMin = Vector2.zero;
        maRT.anchorMax = Vector2.one;
        maRT.offsetMin = new Vector2(SidebarWidth, 0f);
        maRT.offsetMax = Vector2.zero;
        SetImage(mainArea, PanelBg);

        // ── Inventory Bar (top of main area, horizontal scroll) ──────────────
        var invBar = NewRTChild("InventoryBar", mainArea.transform);
        var ibRT   = invBar.GetComponent<RectTransform>();
        ibRT.anchorMin = new Vector2(0f, 1f);
        ibRT.anchorMax = new Vector2(1f, 1f);
        ibRT.pivot     = new Vector2(0.5f, 1f);
        ibRT.sizeDelta = new Vector2(0f, InvBarHeight);
        ibRT.anchoredPosition = Vector2.zero;
        SetImage(invBar, InvBarBg);

        var invScroll    = invBar.AddComponent<ScrollRect>();
        var invViewport  = NewRTChild("Viewport", invBar.transform);
        StretchFull(invViewport);
        invViewport.AddComponent<Image>().color = Color.clear;
        invViewport.AddComponent<Mask>().showMaskGraphic = false;

        var invContent    = NewRTChild("Content", invViewport.transform);
        var icRT          = invContent.GetComponent<RectTransform>();
        icRT.anchorMin    = new Vector2(0f, 0f);
        icRT.anchorMax    = new Vector2(0f, 1f);
        icRT.pivot        = new Vector2(0f, 0.5f);
        icRT.offsetMin    = icRT.offsetMax = Vector2.zero;
        var icHLG = invContent.AddComponent<HorizontalLayoutGroup>();
        icHLG.childControlHeight     = true;
        icHLG.childControlWidth      = false;
        icHLG.childForceExpandHeight = true;
        icHLG.childForceExpandWidth  = false;
        icHLG.spacing                = 6;
        icHLG.padding                = new RectOffset(6, 6, 6, 6);
        invContent.AddComponent<ContentSizeFitter>().horizontalFit = ContentSizeFitter.FitMode.PreferredSize;

        invScroll.viewport        = invViewport.GetComponent<RectTransform>();
        invScroll.content         = icRT;
        invScroll.horizontal      = true;
        invScroll.vertical        = false;
        invScroll.scrollSensitivity = 30f;

        var invComp            = invBar.AddComponent<NodeInventoryPanel>();
        invComp.ContentRoot    = invContent.transform;
        invComp.Catalog        = NodeCatalog;
        invComp.NodeCardPrefab = BuildNodeCardPrefab();

        // ── Graph Area (below inventory bar) ─────────────────────────────────
        var graphArea = NewRTChild("GraphArea", mainArea.transform);
        var gaRT      = graphArea.GetComponent<RectTransform>();
        gaRT.anchorMin = Vector2.zero;
        gaRT.anchorMax = Vector2.one;
        gaRT.offsetMin = Vector2.zero;
        gaRT.offsetMax = new Vector2(0f, -InvBarHeight);
        SetImage(graphArea, new Color(0.10f, 0.10f, 0.14f));
        graphArea.AddComponent<GraphAreaClickHandler>();

        var pendingGO = NewRTChild("PendingLine", graphArea.transform);
        StretchFull(pendingGO);
        pendingGO.AddComponent<UIBezierLine>().color = new Color(1f, 0.8f, 0.2f, 0.8f);
        pendingGO.AddComponent<PendingConnectionController>();
        pendingGO.SetActive(false);

        var gca                  = graphArea.AddComponent<GraphCanvasController>();
        gca.GraphArea            = gaRT;
        gca.NodeViewPrefab       = BuildNodeViewPrefab();
        gca.ConnectionViewPrefab = BuildConnectionViewPrefab();
        gca.PendingLinePrefab    = pendingGO;

        // ── Launcher→Node0 bezier — added LAST so it renders above opaque panels ─
        var connLineGO    = NewRTChild("LauncherConnectionLine", panelRoot.transform);
        StretchFull(connLineGO);
        var connBezier    = connLineGO.AddComponent<UIBezierLine>();
        connBezier.color  = new Color(1f, 0.7f, 0.15f, 0.85f);
        var connLine      = connLineGO.AddComponent<LauncherConnectionLine>();

        // ── Wire everything ──────────────────────────────────────────────────
        panel.CanvasController = gca;
        panel.InventoryPanel   = invComp;
        panel.SlotSidebar      = sidebarComp;
        connLine.Sidebar       = sidebarComp;
        connLine.Canvas        = gca;
        connLine.Panel         = panel;
        SetPrivateField(toggle, "_panelRT", panelRT);
        SetPrivateField(toggle, "_panel",   panel);

        Debug.Log("[SpellCraftingUIBuilder] Canvas built successfully.");

#if UNITY_EDITOR
        Selection.activeGameObject = canvasGO;
#endif
    }

    // ── EventSystem ──────────────────────────────────────────────────────────

    private static void EnsureEventSystem()
    {
        if (FindObjectOfType<EventSystem>() != null) return;
        var esGO = new GameObject("EventSystem");
        esGO.AddComponent<EventSystem>();
#if ENABLE_INPUT_SYSTEM && !ENABLE_LEGACY_INPUT_MANAGER
        esGO.AddComponent<UnityEngine.InputSystem.UI.InputSystemUIInputModule>();
#else
        esGO.AddComponent<StandaloneInputModule>();
#endif
    }

    // ── Prefab builders ──────────────────────────────────────────────────────

    private GameObject BuildNodeCardPrefab()
    {
        // Horizontal card for the top inventory strip
        var go = NewGO("NodeCardPrefab");
        go.AddComponent<RectTransform>();
        SetImage(go, NodeBg);
        go.AddComponent<NodeCardView>();
        var le = go.AddComponent<LayoutElement>();
        le.preferredWidth  = 90f;
        le.minWidth        = 90f;
        var vl = go.AddComponent<VerticalLayoutGroup>();
        vl.childControlWidth      = true;
        vl.childControlHeight     = true;
        vl.childForceExpandHeight = false;
        vl.childAlignment         = TextAnchor.MiddleCenter;
        vl.padding                = new RectOffset(6, 6, 6, 6);
        vl.spacing                = 2;
        NewLegacyText("NameLabel", go.transform, "NodeName", 12, true);
        NewLegacyText("CostLabel", go.transform, "Cost: 1",  10, false);
        go.SetActive(false);
        return go;
    }

    private GameObject BuildNodeViewPrefab()
    {
        var go = NewGO("NodeViewPrefab");
        go.AddComponent<RectTransform>().sizeDelta = new Vector2(160f, 64f);
        SetImage(go, NodeBg);
        go.AddComponent<NodeView>();
        go.AddComponent<CanvasGroup>();

        var hlg = go.AddComponent<HorizontalLayoutGroup>();
        hlg.childControlWidth      = true;
        hlg.childControlHeight     = true;
        hlg.childForceExpandHeight = true;
        hlg.childForceExpandWidth  = false;
        hlg.spacing                = 0;

        var inPort = NewRTChild("InputPort", go.transform);
        inPort.AddComponent<LayoutElement>().preferredWidth = 24f;
        SetImage(inPort, new Color(0.25f, 0.65f, 1f));
        inPort.AddComponent<PortView>();

        var center = NewRTChild("Content", go.transform);
        center.AddComponent<LayoutElement>().flexibleWidth = 1f;
        var vl = center.AddComponent<VerticalLayoutGroup>();
        vl.childControlWidth      = true;
        vl.childControlHeight     = true;
        vl.childForceExpandHeight = false;
        vl.childAlignment         = TextAnchor.MiddleCenter;
        vl.padding                = new RectOffset(4, 4, 6, 6);
        vl.spacing                = 2;
        NewLegacyText("NameLabel", center.transform, "Node", 13, true);
        NewLegacyText("CostLabel", center.transform, "[1]",  11, false);

        var outPort = NewRTChild("OutputPort", go.transform);
        outPort.AddComponent<LayoutElement>().preferredWidth = 24f;
        SetImage(outPort, new Color(1f, 0.55f, 0.15f));
        outPort.AddComponent<PortView>();

        go.SetActive(false);
        return go;
    }

    private GameObject BuildConnectionViewPrefab()
    {
        var go = NewGO("ConnectionViewPrefab");
        go.AddComponent<RectTransform>();
        go.AddComponent<UIBezierLine>().color = new Color(0.8f, 0.8f, 0.8f, 0.9f);
        go.AddComponent<ConnectionView>();
        go.SetActive(false);
        return go;
    }

    // ── UI helpers ───────────────────────────────────────────────────────────

    private static GameObject NewGO(string name) => new GameObject(name);

    private static GameObject NewRTChild(string name, Transform parent)
    {
        var go = new GameObject(name, typeof(RectTransform));
        go.transform.SetParent(parent, false);
        return go;
    }

    private static void StretchFull(GameObject go)
    {
        var rt       = go.GetComponent<RectTransform>();
        rt.anchorMin = Vector2.zero;
        rt.anchorMax = Vector2.one;
        rt.offsetMin = rt.offsetMax = Vector2.zero;
    }

    private static void SetImage(GameObject go, Color c)
    {
        var img   = go.GetComponent<Image>() ?? go.AddComponent<Image>();
        img.color = c;
    }

    private static GameObject NewLegacyText(string name, Transform parent, string text,
                                             int fontSize = 12, bool bold = false)
    {
        var go  = NewRTChild(name, parent);
        var le  = go.AddComponent<LayoutElement>();
        le.preferredHeight = fontSize + 8f;
        var txt = go.AddComponent<Text>();
        txt.text      = text;
        txt.fontSize  = fontSize;
        txt.fontStyle = bold ? FontStyle.Bold : FontStyle.Normal;
        txt.color     = Color.white;
        txt.alignment = TextAnchor.MiddleCenter;
        return go;
    }

    private static void SetPrivateField(object obj, string field, object value)
    {
        var f = obj.GetType().GetField(field,
            System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance);
        f?.SetValue(obj, value);
    }
}
