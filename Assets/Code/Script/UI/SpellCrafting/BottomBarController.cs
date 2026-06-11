using UnityEngine;
using UnityEngine.UI;

/// Bottom bar: slot selector buttons, budget display, Apply button.
public class BottomBarController : MonoBehaviour
{
    [Header("References")]
    public Text   BudgetLabel;
    public Slider BudgetSlider;
    public Button ApplyButton;

    private SpellCraftingPanel _panel;
    private Button[]           _slotButtons;
    private int                _selectedSlot;

    public void Init(SpellCraftingPanel panel, int slotCount)
    {
        _panel = panel;

        _slotButtons = new Button[slotCount];
        var slotContainer = transform.Find("SlotContainer");
        if (slotContainer != null)
            for (int i = 0; i < slotContainer.childCount && i < slotCount; i++)
            {
                int captured = i;
                _slotButtons[i] = slotContainer.GetChild(i).GetComponent<Button>();
                _slotButtons[i]?.onClick.AddListener(() => SelectSlot(captured));
            }

        ApplyButton?.onClick.AddListener(_panel.OnApply);
        SelectSlot(0);
    }

    public void RefreshBudget(int current, int max)
    {
        if (BudgetLabel  != null) BudgetLabel.text  = $"{current} / {max}";
        if (BudgetSlider != null)
        {
            BudgetSlider.maxValue = max;
            BudgetSlider.value    = current;
        }
    }

    private void SelectSlot(int index)
    {
        _selectedSlot = index;
        _panel.OnSlotChanged(index);

        for (int i = 0; i < _slotButtons.Length; i++)
        {
            var img = _slotButtons[i]?.GetComponent<Image>();
            if (img) img.color = (i == index) ? Color.white : new Color(0.6f, 0.6f, 0.6f);
        }
    }
}
