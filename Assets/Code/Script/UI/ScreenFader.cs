using System.Collections;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.UI;

/// Écran noir persistant qui habille tous les changements de scène : fade vers le noir
/// avant le chargement, un hold supplémentaire pour Gameplay (le temps que la caméra et
/// le décor additif de LevelDirector s'installent), puis fade depuis le noir une fois prêt.
/// S'auto-instancie au tout premier lancement (RuntimeInitializeOnLoadMethod) — aucun
/// GameObject à placer dans les scènes, aucune référence Inspector à câbler.
public class ScreenFader : MonoBehaviour
{
    public static ScreenFader Instance { get; private set; }

    private const float FadeDuration        = 0.4f;
    private const float GameplayHoldSeconds = 1.5f;
    private const int   SortingOrder        = 1000;

    private Image _fadeImage;

    [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.BeforeSceneLoad)]
    private static void Bootstrap()
    {
        if (Instance != null) return;
        new GameObject("ScreenFader").AddComponent<ScreenFader>();
    }

    private void Awake()
    {
        if (Instance != null && Instance != this) { Destroy(gameObject); return; }
        Instance = this;
        DontDestroyOnLoad(gameObject);
        BuildUI();
    }

    // Point d'entrée utilisé à la place de SceneManager.LoadScene partout où un changement
    // de scène doit être habillé par le fade (menu, run, changement de niveau...).
    public static void LoadScene(string sceneName)
    {
        if (Instance != null)
            Instance.StartCoroutine(Instance.LoadSceneRoutine(sceneName));
        else
            SceneManager.LoadScene(sceneName);
    }

    private IEnumerator LoadSceneRoutine(string sceneName)
    {
        yield return Fade(1f);

        SceneManager.LoadScene(sceneName);

        if (sceneName == "Gameplay")
            yield return new WaitForSecondsRealtime(GameplayHoldSeconds);

        yield return Fade(0f);
    }

    private IEnumerator Fade(float targetAlpha)
    {
        float startAlpha = _fadeImage.color.a;
        float t = 0f;

        _fadeImage.raycastTarget = true;

        while (t < FadeDuration)
        {
            t += Time.unscaledDeltaTime;
            SetAlpha(Mathf.Lerp(startAlpha, targetAlpha, t / FadeDuration));
            yield return null;
        }

        SetAlpha(targetAlpha);
        _fadeImage.raycastTarget = targetAlpha > 0.01f;
    }

    private void SetAlpha(float a)
    {
        var c = _fadeImage.color;
        _fadeImage.color = new Color(c.r, c.g, c.b, a);
    }

    private void BuildUI()
    {
        var canvasGO = new GameObject("Canvas");
        canvasGO.transform.SetParent(transform, false);

        var canvas = canvasGO.AddComponent<Canvas>();
        canvas.renderMode   = RenderMode.ScreenSpaceOverlay;
        canvas.sortingOrder = SortingOrder;
        canvasGO.AddComponent<CanvasScaler>();
        canvasGO.AddComponent<GraphicRaycaster>();

        var imageGO = new GameObject("FadeImage");
        imageGO.transform.SetParent(canvasGO.transform, false);

        _fadeImage = imageGO.AddComponent<Image>();
        _fadeImage.color         = new Color(0f, 0f, 0f, 0f);
        _fadeImage.raycastTarget = false;

        var rt = _fadeImage.rectTransform;
        rt.anchorMin = Vector2.zero;
        rt.anchorMax = Vector2.one;
        rt.offsetMin = Vector2.zero;
        rt.offsetMax = Vector2.zero;
    }
}
