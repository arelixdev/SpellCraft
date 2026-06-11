using UnityEngine;
using UnityEngine.UI;
using Sirenix.OdinInspector;

#if UNITY_EDITOR
using UnityEditor;
#endif

/// Editor utility: click [Build UI] to generate the entire crafting UI hierarchy.
/// Attach to any GameObject in the scene. The button is safe to run multiple times
/// (it destroys the old Canvas first).
public class SpellCraftingUIBuilder : MonoBehaviour
{
    [Title("References to wire up")]
    public SpellCaster        TargetCaster;
    public SpellNodeCatalogSO NodeCatalog;

    [Title("Prefab colours / sizing")]
    public Color PanelBg       = new Color(0.08f, 0.08f, 0.12f, 0.95f);
    public Color NodeBg        = new Color(0.15f, 0.15f, 0.20f, 1.00f);
    public Color BottomBarBg   = new Color(0.05f, 0.05f, 0.08f, 1.00f);

    [Button("Build UI"), GUIColor(0.4f, 0.9f, 0.4f)]
    public void BuildUI()
    {
        // Destroy old canvas if present
        var old = GameObject.Find("SpellCraftingCanvas");
        if (old != null) DestroyImmediate(old);

        // ── Root Canvas ──────────────────────────────────────────────────
        var canvasGO  = NewGO("SpellCraftingCanvas");
        var canvas    = canvasGO.AddComponent<Canvas>();
        canvas.renderMode = RenderMode.ScreenSpaceOverlay;
        canvas.sortingOrder = 100;
        canvasGO.AddComponent<CanvasScaler>().uiScaleMode = CanvasScaler.ScaleMode.ScaleWithScreenSize;
        canvasGO.AddComponent<GraphicRaycaster>();
        var toggle    = canvasGO.AddComponent<SpellCraftingToggle>();

        // ── Panel Root (hidden by default) ───────────────────────────────
        var panelRoot = NewRTChild("CraftingPanel", canvasGO.transform);
        StretchFull(panelRoot);
        SetImage(panelRoot, PanelBg);

        var panel = panelRoot.AddComponent<SpellCraftingPanel>();
        panel.TargetCaster = TargetCaster;

        // ── Left: Inventory ──────────────────────────────────────────────
        var invPanel = NewRTChild("InventoryPanel", panelRoot.transform);
        var invRT    = invPanel.GetComponent<RectTransform>();
        invRT.anchorMin = Vector2.zero; invRT.anchorMax = new Vector2(0.22f, 1f);
        invRT.offsetMin = invRT.offsetMax = Vector2.zero;
        SetImage(invPanel, new Color(0.06f, 0.06f, 0.10f));

        var scroll  = invPanel.AddComponent<ScrollRect>();
        var content = NewRTChild("Content", invPanel.transform);
        StretchFull(content);
        var vlg     = content.AddComponent<VerticalLayoutGroup>();
        vlg.childControlWidth = true; vlg.childControlHeight = false;
        vlg.spacing           = 6;    vlg.padding             = new RectOffset(6, 6, 6, 6);
        content.AddComponent<ContentSizeFitter>().verticalFit = ContentSizeFitter.FitMode.PreferredSize;
        scroll.content   = content.GetComponent<RectTransform>();
        scroll.vertical  = true;
        scroll.horizontal = false;

        var invComp  = invPanel.AddComponent<NodeInventoryPanel>();
        invComp.ContentRoot = content.transform;
        invComp.Catalog     = NodeCatalog;
        invComp.NodeCardPrefab = BuildNodeCardPrefab();

        // ── Centre: Graph Area ───────────────────────────────────────────
        var graphArea = NewRTChild("GraphArea", panelRoot.transform);
        var gaRT      = graphArea.GetComponent<RectTransform>();
        gaRT.anchorMin = new Vector2(0.22f, 0.08f);
        gaRT.anchorMax = new Vector2(1f,    1f);
        gaRT.offsetMin = gaRT.offsetMax = Vector2.zero;
        SetImage(graphArea, new Color(0.10f, 0.10f, 0.14f));

        // Pending connection line (fills GraphArea)
        var pendingGO   = NewRTChild("PendingLine", graphArea.transform);
        StretchFull(pendingGO);
        pendingGO.AddComponent<UIBezierLine>().color = new Color(1f, 0.8f, 0.2f, 0.8f);
        pendingGO.AddComponent<PendingConnectionController>();
        pendingGO.SetActive(false);

        var gca          = graphArea.AddComponent<GraphCanvasController>();
        gca.GraphArea    = gaRT;
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
        hlg.childControlHeight = true; hlg.childForceExpandWidth = false;
        hlg.spacing = 8; hlg.padding = new RectOffset(8, 8, 4, 4);

        var budgetLabel  = NewTextChild("BudgetLabel",  bottomBar.transform, "0 / 10");
        var budgetSlider = NewSliderChild("BudgetSlider", bottomBar.transform);
        var slotContainer = NewRTChild("SlotContainer",  bottomBar.transform);
        slotContainer.AddComponent<HorizontalLayoutGroup>().spacing = 4;
        int slotCount = TargetCaster ? TargetCaster.GetSlots().Length : 4;
        for (int i = 0; i < slotCount; i++) NewButtonChild($"Slot{i}", slotContainer.transform, $"Slot {i}");
        var applyBtn = NewButtonChild("ApplyButton", bottomBar.transform, "Apply");

        var bbc         = bottomBar.AddComponent<BottomBarController>();
        bbc.BudgetLabel  = budgetLabel.GetComponent<Text>();
        bbc.BudgetSlider = budgetSlider.GetComponent<Slider>();
        bbc.ApplyButton  = applyBtn.GetComponent<Button>();

        // ── Wire panel ───────────────────────────────────────────────────
        panel.CanvasController = gca;
        panel.InventoryPanel   = invComp;
        panel.BottomBar        = bbc;

        SetPrivateField(toggle, "_panelRoot", panelRoot);
        SetPrivateField(toggle, "_panel",     panel);

        Debug.Log("[SpellCraftingUIBuilder] Canvas built successfully.");

#if UNITY_EDITOR
        UnityEditor.Selection.activeGameObject = canvasGO;
#endif
    }

    // ── Prefab builders ──────────────────────────────────────────────────

    private GameObject BuildNodeCardPrefab()
    {
        var go = NewGO("NodeCardPrefab");
        var rt = go.AddComponent<RectTransform>();
        rt.sizeDelta = new Vector2(0f, 60f);
        SetImage(go, NodeBg);
        go.AddComponent<NodeCardView>();
        var vl = go.AddComponent<VerticalLayoutGroup>();
        vl.childControlWidth = true; vl.childControlHeight = false;
        NewTextChild("NameLabel",  go.transform, "NodeName");
        NewTextChild("CostLabel",  go.transform, "Cost: 1");
        go.SetActive(false);
        return go;
    }

    private GameObject BuildNodeViewPrefab()
    {
        var go = NewGO("NodeViewPrefab");
        go.AddComponent<RectTransform>().sizeDelta = new Vector2(150f, 80f);
        SetImage(go, NodeBg);
        go.AddComponent<NodeView>();
        go.AddComponent<CanvasGroup>();
        var vl = go.AddComponent<VerticalLayoutGroup>();
        vl.childControlWidth = true; vl.childControlHeight = false;
        vl.padding = new RectOffset(4, 4, 4, 4); vl.spacing = 2;
        NewTextChild("NameLabel", go.transform, "Node");
        NewTextChild("CostLabel", go.transform, "[1]");

        // Input port (left side)
        var inPort = NewGO("InputPort");
        var inRT   = inPort.AddComponent<RectTransform>();
        inRT.sizeDelta = new Vector2(12f, 12f);
        SetImage(inPort, new Color(0.3f, 0.75f, 1f));
        inPort.AddComponent<PortView>();
        inPort.transform.SetParent(go.transform, false);

        // Output port (right side)
        var outPort = NewGO("OutputPort");
        var outRT   = outPort.AddComponent<RectTransform>();
        outRT.sizeDelta = new Vector2(12f, 12f);
        SetImage(outPort, new Color(1f, 0.6f, 0.2f));
        outPort.AddComponent<PortView>();
        outPort.transform.SetParent(go.transform, false);

        go.SetActive(false);
        return go;
    }

    private GameObject BuildConnectionViewPrefab()
    {
        var go = NewGO("ConnectionViewPrefab");
        go.AddComponent<RectTransform>();
        var line = go.AddComponent<UIBezierLine>();
        line.color = new Color(0.8f, 0.8f, 0.8f, 0.9f);
        go.AddComponent<ConnectionView>();
        go.SetActive(false);
        return go;
    }

    // ── UI helpers ───────────────────────────────────────────────────────

    private static GameObject NewGO(string name)
    {
        var go = new GameObject(name);
        return go;
    }

    private static GameObject NewRTChild(string name, Transform parent)
    {
        var go = new GameObject(name, typeof(RectTransform));
        go.transform.SetParent(parent, false);
        return go;
    }

    private static void StretchFull(GameObject go)
    {
        var rt = go.GetComponent<RectTransform>();
        rt.anchorMin = Vector2.zero;
        rt.anchorMax = Vector2.one;
        rt.offsetMin = rt.offsetMax = Vector2.zero;
    }

    private static void SetImage(GameObject go, Color c)
    {
        var img   = go.GetComponent<Image>() ?? go.AddComponent<Image>();
        img.color = c;
    }

    private static GameObject NewTextChild(string name, Transform parent, string text)
    {
        var go  = NewRTChild(name, parent);
        var lef = go.AddComponent<LayoutElement>();
        lef.preferredHeight = 20f;
        var txt = go.AddComponent<Text>();
        txt.text      = text;
        txt.fontSize  = 12;
        txt.color     = Color.white;
        txt.alignment = TextAnchor.MiddleLeft;
        return go;
    }

    private static GameObject NewSliderChild(string name, Transform parent)
    {
        var go  = NewRTChild(name, parent);
        var lef = go.AddComponent<LayoutElement>();
        lef.preferredWidth  = 120f;
        lef.preferredHeight = 20f;
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
        var txt  = NewTextChild("Label", go.transform, label);
        StretchFull(txt);
        return go;
    }

    private static void SetPrivateField(object obj, string field, object value)
    {
        var f = obj.GetType().GetField(field,
            System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance);
        f?.SetValue(obj, value);
    }
}
