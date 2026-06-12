using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

/// Manages the node canvas: creates/destroys NodeViews, draws connections,
/// serialises back to a SpellGraphSO working copy.
public class GraphCanvasController : MonoBehaviour
{
    public static GraphCanvasController Instance { get; private set; }

    [Header("Prefabs (set by SpellCraftingUIBuilder)")]
    public GameObject NodeViewPrefab;
    public GameObject ConnectionViewPrefab;
    public GameObject PendingLinePrefab;

    [Header("References (set by SpellCraftingUIBuilder)")]
    public RectTransform GraphArea;

    private SpellGraphSO        _workingGraph;
    private List<NodeView>       _nodeViews       = new();
    private List<ConnectionView> _connectionViews = new();
    private PendingConnectionController _pending;

    private void Awake() => Instance = this;

    // ── Graph Load / Flush ───────────────────────────────────────────────

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

            var placement = graph.editorLayout.Find(p => p.nodeIndex == i);
            view.SetLocalPosition(placement.canvasPosition != Vector2.zero
                ? placement.canvasPosition
                : new Vector2(-300f + i * 160f, 0f));
        }

        foreach (var conn in graph.connections)
        {
            var from = GetNodeView(conn.fromIndex)?.OutputPort;
            var to   = GetNodeView(conn.toIndex)?.InputPort;
            if (from != null && to != null) SpawnConnectionView(from, to);
        }
    }

    public void FlushToGraph()
    {
        if (_workingGraph == null) return;

        _workingGraph.connections.Clear();
        _workingGraph.editorLayout.Clear();

        foreach (var cv in _connectionViews)
            _workingGraph.connections.Add(new SpellGraphSO.Connection
            {
                fromIndex = cv.FromNodeIndex,
                toIndex   = cv.ToNodeIndex
            });

        foreach (var nv in _nodeViews)
            _workingGraph.editorLayout.Add(new SpellGraphSO.NodePlacement
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

    // ── Node Management ──────────────────────────────────────────────────

    public void AddNode(SpellNodeSO data)
    {
        if (_workingGraph == null) return;
        _workingGraph.nodes.Add(data);
        var view = SpawnNodeView(data, _workingGraph.nodes.Count - 1);
        view.SetLocalPosition(new Vector2(Random.Range(-200f, 200f), Random.Range(-100f, 100f)));
    }

    public void DeleteNode(NodeView view)
    {
        int idx = view.NodeIndex;

        // Remove connections that reference this node
        for (int i = _connectionViews.Count - 1; i >= 0; i--)
        {
            var cv = _connectionViews[i];
            if (cv.FromNodeIndex == idx || cv.ToNodeIndex == idx)
            {
                Destroy(cv.gameObject);
                _connectionViews.RemoveAt(i);
            }
        }

        // Update indices on remaining connections and node views
        foreach (var cv in _connectionViews)  cv.UpdateIndices(idx);
        foreach (var nv in _nodeViews)
            if (nv.NodeIndex > idx) nv.NodeIndex--;

        _workingGraph?.nodes.RemoveAt(idx);
        _nodeViews.Remove(view);
        Destroy(view.gameObject);
    }

    // ── Connection Wiring ────────────────────────────────────────────────

    /// Détache la connexion existante sur ce port (s'il y en a une) et
    /// démarre un câble pendant depuis le port OUTPUT. Si aucune connexion
    /// n'existe, démarre un nouveau câble (ports OUTPUT seulement).
    public void GrabConnection(PortView port)
    {
        if (_pending == null) _pending = GetComponentInChildren<PendingConnectionController>(true);

        var existing = FindConnectionForPort(port);
        if (existing != null)
        {
            // Garder le port OUTPUT comme point d'ancrage du câble pendant
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
        // Clic sur un INPUT sans connexion : rien
    }

    public void BeginConnection(PortView outputPort)
    {
        if (_pending == null) _pending = GetComponentInChildren<PendingConnectionController>(true);
        _pending?.StartFrom(outputPort);
    }

    public void DeleteConnection(ConnectionView cv)
    {
        _connectionViews.Remove(cv);
        Destroy(cv.gameObject);
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

    public void CompleteConnection(PortView inputPort)
    {
        if (_pending == null || !_pending.IsActive) return;
        var from = _pending.SourcePort;
        _pending.Cancel();

        if (from.OwnerNodeView == inputPort.OwnerNodeView) return;
        if (ConnectionExists(from.OwnerNodeView.NodeIndex, inputPort.OwnerNodeView.NodeIndex)) return;

        SpawnConnectionView(from, inputPort);
    }

    // ── Helpers ──────────────────────────────────────────────────────────

    private NodeView SpawnNodeView(SpellNodeSO data, int index)
    {
        var go   = Instantiate(NodeViewPrefab, GraphArea);
        go.SetActive(true);
        var view = go.GetComponent<NodeView>();
        view.NodeIndex = index;
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

        // Keep cable GO behind nodes
        go.transform.SetSiblingIndex(0);
    }

    private bool ConnectionExists(int fromIdx, int toIdx)
    {
        foreach (var cv in _connectionViews)
            if (cv.FromNodeIndex == fromIdx && cv.ToNodeIndex == toIdx) return true;
        return false;
    }

    private NodeView GetNodeView(int index)
    {
        foreach (var nv in _nodeViews)
            if (nv.NodeIndex == index) return nv;
        return null;
    }

    public RectTransform GetNode0InputPortRT()
    {
        var node0 = GetNodeView(0);
        return node0?.InputPort != null
            ? node0.InputPort.GetComponent<RectTransform>()
            : null;
    }
}
