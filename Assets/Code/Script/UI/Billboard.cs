using UnityEngine;

/// <summary>
/// Oriente ce Transform pour qu'il fasse toujours face à la caméra active (texte world-space, etc.).
/// </summary>
public class Billboard : MonoBehaviour
{
    private Camera _camera;

    private void LateUpdate()
    {
        if (_camera == null) _camera = Camera.main;
        if (_camera == null) return;

        transform.forward = transform.position - _camera.transform.position;
    }
}
