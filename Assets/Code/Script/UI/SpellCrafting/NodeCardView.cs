using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;

/// A card in the inventory panel. Clicking it adds that node to the canvas.
public class NodeCardView : MonoBehaviour, IPointerClickHandler
{
    private SpellNodeSO _data;

    public void Init(SpellNodeSO data)
    {
        _data = data;
        var bg = GetComponent<Image>();
        if (bg) bg.color = NodeView.ColorForType(data.nodeType) * 0.7f;

        var labels = GetComponentsInChildren<Text>();
        if (labels.Length > 0) labels[0].text = data.nodeName;
        if (labels.Length > 1) labels[1].text = $"Cost: {data.complexityCost}";
    }

    public void OnPointerClick(PointerEventData e)
    {
        GraphCanvasController.Instance?.AddNode(_data);
    }
}
