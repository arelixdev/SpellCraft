using UnityEngine;
using UnityEngine.InputSystem;

/// Sits on the Canvas root (always active).
/// Tab key toggles the crafting panel and pauses/unpauses the game.
[DefaultExecutionOrder(-100)]
public class SpellCraftingToggle : MonoBehaviour
{
    public static SpellCraftingToggle Instance { get; private set; }

    [SerializeField] private GameObject       _panelRoot;
    [SerializeField] private SpellCraftingPanel _panel;

    public bool IsOpen { get; private set; }

    private void Awake()
    {
        Instance = this;
        _panelRoot?.SetActive(false);
    }

    private void Update()
    {
        if (Keyboard.current?.tabKey.wasPressedThisFrame == true)
            SetOpen(!IsOpen);
    }

    public void SetOpen(bool open)
    {
        IsOpen = open;
        _panelRoot?.SetActive(open);
        Time.timeScale = open ? 0f : 1f;

        if (open)  _panel?.OnOpen();
        else        _panel?.OnClose();
    }
}
