using UnityEngine;
using UnityEngine.SceneManagement;

[RequireComponent(typeof(Collider))]
public class ZonePortal : MonoBehaviour
{
    [Header("Scène suivante")]
    [SerializeField] private string _nextSceneName;

    [Header("Visuals")]
    [SerializeField] private GameObject _mesh;

    private Collider _collider;

    private void Awake()
    {
        _collider           = GetComponent<Collider>();
        _collider.isTrigger = true;
        _collider.enabled   = false;

        if (_mesh != null)
            _mesh.SetActive(false);
    }

    // Appeler depuis OnZoneCompleted du ZoneManager (via l'Inspector)
    public void Open()
    {
        _collider.enabled = true;

        if (_mesh != null)
            _mesh.SetActive(true);
    }

    private void OnTriggerEnter(Collider other)
    {
        if (!other.CompareTag("Player")) return;
        if (string.IsNullOrEmpty(_nextSceneName)) return;

        SceneManager.LoadScene(_nextSceneName);
    }
}
