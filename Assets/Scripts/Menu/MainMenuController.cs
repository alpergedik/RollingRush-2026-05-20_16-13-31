using DG.Tweening;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.UI;

public class MainMenuController : MonoBehaviour
{
    [Header("Scene Names")]
    [SerializeField] private string gameSceneName = "GameScene";

    [Header("Panels")]
    [SerializeField] private GameObject settingsMenuPanel;
    [SerializeField] private GameObject difficultyMenuPanel;
    [SerializeField] private Image darkOverlay;

    [Header("Buttons")]
    [SerializeField] private Button playButton;
    [SerializeField] private Button settingsButton;
    [SerializeField] private Button closeSettingsButton;
    [SerializeField] private Button muteButton;
    [SerializeField] private Button unmuteButton;

    [Header("Difficulty Buttons")]
    [SerializeField] private Button easyButton;
    [SerializeField] private Button normalButton;
    [SerializeField] private Button hardButton;
    [SerializeField] private Button closeDifficultyButton;

    [Header("Tween Settings")]
    [SerializeField] private float buttonPressScale = 0.9f;
    [SerializeField] private float buttonPressDuration = 0.08f;
    [SerializeField] private float panelOpenDuration = 0.28f;
    [SerializeField] private float panelCloseDuration = 0.2f;
    [SerializeField] private float overlayFadeDuration = 0.2f;
    [SerializeField] private float overlayMaxAlpha = 0.55f;

    private enum OpenMenuPanel
    {
        None,
        Settings,
        Difficulty
    }

    private OpenMenuPanel openMenuPanel = OpenMenuPanel.None;

    private RectTransform settingsPanelRect;
    private RectTransform difficultyPanelRect;

    private void Awake()
    {
        if (settingsMenuPanel != null)
        {
            settingsPanelRect = settingsMenuPanel.GetComponent<RectTransform>();
            settingsMenuPanel.SetActive(false);

            if (settingsPanelRect != null)
            {
                settingsPanelRect.localScale = Vector3.zero;
            }
        }

        if (difficultyMenuPanel != null)
        {
            difficultyPanelRect = difficultyMenuPanel.GetComponent<RectTransform>();
            difficultyMenuPanel.SetActive(false);

            if (difficultyPanelRect != null)
            {
                difficultyPanelRect.localScale = Vector3.zero;
            }
        }

        if (darkOverlay != null)
        {
            darkOverlay.gameObject.SetActive(false);
            SetOverlayAlpha(0f);
        }
    }

    private void Start()
    {
        ApplySoundState();
        SoundManager.Instance?.PlayMusic();

        if (playButton != null)
        {
            playButton.onClick.AddListener(PlayButtonPressed);
        }

        if (settingsButton != null)
        {
            settingsButton.onClick.AddListener(SettingsButtonPressed);
        }

        if (closeSettingsButton != null)
        {
            closeSettingsButton.onClick.AddListener(CloseSettingsButtonPressed);
        }

        if (muteButton != null)
        {
            muteButton.onClick.AddListener(MuteButtonPressed);
        }

        if (unmuteButton != null)
        {
            unmuteButton.onClick.AddListener(UnmuteButtonPressed);
        }

        easyButton?.onClick.AddListener(EasyButtonPressed);
        normalButton?.onClick.AddListener(NormalButtonPressed);
        hardButton?.onClick.AddListener(HardButtonPressed);
        closeDifficultyButton?.onClick.AddListener(CloseDifficultyButtonPressed);
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
    
    private void PlayButtonPressed()
    {
        SoundManager.Instance?.PlayButton();
        if (playButton != null)
        {
            AnimateButton(playButton.transform, OpenDifficulty);
        }
        else
        {
            OpenDifficulty();
        }
    }

    private void EasyButtonPressed()
    {
        SelectDifficultyAndPlay(DifficultyProfile.Easy, easyButton);
    }

    private void NormalButtonPressed()
    {
        SelectDifficultyAndPlay(DifficultyProfile.Normal, normalButton);
    }

    private void HardButtonPressed()
    {
        SelectDifficultyAndPlay(DifficultyProfile.Hard, hardButton);
    }

    private void CloseDifficultyButtonPressed()
    {
        SoundManager.Instance?.PlayButton();
        if (closeDifficultyButton != null)
        {
            AnimateButton(closeDifficultyButton.transform, CloseDifficulty);
        }
        else
        {
            CloseDifficulty();
        }
    }

    private void SelectDifficultyAndPlay(DifficultyProfile difficulty, Button selectedButton)
    {
        DifficultySelection.SetSelectedDifficulty(difficulty);
        SoundManager.Instance?.PlayButton();

        if (selectedButton != null)
        {
            AnimateButton(selectedButton.transform, () => PlayGame(selectedButton));
        }
        else
        {
            PlayGame(null);
        }
    }

    private void SettingsButtonPressed()
    {
        SoundManager.Instance?.PlayButton();
        if (settingsButton != null)
        {
            AnimateButton(settingsButton.transform, OpenSettings);
        }
        else
        {
            OpenSettings();
        }
    }

    private void CloseSettingsButtonPressed()
    {
        SoundManager.Instance?.PlayButton();
        if (closeSettingsButton != null)
        {
            AnimateButton(closeSettingsButton.transform, CloseSettings);
        }
        else
        {
            CloseSettings();
        }
    }

    public void PlayGame(Button sourceButton = null)
    {
        if (SceneTransitionManager.Instance != null)
        {
            SceneTransitionManager.Instance.TransitionToScene(gameSceneName, sourceButton != null ? sourceButton.transform as RectTransform : null);
        }
        else
        {
            Time.timeScale = 1f;
            SceneManager.LoadScene(gameSceneName);
        }
    }

    public void OpenSettings()
    {
        if (settingsMenuPanel == null || settingsPanelRect == null)
        {
            return;
        }

        if (openMenuPanel != OpenMenuPanel.None)
        {
            return;
        }

        openMenuPanel = OpenMenuPanel.Settings;

        settingsPanelRect.DOKill();

        if (darkOverlay != null)
        {
            darkOverlay.DOKill();
            darkOverlay.gameObject.SetActive(true);
            SetOverlayAlpha(0f);

            darkOverlay
                .DOFade(overlayMaxAlpha, overlayFadeDuration)
                .SetEase(Ease.OutQuad);
        }

        settingsMenuPanel.SetActive(true);
        settingsPanelRect.localScale = Vector3.zero;

        settingsPanelRect
            .DOScale(Vector3.one, panelOpenDuration)
            .SetEase(Ease.OutBack);
    }

    public void CloseSettings()
    {
        if (settingsMenuPanel == null || settingsPanelRect == null)
        {
            return;
        }

        if (openMenuPanel != OpenMenuPanel.Settings)
        {
            return;
        }

        openMenuPanel = OpenMenuPanel.None;

        settingsPanelRect.DOKill();

        settingsPanelRect
            .DOScale(Vector3.zero, panelCloseDuration)
            .SetEase(Ease.InBack)
            .OnComplete(() =>
            {
                settingsMenuPanel.SetActive(false);
            });

        if (darkOverlay != null)
        {
            darkOverlay.DOKill();

            darkOverlay
                .DOFade(0f, overlayFadeDuration)
                .SetEase(Ease.OutQuad)
                .OnComplete(() =>
                {
                    darkOverlay.gameObject.SetActive(false);
                });
        }
    }

    public void OpenDifficulty()
    {
        if (difficultyMenuPanel == null || difficultyPanelRect == null)
        {
            return;
        }

        if (openMenuPanel != OpenMenuPanel.None)
        {
            return;
        }

        openMenuPanel = OpenMenuPanel.Difficulty;

        difficultyPanelRect.DOKill();

        if (darkOverlay != null)
        {
            darkOverlay.DOKill();
            darkOverlay.gameObject.SetActive(true);
            SetOverlayAlpha(0f);

            darkOverlay
                .DOFade(overlayMaxAlpha, overlayFadeDuration)
                .SetEase(Ease.OutQuad);
        }

        difficultyMenuPanel.SetActive(true);
        difficultyPanelRect.localScale = Vector3.zero;

        difficultyPanelRect
            .DOScale(Vector3.one, panelOpenDuration)
            .SetEase(Ease.OutBack);
    }

    public void CloseDifficulty()
    {
        if (difficultyMenuPanel == null || difficultyPanelRect == null)
        {
            return;
        }

        if (openMenuPanel != OpenMenuPanel.Difficulty)
        {
            return;
        }

        openMenuPanel = OpenMenuPanel.None;

        difficultyPanelRect.DOKill();

        difficultyPanelRect
            .DOScale(Vector3.zero, panelCloseDuration)
            .SetEase(Ease.InBack)
            .OnComplete(() =>
            {
                difficultyMenuPanel.SetActive(false);
            });

        if (darkOverlay != null)
        {
            darkOverlay.DOKill();

            darkOverlay
                .DOFade(0f, overlayFadeDuration)
                .SetEase(Ease.OutQuad)
                .OnComplete(() =>
                {
                    darkOverlay.gameObject.SetActive(false);
                });
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
            .OnComplete(() =>
            {
                buttonTransform
                    .DOScale(originalScale, buttonPressDuration)
                    .SetEase(Ease.OutBack)
                    .OnComplete(onComplete);
            });
    }

    private void SetOverlayAlpha(float alpha)
    {
        if (darkOverlay == null)
        {
            return;
        }

        Color color = darkOverlay.color;
        color.a = alpha;
        darkOverlay.color = color;
    }
    private void DarkOverlayPressed()
    {
        switch (openMenuPanel)
        {
            case OpenMenuPanel.Settings:
                CloseSettings();
                break;

            case OpenMenuPanel.Difficulty:
                CloseDifficulty();
                break;
        }
    }
}