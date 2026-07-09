using System.Collections.Generic;
using UnityEngine;
using UnityEngine.InputSystem;

[RequireComponent(typeof(CharacterController))]
public class PlayerController : MonoBehaviour
{
    [SerializeField] private float moveSpeed = 5f;

    private InputSystem_Actions _inputActions;
    private CharacterController _characterController;
    private Vector2 _moveInput;

    // Stacked multiplicatively, keyed by the source modifier so each contributor
    // can be added/removed independently (e.g. corrupted nodes wired/unwired
    // from a slot) without stomping on other active sources.
    private readonly Dictionary<object, float> _speedMultipliers = new();

    public void SetSpeedMultiplier(object source, float multiplier) => _speedMultipliers[source] = multiplier;
    public void ClearSpeedMultiplier(object source) => _speedMultipliers.Remove(source);
    public void SetBaseMoveSpeed(float value) => moveSpeed = value;

    private float AggregateSpeedMultiplier()
    {
        float result = 1f;
        foreach (var m in _speedMultipliers.Values) result *= m;
        return result;
    }

    private void Awake()
    {
        _inputActions = new InputSystem_Actions();
        _characterController = GetComponent<CharacterController>();
    }

    private void OnEnable()
    {
        _inputActions.Player.Move.performed += OnMove;
        _inputActions.Player.Move.canceled += OnMove;
        _inputActions.Player.Enable();
    }

    private void OnDisable()
    {
        _inputActions.Player.Move.performed -= OnMove;
        _inputActions.Player.Move.canceled -= OnMove;
        _inputActions.Player.Disable();
    }

    private void OnMove(InputAction.CallbackContext context)
    {
        _moveInput = context.ReadValue<Vector2>();
    }

    private void Update()
    {
        Vector3 direction = new Vector3(_moveInput.x, 0f, _moveInput.y);
        _characterController.SimpleMove(direction * moveSpeed * AggregateSpeedMultiplier());
    }
}
