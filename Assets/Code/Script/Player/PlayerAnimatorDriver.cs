using UnityEngine;

/// <summary>
/// Pilote l'Animator du robot visuel (enfant dynamique instancié par RobotLoader.ApplyVisual,
/// recréé à chaque changement de robot) à partir de l'input de PlayerController. Le mouvement
/// est en espace monde (WASD absolu, voir PlayerController.Update) alors que le Player tourne
/// pour faire face au curseur (PlayerRotation) : on reprojette donc l'input en espace local du
/// Player pour que le blend tree (MoveX/MoveZ, AC_Robot) joue le clip Sidekick correspondant à
/// la direction relative à l'orientation du personnage plutôt qu'au monde.
/// </summary>
[RequireComponent(typeof(PlayerController))]
public class PlayerAnimatorDriver : MonoBehaviour
{
    private static readonly int _MoveXHash = Animator.StringToHash("MoveX");
    private static readonly int _MoveZHash = Animator.StringToHash("MoveZ");
    private static readonly int _SpeedHash = Animator.StringToHash("Speed");

    private PlayerController _playerController;
    private Animator _animator;

    private void Awake()
    {
        _playerController = GetComponent<PlayerController>();
    }

    private void Update()
    {
        // Recherché à chaque frame où la référence est manquante plutôt que caché une seule
        // fois : RobotLoader détruit et recrée le visuel (donc son Animator) à chaque
        // changement de robot, et Unity renvoie bien "null" pour une référence détruite.
        if (_animator == null)
        {
            _animator = GetComponentInChildren<Animator>();
            if (_animator == null) return;
        }

        Vector2 moveInput = _playerController.MoveInput;
        Vector3 localMove = transform.InverseTransformDirection(new Vector3(moveInput.x, 0f, moveInput.y));

        _animator.SetFloat(_MoveXHash, localMove.x);
        _animator.SetFloat(_MoveZHash, localMove.z);
        _animator.SetFloat(_SpeedHash, moveInput.magnitude);
    }
}
