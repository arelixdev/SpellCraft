using UnityEngine;
using UnityEngine.UI;
using UnityEngine.EventSystems;
using Sirenix.OdinInspector;

#if UNITY_EDITOR
using UnityEditor;
#endif

public class SpellCraftingUIBuilder : MonoBehaviour
{
    [Title("References to wire up")]
    public SpellCaster        TargetCaster;
    public SpellNodeCatalogSO NodeCatalog;

    [Title("Prefab colours / sizing")]
    public Color PanelBg     = new Color(0.08f, 0.08f, 0.12f, 0.95f);
    public Color NodeBg      = new Color(0.15f, 0.15f, 0.20f, 1.00f);
    public Color BottomBarBg = new Color(0.05f, 0.05f, 0.08f, 1.00f);

    [Button("Build UI"), GUIColor(0.4f, 0.9f, 0.4f)]
    public void BuildUI()
    {
        // ── EventSystem (obligatoire pour tous les clics/drags UI) ───────
        EnsureEventSystem();

        // Destroy old canvas if present
        var old = GameObject.Find("SpellCraftingCanvas");
        if (old != null) DestroyImmediate(old);

        // ── Root Canvas ──────────────────────────────────────────────────
        var canvasGO = NewGO("SpellCraftingCanvas");
        var canvas   = canvasGO.AddComponent<Canvas>();
        canvas.renderMode   = RenderMode.ScreenSpaceOverlay;
        canvas.sortingOrder = 100;
        var scaler = canvasGO.AddComponent<CanvasScaler>();
        scaler.uiScaleMode         = CanvasScaler.ScaleMode.ScaleWithScreenSize;
        scaler.referenceResolution = new Vector2(1920, 1080);
        canvasGO.AddComponent<GraphicRaycaster>();
        var toggle = canvasGO.AddComponent<SpellCraftingToggle>();

        // ── Panel Root (hidden by default) ───────────────────────────────
        var panelRoot = NewRTChild("CraftingPanel", canvasGO.transform);
        StretchFull(panelRoot);
        SetImage(panelRoot, PanelBg);
        var panel = panelRoot.AddComponent<SpellCraftingPanel>();
        panel.TargetCaster = TargetCaster;

        // ── Left: Inventory ──────────────────────────────────────────────
        var invPanel = NewRTChild("InventoryPanel", panelRoot.transform);
        var invRT    = invPanel.GetComponent<RectTransform>();
        invRT.anchorMin = Vector2.zero;
        invRT.anchorMax = new Vector2(0.22f, 1f);
        invRT.offsetMin = invRT.offsetMax = Vector2.zero;
        SetImage(invPanel, new Color(0.06f, 0.06f, 0.10f));

        var scroll   = invPanel.AddComponent<ScrollRect>();

        var viewport = NewRTChild("Viewport", invPanel.transform);
        StretchFull(viewport);
        viewport.AddComponent<Image>().color = Color.clear;
        viewport.AddComponent<Mask>().showMaskGraphic = false;

        var content   = NewRTChild("Content", viewport.transform);
        var contentRT = content.GetComponent<RectTransform>();
        contentRT.anchorMin        = new Vector2(0f, 1f);
        contentRT.anchorMax        = new Vector2(1f, 1f);
        contentRT.pivot            = new Vector2(0.5f, 1f);
        contentRT.offsetMin        = contentRT.offsetMax = Vector2.zero;
        var vlg = content.AddComponent<VerticalLayoutGroup>();
        vlg.childControlWidth  = true;
        vlg.childControlHeight = false;
        vlg.spacing            = 6;
        vlg.padding            = new RectOffset(6, 6, 6, 6);
        content.AddComponent<ContentSizeFitter>().verticalFit = ContentSizeFitter.FitMode.PreferredSize;
        scroll.viewport   = viewport.GetComponent<RectTransform>();
        scroll.content    = contentRT;
        scroll.vertical   = true;
        scroll.horizontal = false;
        scroll.scrollSensitivity = 30f;

        var invComp            = invPanel.AddComponent<NodeInventoryPanel>();
        invComp.ContentRoot    = content.transform;
        invComp.Catalog        = NodeCatalog;
        invComp.NodeCardPrefab = BuildNodeCardPrefab();

        // ── Centre: Graph Area ───────────────────────────────────────────
        var graphArea = NewRTChild("GraphArea", panelRoot.transform);
        var gaRT      = graphArea.GetComponent<RectTransform>();
        gaRT.anchorMin = new Vector2(0.22f, 0.08f);
        gaRT.anchorMax = new Vector2(1f,    1f);
        gaRT.offsetMin = gaRT.offsetMax = Vector2.zero;
        SetImage(graphArea, new Color(0.10f, 0.10f, 0.14f));
        graphArea.AddComponent<GraphAreaClickHandler>();

        // Pending connection line (fills GraphArea, starts hidden)
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

        // ── Bottom: Budget + Slots + Apply ───────────────────────────────
        var bottomBar = NewRTChild("BottomBar", panelRoot.transform);
        var bbRT      = bottomBar.GetComponent<RectTransform>();
        bbRT.anchorMin = new Vector2(0.22f, 0f);
        bbRT.anchorMax = new Vector2(1f,    0.08f);
        bbRT.offsetMin = bbRT.offsetMax = Vector2.zero;
        SetImage(bottomBar, BottomBarBg);

        var hlg = bottomBar.AddComponent<HorizontalLayoutGroup>();
        hlg.childControlHeight  = true;
        hlg.childForceExpandWidth = false;
        hlg.childAlignment      = TextAnchor.MiddleLeft;
        hlg.spacing             = 8;
        hlg.padding             = new RectOffset(8, 8, 4, 4);

        var budgetLabel   = NewTextChild("BudgetLabel",   bottomBar.transform, "0 / 10");
        var budgetSlider  = NewSliderChild("BudgetSlider", bottomBar.transform);
        var slotContainer = NewRTChild("SlotContainer",   bottomBar.transform);
        var slotHLG       = slotContainer.AddComponent<HorizontalLayoutGroup>();
        slotHLG.childForceExpandWidth  = false;
        slotHLG.childControlHeight     = true;
        slotHLG.spacing                = 4;
        slotContainer.AddComponent<LayoutElement>().flexibleWidth = 1f;
        int slotCount = TargetCaster ? TargetCaster.GetSlots().Length : 4;
        for (int i = 0; i < slotCount; i++)
            NewButtonChild($"Slot{i}", slotContainer.transform, $"Slot {i}");
        var applyBtn = NewButtonChild("ApplyButton", bottomBar.transform, "Apply");

        var bbc          = bottomBar.AddComponent<BottomBarController>();
        bbc.BudgetLabel  = budgetLabel.GetComponent<Text>();
        bbc.BudgetSlider = budgetSlider.GetComponent<Slider>();
        bbc.ApplyButton  = applyBtn.GetComponent<Button>();

        // ── Wire everything ──────────────────────────────────────────────
        panel.CanvasController = gca;
        panel.InventoryPanel   = invComp;
        panel.BottomBar        = bbc;
        SetPrivateField(toggle, "_panelRoot", panelRoot);
        SetPrivateField(toggle, "_panel",     panel);

        Debug.Log("[SpellCraftingUIBuilder] Canvas built successfully.");

#if UNITY_EDITOR
        Selection.activeGameObject = canvasGO;
#endif
    }

    // ── EventSystem ──────────────────────────────────────────────────────

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
        Debug.Log("[SpellCraftingUIBuilder] EventSystem créé.");
    }

    // ── Prefab builders ──────────────────────────────────────────────────

    private GameObject BuildNodeCardPrefab()
    {
        var go = NewGO("NodeCardPrefab");
        go.AddComponent<RectTransform>().sizeDelta = new Vector2(0f, 56f);
        SetImage(go, NodeBg);
        go.AddComponent<NodeCardView>();
        var le = go.AddComponent<LayoutElement>();
        le.minHeight       = 56f;
        le.preferredHeight = 56f;
        var vl = go.AddComponent<VerticalLayoutGroup>();
        vl.childControlWidth    = true;
        vl.childControlHeight   = true;
        vl.childForceExpandHeight = false;
        vl.childAlignment       = TextAnchor.MiddleLeft;
        vl.padding              = new RectOffset(10, 10, 6, 6);
        vl.spacing              = 2;
        NewTextChild("NameLabel", go.transform, "NodeName", 14, true);
        NewTextChild("CostLabel", go.transform, "Cost: 1",  11, false);
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

        // HLG : [InputPort 24px] [Content flex] [OutputPort 24px]
        // Les ports occupent toute la hauteur → zone de clic généreuse
        var hlg = go.AddComponent<HorizontalLayoutGroup>();
        hlg.childControlWidth      = true;
        hlg.childControlHeight     = true;
        hlg.childForceExpandHeight = true;
        hlg.childForceExpandWidth  = false;
        hlg.spacing                = 0;
        hlg.padding                = new RectOffset(0, 0, 0, 0);

        // Port INPUT — bande bleue à gauche, toute la hauteur
        var inPort = NewRTChild("InputPort", go.transform);
        inPort.AddComponent<LayoutElement>().preferredWidth = 24f;
        SetImage(inPort, new Color(0.25f, 0.65f, 1f));
        inPort.AddComponent<PortView>();

        // Contenu central — nom + coût
        var center = NewRTChild("Content", go.transform);
        center.AddComponent<LayoutElement>().flexibleWidth = 1f;
        var vl = center.AddComponent<VerticalLayoutGroup>();
        vl.childControlWidth      = true;
        vl.childControlHeight     = true;
        vl.childForceExpandHeight = false;
        vl.childAlignment         = TextAnchor.MiddleCenter;
        vl.padding                = new RectOffset(4, 4, 6, 6);
        vl.spacing                = 2;
        NewTextChild("NameLabel", center.transform, "Node", 13, true);
        NewTextChild("CostLabel", center.transform, "[1]",  11, false);

        // Port OUTPUT — bande orange à droite, toute la hauteur
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

    // ── UI helpers ───────────────────────────────────────────────────────

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

    private static GameObject NewTextChild(string name, Transform parent, string text,
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

    private static GameObject NewSliderChild(string name, Transform parent)
    {
        var go = NewRTChild(name, parent);
        var le = go.AddComponent<LayoutElement>();
        le.preferredWidth  = 140f;
        le.preferredHeight = 20f;
        go.AddComponent<Slider>();
        return go;
    }

    private static GameObject NewButtonChild(string name, Transform parent, string label)
    {
        var go = NewRTChild(name, parent);
        var le = go.AddComponent<LayoutElement>();
        le.preferredWidth  = 80f;
        le.preferredHeight = 32f;
        SetImage(go, new Color(0.2f, 0.2f, 0.35f));
        go.AddComponent<Button>();
        var lbl = NewRTChild("Label", go.transform);
        StretchFull(lbl);
        var txt = lbl.AddComponent<Text>();
        txt.text      = label;
        txt.fontSize  = 12;
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
