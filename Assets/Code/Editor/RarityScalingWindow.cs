using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using UnityEditor;
using UnityEngine;

// Semi-assisted rarity balancing tool: pick a "Commun" reference node, drag in its
// Rare/Epic/Boss/... siblings, and it pre-fills each numeric field with a suggested
// value (reference * ratio for that rarity). Nothing is written until you review and
// click Appliquer — every field stays individually editable/toggle-able first.
public class RarityScalingWindow : EditorWindow
{
    [MenuItem("Spell/Rarity Scaling Tool")]
    private static void Open() => GetWindow<RarityScalingWindow>("Rarity Scaling");

    private static readonly Dictionary<NodeRarity, float> DefaultRatios = new()
    {
        { NodeRarity.Commun,   1.0f },
        { NodeRarity.Rare,     1.3f },
        { NodeRarity.Epique,   1.6f },
        { NodeRarity.Boss,     2.0f },
        { NodeRarity.Corrompu, 1.0f }, // géré séparément via CorruptionModifier
    };

    private class TargetEntry
    {
        public SpellNodeSO          node;
        public float                ratio;
        public Dictionary<string, bool>  include = new();
        public Dictionary<string, float> value   = new();
        public bool                 foldout = true;
    }

    private SpellNodeSO         _reference;
    private readonly List<TargetEntry> _targets = new();
    private Vector2              _scroll;

    private void OnGUI()
    {
        EditorGUILayout.Space(4);
        EditorGUILayout.LabelField("Référence (variante Commun)", EditorStyles.boldLabel);
        var newRef = (SpellNodeSO)EditorGUILayout.ObjectField(_reference, typeof(SpellNodeSO), false);
        if (newRef != _reference)
        {
            _reference = newRef;
            _targets.Clear();
        }

        EditorGUILayout.Space(8);
        EditorGUILayout.LabelField("Variantes à mettre à l'échelle (Rare / Epique / Boss / ...)", EditorStyles.boldLabel);
        HandleDragAndDrop();

        if (_reference == null)
        {
            EditorGUILayout.HelpBox("Assigne d'abord la variante Commun de la famille (ex: Aura Common).", MessageType.Info);
            return;
        }

        _scroll = EditorGUILayout.BeginScrollView(_scroll);
        for (int i = _targets.Count - 1; i >= 0; i--)
            DrawTarget(_targets[i], i);
        EditorGUILayout.EndScrollView();

        EditorGUILayout.Space(8);
        using (new EditorGUI.DisabledScope(_targets.Count == 0))
        {
            if (GUILayout.Button($"Appliquer aux {_targets.Count} variante(s)", GUILayout.Height(32)))
                ApplyAll();
        }
    }

    private void HandleDragAndDrop()
    {
        var dropArea = GUILayoutUtility.GetRect(0, 40, GUILayout.ExpandWidth(true));
        GUI.Box(dropArea, "Glisse ici les assets Rare / Epique / Boss / ... de la même famille");

        var evt = Event.current;
        if (!dropArea.Contains(evt.mousePosition)) return;

        if (evt.type == EventType.DragUpdated)
        {
            DragAndDrop.visualMode = DragAndDropVisualMode.Copy;
            evt.Use();
        }
        else if (evt.type == EventType.DragPerform)
        {
            DragAndDrop.AcceptDrag();
            foreach (var obj in DragAndDrop.objectReferences)
            {
                if (obj is not SpellNodeSO node) continue;
                if (node == _reference) continue;
                if (_targets.Any(t => t.node == node)) continue;
                if (_reference != null && node.GetType() != _reference.GetType())
                {
                    Debug.LogWarning($"[RarityScalingTool] '{node.name}' ignoré — type différent de la référence ({node.GetType().Name} ≠ {_reference.GetType().Name}).");
                    continue;
                }

                float ratio = DefaultRatios.TryGetValue(node.rarity, out var r) ? r : 1f;
                var entry = new TargetEntry { node = node, ratio = ratio };
                RefreshSuggestions(entry);
                _targets.Add(entry);
            }
            evt.Use();
        }
    }

    private static bool HasEnableRoll(SpellNodeSO node)
    {
        var field = node.GetType().GetField("enableRoll", BindingFlags.Public | BindingFlags.Instance);
        return field != null && (bool)field.GetValue(node);
    }

    private static IEnumerable<FieldInfo> ScalableFields(SpellNodeSO reference) =>
        reference.GetType()
            .GetFields(BindingFlags.Public | BindingFlags.Instance)
            .Where(f => f.FieldType == typeof(float));

    private void RefreshSuggestions(TargetEntry entry)
    {
        foreach (var field in ScalableFields(_reference))
        {
            float baseValue = (float)field.GetValue(_reference);
            entry.value[field.Name]   = baseValue * entry.ratio;
            entry.include.TryAdd(field.Name, true);
        }
    }

    private void DrawTarget(TargetEntry entry, int index)
    {
        if (entry.node == null) { _targets.RemoveAt(index); return; }

        EditorGUILayout.BeginVertical(EditorStyles.helpBox);
        EditorGUILayout.BeginHorizontal();
        entry.foldout = EditorGUILayout.Foldout(entry.foldout, $"{entry.node.name}  ({entry.node.rarity})", true);
        GUILayout.FlexibleSpace();
        if (GUILayout.Button("Retirer", GUILayout.Width(60)))
        {
            _targets.RemoveAt(index);
            EditorGUILayout.EndHorizontal();
            EditorGUILayout.EndVertical();
            return;
        }
        EditorGUILayout.EndHorizontal();

        if (HasEnableRoll(entry.node))
            EditorGUILayout.HelpBox("Tirage (enableRoll) actif sur ce node — les valeurs de base seront ré-écrasées au lancement par RollValues(). Pense à aussi ajuster les Ranges si besoin.", MessageType.Warning);

        if (!entry.foldout) { EditorGUILayout.EndVertical(); return; }

        EditorGUI.BeginChangeCheck();
        float newRatio = EditorGUILayout.FloatField("Ratio suggéré", entry.ratio);
        if (EditorGUI.EndChangeCheck())
        {
            entry.ratio = newRatio;
            RefreshSuggestions(entry);
        }

        foreach (var field in ScalableFields(_reference))
        {
            float baseValue = (float)field.GetValue(_reference);
            EditorGUILayout.BeginHorizontal();
            entry.include[field.Name] = EditorGUILayout.Toggle(entry.include.GetValueOrDefault(field.Name, true), GUILayout.Width(18));
            EditorGUILayout.LabelField($"{field.Name} ({baseValue:0.###} →)", GUILayout.Width(180));
            entry.value[field.Name] = EditorGUILayout.FloatField(entry.value.GetValueOrDefault(field.Name, baseValue * entry.ratio));
            EditorGUILayout.EndHorizontal();
        }

        EditorGUILayout.EndVertical();
    }

    private void ApplyAll()
    {
        foreach (var entry in _targets)
        {
            if (entry.node == null) continue;

            var so = new SerializedObject(entry.node);
            foreach (var field in ScalableFields(_reference))
            {
                if (!entry.include.GetValueOrDefault(field.Name, true)) continue;

                var prop = so.FindProperty(field.Name);
                if (prop == null) continue;
                prop.floatValue = entry.value.GetValueOrDefault(field.Name, (float)field.GetValue(_reference) * entry.ratio);
            }
            so.ApplyModifiedProperties();
            EditorUtility.SetDirty(entry.node);
        }

        AssetDatabase.SaveAssets();
        Debug.Log($"[RarityScalingTool] Valeurs appliquées à {_targets.Count} variante(s).");
    }
}
