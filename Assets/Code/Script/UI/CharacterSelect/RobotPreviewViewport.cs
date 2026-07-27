using UnityEngine;
using UnityEngine.Rendering.Universal;
using UnityEngine.UI;

/// <summary>
/// Vignette 3D d'un robot dans une carte de CharacterSelect. Filme, via une caméra dédiée
/// sur le layer "RobotPreview", le mesh généré par RobotVisualGenerator posé sur un stage
/// hors-champ (sous la map, décalé en X selon previewSlotIndex) : toutes les cartes
/// partagent le même layer de rendu, donc c'est cet écart physique entre stages — pas le
/// layer — qui empêche une carte de filmer le robot d'une autre. Rendu vers un
/// RenderTexture affiché dans un RawImage, en pose fixe (pas de rotation automatique).
/// </summary>
public class RobotPreviewViewport : MonoBehaviour
{
    private const string _PREVIEW_LAYER_NAME = "RobotPreview";
    private const float _SLOT_SPACING = 12f;
    private const float _STAGE_BASE_Y = -500f;

    [SerializeField] private RawImage _previewImage;
    [SerializeField] private int _textureSize = 256;
    [SerializeField] private float _cameraDistance = 2f;
    [SerializeField] private float _cameraHeight = 1.58f;
    [SerializeField] private float _lookAtHeight = 1.55f;
    [SerializeField] private float _fieldOfView = 28f;

    private Camera _camera;
    private RenderTexture _renderTexture;
    private Transform _stage;
    private GameObject _visualInstance;
    private bool _initialized;

    public void Initialize(int previewSlotIndex)
    {
        if (_initialized)
        {
            return;
        }

        _initialized = true;

        int previewLayer = LayerMask.NameToLayer(_PREVIEW_LAYER_NAME);

        GameObject stageGO = new GameObject($"RobotPreviewStage_{previewSlotIndex}");
        stageGO.transform.position = new Vector3(previewSlotIndex * _SLOT_SPACING, _STAGE_BASE_Y, 0f);
        _stage = stageGO.transform;

        GameObject cameraGO = new GameObject("PreviewCamera");
        cameraGO.transform.SetParent(_stage, false);
        cameraGO.transform.localPosition = new Vector3(0f, _cameraHeight, _cameraDistance);
        Vector3 lookTarget = new Vector3(0f, _lookAtHeight, 0f);
        cameraGO.transform.localRotation = Quaternion.LookRotation((lookTarget - cameraGO.transform.localPosition).normalized);
        cameraGO.layer = Mathf.Max(previewLayer, 0);

        _camera = cameraGO.AddComponent<Camera>();
        _camera.clearFlags = CameraClearFlags.SolidColor;
        _camera.backgroundColor = new Color(0f, 0f, 0f, 0f);
        _camera.cullingMask = previewLayer >= 0 ? (1 << previewLayer) : 0;
        _camera.fieldOfView = _fieldOfView;
        _camera.nearClipPlane = 0.1f;
        _camera.farClipPlane = 10f;

        // Sans ça, le post-processing (Bloom) reste désactivé par défaut sur une caméra créée
        // par script en URP — nécessaire pour que le glow émissif des yeux des robots produise
        // un vrai halo au lieu d'une simple couleur vive.
        UniversalAdditionalCameraData cameraData = cameraGO.AddComponent<UniversalAdditionalCameraData>();
        cameraData.renderPostProcessing = true;

        // Format ARGB32 explicite : sans lui, certaines plateformes créent la RenderTexture
        // sans canal alpha et le fond transparent de la caméra devient un fond noir opaque.
        _renderTexture = new RenderTexture(_textureSize, _textureSize, 16, RenderTextureFormat.ARGB32)
        {
            antiAliasing = 2
        };
        _camera.targetTexture = _renderTexture;

        if (_previewImage != null)
        {
            _previewImage.texture = _renderTexture;
            _previewImage.color = Color.white;
        }
    }

    public void ShowRobot(RobotVisualRecipe recipe)
    {
        if (_visualInstance != null)
        {
            DestroyGameObject(_visualInstance);
            _visualInstance = null;
        }

        if (recipe == null || _stage == null)
        {
            return;
        }

        _visualInstance = RobotVisualGenerator.BuildVisual(recipe);
        if (_visualInstance == null)
        {
            return;
        }

        _visualInstance.transform.SetParent(_stage, false);
        _visualInstance.transform.localPosition = Vector3.zero;
        _visualInstance.transform.localRotation = Quaternion.identity;
        SetLayerRecursively(_visualInstance, _camera.gameObject.layer);
    }

    private static void SetLayerRecursively(GameObject go, int layer)
    {
        go.layer = layer;
        foreach (Transform child in go.transform)
        {
            SetLayerRecursively(child.gameObject, layer);
        }
    }

    private void OnDestroy()
    {
        if (_camera != null)
        {
            _camera.targetTexture = null;
        }

        if (_renderTexture != null)
        {
            _renderTexture.Release();
            DestroyGameObject(_renderTexture);
        }

        if (_stage != null)
        {
            DestroyGameObject(_stage.gameObject);
        }
    }

    // Destroy() ne fait rien hors Play Mode (juste un warning) : le stage (root de scène, pas
    // enfant de la carte) ne serait alors jamais nettoyé si on testait ce composant depuis l'éditeur.
    private static void DestroyGameObject(Object obj)
    {
        if (Application.isPlaying)
        {
            Destroy(obj);
        }
        else
        {
            DestroyImmediate(obj);
        }
    }
}
