using DG.Tweening;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.UI;

public class PauseMenuController : MonoBehaviour
{
    [Header("Scene Names")]
    [SerializeField] private string homeSceneName = "MainMenuScene";

    [Header("Panels")]
    [SerializeField] private GameObject pauseMenuPanel;
    [SerializeField] private Image pauseDarkOverlay;

    [Header("Buttons")]
    [SerializeField] private Button pauseButton;
    [SerializeField] private Button muteButton;
    [SerializeField] private Button unmuteButton;
    [SerializeField] private Button homeButton;
    [SerializeField] private Button retryButton;
    [SerializeField] private Button continueButton;
    [SerializeField] private Button closeButton;

    [Header("Tween Settings")]
    [SerializeField] private float buttonPressScale = 0.9f;
    [SerializeField] private float buttonPressDuration = 0.08f;
    [SerializeField] private float panelOpenDuration = 0.28f;
    [SerializeField] private float panelCloseDuration = 0.2f;
    [SerializeField] private float overlayFadeDuration = 0.2f;
    [SerializeField] private float overlayMaxAlpha = 0.55f;

    private RectTransform pausePanelRect;
    private bool isPaused;

    private void Awake()
    {
        if (pauseMenuPanel != null)
        {
            pausePanelRect = pauseMenuPanel.GetComponent<RectTransform>();
            pauseMenuPanel.SetActive(false);

            if (pausePanelRect != null)
            {
                pausePanelRect.localScale = Vector3.zero;
            }
        }

        if (pauseDarkOverlay != null)
        {
            pauseDarkOverlay.gameObject.SetActive(false);
            SetOverlayAlpha(0f);
        }
    }

    private void Start()
    {
        ApplySoundState();

        if (pauseButton != null)
        {
            pauseButton.onClick.AddListener(PauseButtonPressed);
        }

        if (muteButton != null)
        {
            muteButton.onClick.AddListener(MuteButtonPressed);
        }

        if (unmuteButton != null)
        {
            unmuteButton.onClick.AddListener(UnmuteButtonPressed);
        }

        if (homeButton != null)
        {
            homeButton.onClick.AddListener(HomeButtonPressed);
        }

        if (retryButton != null)
        {
            retryButton.onClick.AddListener(RetryButtonPressed);
        }

        if (continueButton != null)
        {
            continueButton.onClick.AddListener(ContinueButtonPressed);
        }

        if (closeButton != null)
        {
            closeButton.onClick.AddListener(CloseButtonPressed);
        }
    }

    private void PauseButtonPressed()
    {
        SoundManager.Instance?.PlayButton();
        if (pauseButton != null)
        {
            AnimateButton(pauseButton.transform, OpenPauseMenu);
        }
        else
        {
            OpenPauseMenu();
        }
    }

    private void ContinueButtonPressed()
    {
        SoundManager.Instance?.PlayButton();
        if (continueButton != null)
        {
            AnimateButton(continueButton.transform, ClosePauseMenu);
        }
        else
        {
            ClosePauseMenu();
        }
    }

    private void CloseButtonPressed()
    {
        SoundManager.Instance?.PlayButton();
        if (closeButton != null)
        {
            AnimateButton(closeButton.transform, ClosePauseMenu);
        }
        else
        {
            ClosePauseMenu();
        }
    }

    private void HomeButtonPressed()
    {
        SoundManager.Instance?.PlayButton();
        if (homeButton != null)
        {
            AnimateButton(homeButton.transform, GoToHomePage);
        }
        else
        {
            GoToHomePage();
        }
    }

    private void RetryButtonPressed()
    {
        SoundManager.Instance?.PlayButton();
        if (retryButton != null)
        {
            AnimateButton(retryButton.transform, RetryGame);
        }
        else
        {
            RetryGame();
        }
    }

    private void MuteButtonPressed()
    {
        SoundManager.Instance?.PlayButton();
        if (muteButton != null)
        {
            AnimateButton(muteButton.transform, () =>
            {
                SoundManager.Instance?.SetMuted(true);
                ApplySoundState();
            });
        }
        else
        {
            SoundManager.Instance?.SetMuted(true);
            ApplySoundState();
        }
    }

    private void UnmuteButtonPressed()
    {
        if (unmuteButton != null)
        {
            AnimateButton(unmuteButton.transform, () =>
            {
                SoundManager.Instance?.SetMuted(false);
                SoundManager.Instance?.PlayButton();
                ApplySoundState();
            });
        }
        else
        {
            SoundManager.Instance?.SetMuted(false);
            SoundManager.Instance?.PlayButton();
            ApplySoundState();
        }
    }

    public void OpenPauseMenu()
    {
        if (pauseMenuPanel == null || pausePanelRect == null)
        {
            return;
        }

        if (isPaused)
        {
            return;
        }

        isPaused = true;
        Time.timeScale = 0f;

        pausePanelRect.DOKill();

        if (pauseDarkOverlay != null)
        {
            pauseDarkOverlay.DOKill();
            pauseDarkOverlay.gameObject.SetActive(true);
            SetOverlayAlpha(0f);

            pauseDarkOverlay
                .DOFade(overlayMaxAlpha, overlayFadeDuration)
                .SetEase(Ease.OutQuad)
                .SetUpdate(true);
        }

        pauseMenuPanel.SetActive(true);
        pausePanelRect.localScale = Vector3.zero;

        pausePanelRect
            .DOScale(Vector3.one, panelOpenDuration)
            .SetEase(Ease.OutBack)
            .SetUpdate(true);
    }

    public void ClosePauseMenu()
    {
        if (pauseMenuPanel == null || pausePanelRect == null)
        {
            return;
        }

        if (!isPaused)
        {
            return;
        }

        isPaused = false;

        pausePanelRect.DOKill();

        pausePanelRect
            .DOScale(Vector3.zero, panelCloseDuration)
            .SetEase(Ease.InBack)
            .SetUpdate(true)
            .OnComplete(() =>
            {
                pauseMenuPanel.SetActive(false);
                Time.timeScale = 1f;
            });

        if (pauseDarkOverlay != null)
        {
            pauseDarkOverlay.DOKill();

            pauseDarkOverlay
                .DOFade(0f, overlayFadeDuration)
                .SetEase(Ease.OutQuad)
                .SetUpdate(true)
                .OnComplete(() =>
                {
                    pauseDarkOverlay.gameObject.SetActive(false);
                });
        }
    }

    private void GoToHomePage()
    {
        if (SceneTransitionManager.Instance != null)
        {
            SceneTransitionManager.Instance.TransitionToScene(homeSceneName, homeButton != null ? homeButton.transform as RectTransform : null);
        }
        else
        {
            Time.timeScale = 1f;
            SceneManager.LoadScene(homeSceneName);
        }
    }

    private void RetryGame()
    {
        if (SceneTransitionManager.Instance != null)
        {
            SceneTransitionManager.Instance.TransitionToScene(SceneManager.GetActiveScene().name, retryButton != null ? retryButton.transform as RectTransform : null);
        }
        else
        {
            Time.timeScale = 1f;
            SceneManager.LoadScene(SceneManager.GetActiveScene().name);
        }
    }

    private void ApplySoundState()
    {
        bool isMuted = SoundManager.Instance != null && SoundManager.Instance.IsMuted;

        if (muteButton != null)
        {
            muteButton.gameObject.SetActive(!isMuted);
        }

        if (unmuteButton != null)
        {
            unmuteButton.gameObject.SetActive(isMuted);
        }
    }

    private void AnimateButton(Transform buttonTransform, TweenCallback onComplete = null)
    {
        if (buttonTransform == null)
        {
            onComplete?.Invoke();
            return;
        }

        buttonTransform.DOKill();

        Vector3 originalScale = Vector3.one;

        buttonTransform
            .DOScale(originalScale * buttonPressScale, buttonPressDuration)
            .SetEase(Ease.OutQuad)
            .SetUpdate(true)
            .OnComplete(() =>
            {
                buttonTransform
                    .DOScale(originalScale, buttonPressDuration)
                    .SetEase(Ease.OutBack)
                    .SetUpdate(true)
                    .OnComplete(onComplete);
            });
    }

    private void SetOverlayAlpha(float alpha)
    {
        if (pauseDarkOverlay == null)
        {
            return;
        }

        Color color = pauseDarkOverlay.color;
        color.a = alpha;
        pauseDarkOverlay.color = color;
    }
}