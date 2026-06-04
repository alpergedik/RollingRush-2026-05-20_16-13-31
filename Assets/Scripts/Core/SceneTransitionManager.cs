using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.UI;
using DG.Tweening;
using System.Collections;

public class SceneTransitionManager : MonoBehaviour
{
    public static SceneTransitionManager Instance { get; private set; }

    [Header("UI References")]
    [SerializeField] private Canvas transitionCanvas;
    [SerializeField] private RectTransform circleRect;
    [SerializeField] private Image circleImage;

    [Header("Transition Settings")]
    [SerializeField] private float coverDuration = 0.45f;
    [SerializeField] private float revealDuration = 0.45f;
    [SerializeField] private Ease coverEase = Ease.InCubic;
    [SerializeField] private Ease revealEase = Ease.OutCubic;
    [SerializeField] private float extraCoverMultiplier = 1.15f;

    private bool isTransitioning;
    private bool isFirstLoad = true;

    private void Awake()
    {
        if (Instance != null && Instance != this)
        {
            Destroy(gameObject);
            return;
        }

        Instance = this;
        DontDestroyOnLoad(gameObject);
    }

    private void Start()
    {
        if (isFirstLoad)
        {
            isFirstLoad = false;
            StartCoroutine(RevealSceneRoutine(true));
        }
    }

    private void OnEnable()
    {
        SceneManager.sceneLoaded += OnSceneLoaded;
    }

    private void OnDisable()
    {
        SceneManager.sceneLoaded -= OnSceneLoaded;
    }

    private void OnDestroy()
    {
        circleRect?.DOKill();
    }

    private void OnSceneLoaded(Scene scene, LoadSceneMode mode)
    {
        if (isFirstLoad)
        {
            return; // Handled in Start()
        }

        StartCoroutine(RevealSceneRoutine(false));
    }

    public void TransitionToScene(string sceneName, RectTransform sourceRect)
    {
        if (isTransitioning)
        {
            return;
        }

        StartCoroutine(TransitionRoutine(sceneName, sourceRect));
    }

    private IEnumerator TransitionRoutine(string sceneName, RectTransform sourceRect)
    {
        isTransitioning = true;

        circleRect.DOKill();

        if (circleImage != null)
        {
            circleImage.gameObject.SetActive(true);
        }

        circleRect.anchoredPosition = GetTransitionStartPosition(sourceRect);
        circleRect.localScale = Vector3.zero;

        float coverScale = CalculateCoverScale();

        Tween coverTween = circleRect
            .DOScale(Vector3.one * coverScale, coverDuration)
            .SetEase(coverEase)
            .SetUpdate(true);

        yield return coverTween.WaitForCompletion();

        // Load Scene
        Time.timeScale = 1f;
        SceneManager.LoadScene(sceneName);
    }

    private IEnumerator RevealSceneRoutine(bool isInitialLoad)
    {
        if (!isInitialLoad)
        {
            yield return null; // Wait 1 frame to ensure scene is loaded
        }

        circleRect.DOKill();

        if (circleImage != null)
        {
            circleImage.gameObject.SetActive(true);
        }
        
        circleRect.anchoredPosition = Vector2.zero;

        float coverScale = CalculateCoverScale();
        circleRect.localScale = Vector3.one * coverScale;

        Tween revealTween = circleRect
            .DOScale(Vector3.zero, revealDuration)
            .SetEase(revealEase)
            .SetUpdate(true);

        yield return revealTween.WaitForCompletion();

        if (circleImage != null)
        {
            circleImage.gameObject.SetActive(false);
        }
        
        isTransitioning = false;
    }

    private float CalculateCoverScale()
    {
        if (circleRect == null)
        {
            return 1f;
        }

        float screenDiagonal = Mathf.Sqrt(
            Screen.width * Screen.width +
            Screen.height * Screen.height
        );

        float baseDiameter = Mathf.Max(
            circleRect.rect.width,
            circleRect.rect.height
        );

        if (baseDiameter <= 0f)
        {
            baseDiameter = 100f;
        }

        return (screenDiagonal / baseDiameter) * extraCoverMultiplier;
    }

    private Vector2 GetTransitionStartPosition(RectTransform sourceRect)
    {
        if (sourceRect == null || transitionCanvas == null)
        {
            return Vector2.zero;
        }

        RectTransform canvasRect = transitionCanvas.transform as RectTransform;
        
        // Ensure a canvas overlay exists for correct mapping
        if (transitionCanvas.renderMode == RenderMode.ScreenSpaceOverlay)
        {
            Vector2 screenPoint = RectTransformUtility.WorldToScreenPoint(null, sourceRect.position);
            RectTransformUtility.ScreenPointToLocalPointInRectangle(
                canvasRect,
                screenPoint,
                null,
                out Vector2 localPoint
            );
            return localPoint;
        }

        return Vector2.zero;
    }
}
