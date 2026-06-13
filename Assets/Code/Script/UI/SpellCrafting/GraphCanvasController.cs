using System.Collections.Generic;
using UnityEngine;

/// Manages the node canvas: all nodes live in one shared SpellGraphSO.
/// Slots are entry-points — any slot can connect to any node.
public class GraphCanvasController : MonoBehaviour
{
    public static GraphCanvasController Instance { get; private set; }

    [Header("Prefabs (set by SpellCraftingUIBuilder)")]
    public GameObject NodeViewPrefab;
    public GameObject ConnectionViewPrefab;
    public GameObject PendingLinePrefab;

    [Header("References (set by SpellCraftingUIBuilder)")]
    public RectTransform GraphArea;

    // Fired after any mutation
    public event System.Action OnGraphModified;

    private SpellGraphSO          _workingGraph;
    private List<NodeView>        _nodeViews       = new();
    private List<ConnectionView>  _connectionViews = new();
    private PendingConnectionController _pending;

    private const float NODE_SPACING = 180f;
    private const float ROW_HEIGHT   = 150f;
    private const float ROW_START_X  = -200f;
    private const float ROW_START_Y  =  80f;

    private void Awake() => Instance = this;

    // ── Graph Load / Flush ───────────────────────────────────────────────────

    public void LoadGraph(SpellGraphSO graph)
    {
        ClearGraph();
        _workingGraph = graph;
        if (graph == null) return;

        for (int i = 0; i < graph.nodes.Count; i++)
        {
            var node = graph.nodes[i];
            if (node == null) continue;
            var view = SpawnNodeView(node, i);

            // Use saved layout position if available, else compute a grid position
            var placement = graph.editorLayout.Find(p => p.nodeIndex == i);
            view.SetLocalPosition(placement.canvasPosition != default
                ? placement.canvasPosition
                : GridPosition(i));
        }

        foreach (var conn in graph.connections)
        {
            var from = GetNodeView(conn.fromIndex)?.OutputPort;
            var to   = GetNodeView(conn.toIndex)?.InputPort;
            if (from != null && to != null) SpawnConnectionView(from, to);
        }
    }

    public void FlushToGraph(SpellGraphSO graph)
    {
        if (graph == null) return;
        graph.connections.Clear();
        graph.editorLayout.Clear();

        foreach (var cv in _connectionViews)
            graph.connections.Add(new SpellGraphSO.Connection
            {
                fromIndex = cv.FromNodeIndex,
                toIndex   = cv.ToNodeIndex
            });

        foreach (var nv in _nodeViews)
            graph.editorLayout.Add(new SpellGraphSO.NodePlacement
            {
                nodeIndex      = nv.NodeIndex,
                canvasPosition = nv.GetLocalPosition()
            });
    }

    public void ClearGraph()
    {
        foreach (var cv in _connectionViews) if (cv) Destroy(cv.gameObject);
        foreach (var nv in _nodeViews)       if (nv) Destroy(nv.gameObject);
        _connectionViews.Clear();
        _nodeViews.Clear();
        _workingGraph = null;
    }

    // ── Node Management ──────────────────────────────────────────────────────

    public void AddNode(SpellNodeSO data)
    {
        if (_workingGraph == null) return;
        _workingGraph.nodes.Add(data);
        int newIdx = _workingGraph.nodes.Count - 1;
        var view = SpawnNodeView(data, newIdx);
        view.SetLocalPosition(GridPosition(newIdx));
        OnGraphModified?.Invoke();
    }

    public void DeleteNode(NodeView view)
    {
        int idx = view.NodeIndex;

        // Remove connections touching this node
        for (int i = _connectionViews.Count - 1; i >= 0; i--)
        {
            var cv = _connectionViews[i];
            if (cv.FromNodeIndex == idx || cv.ToNodeIndex == idx)
            {
                Destroy(cv.gameObject);
                _connectionViews.RemoveAt(i);
            }
        }

        // Shift indices down past the deleted node
        foreach (var cv in _connectionViews) cv.UpdateIndices(idx);
        foreach (var nv in _nodeViews)       if (nv.NodeIndex > idx) nv.NodeIndex--;

        // Shift slot entries
        if (_workingGraph != null)
        {
            var entries = _workingGraph.slotEntries;
            for (int i = entries.Count - 1; i >= 0; i--)
            {
                var e = entries[i];
                if (e.nodeIndex == idx)
                    entries.RemoveAt(i);
                else if (e.nodeIndex > idx)
                    entries[i] = new SpellGraphSO.SlotEntry { slotIndex = e.slotIndex, nodeIndex = e.nodeIndex - 1 };
            }
        }

        _workingGraph?.nodes.RemoveAt(idx);
        _nodeViews.Remove(view);
        Destroy(view.gameObject);
        OnGraphModified?.Invoke();
    }

    // ── Connection Wiring ────────────────────────────────────────────────────

    public void GrabConnection(PortView port)
    {
        if (_pending == null) _pending = GetComponentInChildren<PendingConnectionController>(true);

        var existing = FindConnectionForPort(port);
        if (existing != null)
        {
            PortView outputPort = port.Type == PortView.PortType.Output
                ? port
                : GetNodeView(existing.FromNodeIndex)?.OutputPort;

            DeleteConnection(existing);
            if (outputPort != null) _pending?.StartFrom(outputPort);
        }
        else if (port.Type == PortView.PortType.Output)
        {
            _pending?.StartFrom(port);
        }
    }

    public void DeleteConnection(ConnectionView cv)
    {
        _connectionViews.Remove(cv);
        Destroy(cv.gameObject);
        OnGraphModified?.Invoke();
    }

    public void CompleteConnection(PortView inputPort)
    {
        if (_pending == null || !_pending.IsActive) return;
        var from = _pending.SourcePort;
        _pending.Cancel();

        if (from.OwnerNodeView == inputPort.OwnerNodeView) return;
        if (ConnectionExists(from.OwnerNodeView.NodeIndex, inputPort.OwnerNodeView.NodeIndex)) return;

        SpawnConnectionView(from, inputPort);
    }

    public void CompleteLauncherConnection(LauncherPortView launcherPort, NodeView targetNode)
    {
        if (_workingGraph == null) return;
        _workingGraph.SetSlotEntry(launcherPort.SlotIndex, targetNode.NodeIndex);
        OnGraphModified?.Invoke();
    }

    // ── Port Queries ─────────────────────────────────────────────────────────

    public RectTransform GetNodeInputPortRT(int nodeIndex)
    {
        var node = GetNodeView(nodeIndex);
        return node?.InputPort != null ? node.InputPort.GetComponent<RectTransform>() : null;
    }

    // ── Helpers ──────────────────────────────────────────────────────────────

    // Simple grid layout: nodes fill left-to-right, then wrap to next row
    private static Vector2 GridPosition(int nodeIndex)
    {
        const int cols = 4;
        int col = nodeIndex % cols;
        int row = nodeIndex / cols;
        return new Vector2(ROW_START_X + col * NODE_SPACING, ROW_START_Y - row * ROW_HEIGHT);
    }

    private NodeView SpawnNodeView(SpellNodeSO data, int nodeIndex)
    {
        var go   = Instantiate(NodeViewPrefab, GraphArea);
        go.SetActive(true);
        var view = go.GetComponent<NodeView>();
        view.NodeIndex = nodeIndex;
        view.Init(data, this);
        _nodeViews.Add(view);
        return view;
    }

    private void SpawnConnectionView(PortView from, PortView to)
    {
        var go   = Instantiate(ConnectionViewPrefab, GraphArea);
        go.SetActive(true);
        var conn = go.GetComponent<ConnectionView>();
        conn.Init(from, to, GraphArea);
        _connectionViews.Add(conn);
        go.transform.SetSiblingIndex(0);
        OnGraphModified?.Invoke();
    }

    private ConnectionView FindConnectionForPort(PortView port)
    {
        int idx = port.OwnerNodeView.NodeIndex;
        foreach (var cv in _connectionViews)
        {
            if (port.Type == PortView.PortType.Output && cv.FromNodeIndex == idx) return cv;
            if (port.Type == PortView.PortType.Input  && cv.ToNodeIndex   == idx) return cv;
        }
        return null;
    }

    private bool ConnectionExists(int fromIdx, int toIdx)
    {
        foreach (var cv in _connectionViews)
            if (cv.FromNodeIndex == fromIdx && cv.ToNodeIndex == toIdx) return true;
        return false;
    }

    private NodeView GetNodeView(int nodeIndex)
    {
        foreach (var nv in _nodeViews)
            if (nv.NodeIndex == nodeIndex) return nv;
        return null;
    }
}
