using UnityEngine;
using UnityEngine.SceneManagement;
using TMPro;
using System.Collections;
using UnityEngine.UI;
using DG.Tweening;

public class GameManager : MonoBehaviour
{
    public static GameManager Instance;

    [Header("Game State")]
    public bool isGameStarted = false;
    public bool isGameOver = false;

    [Header("Speed")]
    public float currentSpeed = 0f;

    [Header("Game Balance")]
    public GameBalanceConfig balanceConfig;
    
    // Cached at startup
    private BalanceSettings _cachedBalance;
    public BalanceSettings Balance 
    {
        get 
        {
            if (_cachedBalance == null)
            {
                _cachedBalance = balanceConfig != null ? balanceConfig.Current : new BalanceSettings();
            }
            return _cachedBalance;
        }
    }

    [Header("Score Settings")]
    [SerializeField] private float distanceScoreMultiplier = 5f;
    [SerializeField] private int partScoreValue = 150;

    [Header("Tutorial")]
    [SerializeField] private GameObject tutorialPanel;
    [SerializeField] private bool showTutorialOnStart = true;
    private bool isWaitingForTutorialInput;

    private const string HighScorePlayerPrefsKey = "RollingRush_HighScore";
    public int HighScore { get; private set; }

    public float distance = 0f;
    public int score = 0;
    public int collectedParts = 0;
    public int driftScore = 0;

    public int DistanceScore => Mathf.FloorToInt(distance * distanceScoreMultiplier);
    public int CollectibleScore => collectedParts * partScoreValue;
    public int TotalScore => DistanceScore + CollectibleScore + driftScore;

    [Header("Gameplay UI")]
    public GameObject scoreText;

    [Header("Game Over UI")]
    [SerializeField] private GameObject gameOverPanel;
    [SerializeField] private Image gameOverDarkOverlay;
    
    [Header("Game Over Dynamic Value UI")]
    [SerializeField] private TextMeshProUGUI gameOverDistanceValueText;
    [SerializeField] private TextMeshProUGUI gameOverDriftScoreValueText;
    [SerializeField] private TextMeshProUGUI gameOverCollectedPartsValueText;
    [SerializeField] private TextMeshProUGUI gameOverTotalScoreValueText;
    
    [Header("Game Over Buttons")]
    [SerializeField] private Button gameOverHomeButton;
    [SerializeField] private Button gameOverRetryButton;

    [Header("Scene Names")]
    [SerializeField] private string mainMenuSceneName = "MainMenuScene";

    [Header("Game Over Tween Settings")]
    [SerializeField] private float gameOverPanelOpenDuration = 0.28f;
    [SerializeField] private float gameOverOverlayFadeDuration = 0.2f;
    [SerializeField] private float gameOverOverlayMaxAlpha = 0.55f;
    [SerializeField] private float buttonPressScale = 0.9f;
    [SerializeField] private float buttonPressDuration = 0.08f;

    [Header("Game Over Animation Settings")]
    [SerializeField] private float distanceCountDuration = 0.5f;
    [SerializeField] private float driftScoreCountDuration = 0.7f;
    [SerializeField] private float collectedPartsCountDuration = 0.6f;
    [SerializeField] private float totalScoreCountDuration = 0.9f;

    [Header("Game Over Total Score Attention")]
    [SerializeField] private float totalScoreAttentionScale = 1.06f;
    [SerializeField] private float totalScoreAttentionDuration = 0.65f;

    [Header("Game Over High Score UI")]
    [SerializeField] private TextMeshProUGUI gameOverHighScoreValueText;
    [SerializeField] private TextMeshProUGUI newHighScoreText;

    [Header("New High Score Animation")]
    [SerializeField] private float newHighScoreScale = 1.15f;
    [SerializeField] private float newHighScoreAnimationDuration = 0.35f;
    [SerializeField] private Color newHighScoreColorA = Color.yellow;
    [SerializeField] private Color newHighScoreColorB = Color.white;

    private Sequence gameOverScoreSequence;
    private Tween totalScoreAttentionTween;
    private Tween newHighScoreTween;


    private void Awake()
    {
        if (balanceConfig == null)
        {
            Debug.LogError("GameBalanceConfig is not assigned on GameManager.");
            enabled = false;
            return;
        }

        balanceConfig.activeProfile = DifficultySelection.GetSelectedDifficulty(balanceConfig.activeProfile);

        Instance = this;

        Time.timeScale = 1f;

        isGameStarted = false;
        isGameOver = false;

        currentSpeed = 0f;
        distance = 0f;
        score = 0;
        driftScore = 0;
        collectedParts = 0;

        if (scoreText != null)
        {
            scoreText.SetActive(false);
        }

        if (newHighScoreText != null)
        {
            newHighScoreText.gameObject.SetActive(false);
        }

        if (gameOverPanel != null)
        {
            gameOverPanel.SetActive(false);
            gameOverPanel.transform.localScale = Vector3.zero;
        }

        if (gameOverDarkOverlay != null)
        {
            gameOverDarkOverlay.gameObject.SetActive(false);
            SetGameOverOverlayAlpha(0f);
        }

        LoadHighScore();
        UpdateScoreUI();
    }

    private void Start()
    {
        if (gameOverHomeButton != null)
        {
            gameOverHomeButton.onClick.AddListener(GameOverHomeButtonPressed);
        }

        if (gameOverRetryButton != null)
        {
            gameOverRetryButton.onClick.AddListener(GameOverRetryButtonPressed);
        }

        if (showTutorialOnStart && tutorialPanel != null)
        {
            tutorialPanel.SetActive(true);
            tutorialPanel.transform.localScale = Vector3.one;
            isWaitingForTutorialInput = true;
            isGameStarted = false;
            currentSpeed = 0f;
        }
        else
        {
            if (tutorialPanel != null)
            {
                tutorialPanel.SetActive(false);
            }
            StartCoroutine(StartGameAfterTutorial());
        }
    }

    private void Update()
    {
        if (!isGameStarted)
        {
            if (isWaitingForTutorialInput && Input.anyKeyDown)
            {
                isWaitingForTutorialInput = false;

                if (tutorialPanel != null)
                {
                    tutorialPanel.transform.DOKill();
                    tutorialPanel.transform
                        .DOScale(Vector3.zero, 0.25f)
                        .SetEase(Ease.InBack)
                        .SetUpdate(true)
                        .OnComplete(() =>
                        {
                            StartCoroutine(StartGameAfterTutorial());
                        });
                }
                else
                {
                    StartCoroutine(StartGameAfterTutorial());
                }
            }

            return;
        }

        if (isGameOver)
        {
            if (Input.GetKeyDown(KeyCode.R))
            {
                RestartGame();
            }

            return;
        }

        UpdateSpeed();
        distance += currentSpeed * Time.deltaTime;
        score = TotalScore;
        UpdateScoreUI();
    }

    private IEnumerator StartGameAfterTutorial()
    {
        if (tutorialPanel != null)
        {
            tutorialPanel.SetActive(false);
        }

        yield return null;

        StartGame();
    }

    private void StartGame()
    {
        isGameStarted = true;
        currentSpeed = Balance.defaultSpeed;

        if (scoreText != null)
        {
            scoreText.SetActive(true);
        }

        UpdateScoreUI();
    }

    private void UpdateSpeed()
    {
        float targetSpeed = Balance.defaultSpeed;

        if (Input.GetKey(KeyCode.W) || Input.GetKey(KeyCode.UpArrow))
        {
            targetSpeed = Balance.boostSpeed;
        }
        else if (Input.GetKey(KeyCode.S) || Input.GetKey(KeyCode.DownArrow))
        {
            targetSpeed = Balance.brakeSpeed;
        }

        currentSpeed = Mathf.MoveTowards(
            currentSpeed,
            targetSpeed,
            Balance.speedChangeRate * Time.deltaTime
        );
    }


    public void CollectPart()
    {
        if (!isGameStarted || isGameOver)
        {
            return;
        }

        collectedParts++;
        score = TotalScore;
        UpdateScoreUI();
    }

    public void AddDriftScore(int amount)
    {
        if (!isGameStarted || isGameOver)
        {
            return;
        }

        if (amount <= 0)
        {
            return;
        }

        driftScore += amount;
        score = TotalScore;
        UpdateScoreUI();
    }

    private void UpdateScoreUI()
    {
        if (scoreText == null)
        {
            return;
        }

        TMP_Text scoreTextTMP = scoreText.GetComponent<TMP_Text>();

        if (scoreTextTMP == null)
        {
            return;
        }

        scoreTextTMP.text =
            "Distance: " + Mathf.FloorToInt(distance) + " m\n" +
            "Score: " + TotalScore.ToString("N0") + "\n" +
            "Drift Score: " + driftScore.ToString("N0") + "\n" +
            "Parts: " + collectedParts.ToString("N0");
    }

    public void GameOver(float delayBeforeFreeze = 0f)
    {
        if (isGameOver)
        {
            return;
        }

        isGameOver = true;
        currentSpeed = 0f;
        score = TotalScore;

        if (delayBeforeFreeze > 0f)
        {
            StartCoroutine(ShowGameOverAfterDelay(delayBeforeFreeze));
        }
        else
        {
            ShowGameOverUI();
            Time.timeScale = 0f;
        }
    }

    private IEnumerator ShowGameOverAfterDelay(float delay)
    {
        yield return new WaitForSeconds(delay);

        ShowGameOverUI();
        Time.timeScale = 0f;
    }

    private void ShowGameOverUI()
    {
        if (scoreText != null)
        {
            scoreText.SetActive(false);
        }

        if (gameOverDarkOverlay != null)
        {
            gameOverDarkOverlay.DOKill();
            gameOverDarkOverlay.gameObject.SetActive(true);
            SetGameOverOverlayAlpha(0f);

            gameOverDarkOverlay
                .DOFade(gameOverOverlayMaxAlpha, gameOverOverlayFadeDuration)
                .SetEase(Ease.OutQuad)
                .SetUpdate(true);
        }

        if (gameOverPanel != null)
        {
            gameOverPanel.transform.DOKill();
            gameOverPanel.SetActive(true);
            gameOverPanel.transform.localScale = Vector3.zero;

            gameOverPanel.transform
                .DOScale(Vector3.one, gameOverPanelOpenDuration)
                .SetEase(Ease.OutBack)
                .SetUpdate(true);

            bool isNewHighScore = CheckAndSaveHighScore();

            UpdateHighScoreUI();

            if (newHighScoreText != null)
            {
                newHighScoreText.gameObject.SetActive(isNewHighScore);
            }

            PlayGameOverScoreAnimation();

            if (isNewHighScore)
            {
                PlayNewHighScoreAnimation();
            }
        }
    }

    private void GameOverHomeButtonPressed()
    {
        if (gameOverHomeButton != null)
        {
            AnimateButton(gameOverHomeButton.transform, GoToHomePage);
        }
        else
        {
            GoToHomePage();
        }
    }

    private void GameOverRetryButtonPressed()
    {
        if (gameOverRetryButton != null)
        {
            AnimateButton(gameOverRetryButton.transform, RestartGame);
        }
        else
        {
            RestartGame();
        }
    }

    private void GoToHomePage()
    {
        gameOverScoreSequence?.Kill();
        totalScoreAttentionTween?.Kill();
        newHighScoreTween?.Kill();
        newHighScoreText?.transform.DOKill();
        newHighScoreText?.DOKill();

        if (SceneTransitionManager.Instance != null)
        {
            SceneTransitionManager.Instance.TransitionToScene(mainMenuSceneName, gameOverHomeButton != null ? gameOverHomeButton.transform as RectTransform : null);
        }
        else
        {
            Time.timeScale = 1f;
            SceneManager.LoadScene(mainMenuSceneName);
        }
    }

    private void RestartGame()
    {
        gameOverScoreSequence?.Kill();
        totalScoreAttentionTween?.Kill();
        newHighScoreTween?.Kill();
        newHighScoreText?.transform.DOKill();
        newHighScoreText?.DOKill();

        if (SceneTransitionManager.Instance != null)
        {
            SceneTransitionManager.Instance.TransitionToScene(SceneManager.GetActiveScene().name, gameOverRetryButton != null ? gameOverRetryButton.transform as RectTransform : null);
        }
        else
        {
            Time.timeScale = 1f;
            SceneManager.LoadScene(SceneManager.GetActiveScene().buildIndex);
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

    private void SetGameOverOverlayAlpha(float alpha)
    {
        if (gameOverDarkOverlay == null)
        {
            return;
        }

        Color color = gameOverDarkOverlay.color;
        color.a = alpha;
        gameOverDarkOverlay.color = color;
    }

    private void UpdateGameOverUI()
    {
        if (gameOverDistanceValueText != null)
        {
            gameOverDistanceValueText.text = $"{Mathf.FloorToInt(distance):N0} m";
        }
        else
        {
            Debug.LogWarning("Game Over Distance Value Text is not assigned.");
        }

        if (gameOverDriftScoreValueText != null)
        {
            gameOverDriftScoreValueText.text = driftScore.ToString("N0");
        }
        else
        {
            Debug.LogWarning("Game Over Drift Score Value Text is not assigned.");
        }

        if (gameOverCollectedPartsValueText != null)
        {
            gameOverCollectedPartsValueText.text = collectedParts.ToString("N0");
        }
        else
        {
            Debug.LogWarning("Game Over Collected Parts Value Text is not assigned.");
        }

        if (gameOverTotalScoreValueText != null)
        {
            gameOverTotalScoreValueText.text = TotalScore.ToString("N0");
        }
        else
        {
            Debug.LogWarning("Game Over Total Score Value Text is not assigned.");
        }
    }

    private void PlayGameOverScoreAnimation()
    {
        gameOverScoreSequence?.Kill();
        totalScoreAttentionTween?.Kill();
        newHighScoreTween?.Kill();

        if (gameOverTotalScoreValueText != null)
        {
            gameOverTotalScoreValueText.transform.DOKill();
            gameOverTotalScoreValueText.transform.localScale = Vector3.one;
        }

        int finalDistance = Mathf.FloorToInt(distance);
        int finalDriftScore = driftScore;
        int finalCollectedParts = collectedParts;
        int finalTotalScore = TotalScore;

        if (gameOverDistanceValueText != null)
        {
            gameOverDistanceValueText.text = "0 m";
        }

        if (gameOverDriftScoreValueText != null)
        {
            gameOverDriftScoreValueText.text = "0";
        }

        if (gameOverCollectedPartsValueText != null)
        {
            gameOverCollectedPartsValueText.text = "0";
        }

        if (gameOverTotalScoreValueText != null)
        {
            gameOverTotalScoreValueText.text = "0";
        }

        gameOverScoreSequence = DOTween.Sequence()
            .SetUpdate(true);

        if (gameOverDistanceValueText != null)
        {
            gameOverScoreSequence.Join(
                DOTween.To(
                    () => 0,
                    value => gameOverDistanceValueText.text = $"{value:N0} m",
                    finalDistance,
                    distanceCountDuration
                ).SetEase(Ease.OutCubic)
            );
        }

        if (gameOverDriftScoreValueText != null)
        {
            gameOverScoreSequence.Join(
                DOTween.To(
                    () => 0,
                    value => gameOverDriftScoreValueText.text = value.ToString("N0"),
                    finalDriftScore,
                    driftScoreCountDuration
                ).SetEase(Ease.OutCubic)
            );
        }

        if (gameOverCollectedPartsValueText != null)
        {
            gameOverScoreSequence.Join(
                DOTween.To(
                    () => 0,
                    value => gameOverCollectedPartsValueText.text = value.ToString("N0"),
                    finalCollectedParts,
                    collectedPartsCountDuration
                ).SetEase(Ease.OutCubic)
            );
        }

        if (gameOverTotalScoreValueText != null)
        {
            gameOverScoreSequence.Join(
                DOTween.To(
                    () => 0,
                    value => gameOverTotalScoreValueText.text = value.ToString("N0"),
                    finalTotalScore,
                    totalScoreCountDuration
                ).SetEase(Ease.OutCubic)
            );
        }

        gameOverScoreSequence.OnComplete(() =>
        {
            UpdateGameOverUI();
            StartTotalScoreAttentionAnimation();
        });
    }

    private void StartTotalScoreAttentionAnimation()
    {
        totalScoreAttentionTween?.Kill();

        if (gameOverTotalScoreValueText == null)
        {
            return;
        }

        Transform totalScoreTransform = gameOverTotalScoreValueText.transform;

        totalScoreTransform.DOKill();
        totalScoreTransform.localScale = Vector3.one;

        totalScoreAttentionTween = totalScoreTransform
            .DOScale(Vector3.one * totalScoreAttentionScale, totalScoreAttentionDuration)
            .SetEase(Ease.InOutSine)
            .SetLoops(-1, LoopType.Yoyo)
            .SetUpdate(true);
    }

    private void OnDestroy()
    {
        gameOverScoreSequence?.Kill();
        totalScoreAttentionTween?.Kill();
        newHighScoreTween?.Kill();
    }

    private void LoadHighScore()
    {
        HighScore = PlayerPrefs.GetInt(HighScorePlayerPrefsKey, 0);
    }

    private bool CheckAndSaveHighScore()
    {
        int finalScore = TotalScore;

        if (finalScore <= HighScore)
        {
            return false;
        }

        HighScore = finalScore;

        PlayerPrefs.SetInt(HighScorePlayerPrefsKey, HighScore);
        PlayerPrefs.Save();

        return true;
    }

    private void UpdateHighScoreUI()
    {
        if (gameOverHighScoreValueText != null)
        {
            gameOverHighScoreValueText.text = HighScore.ToString("N0");
        }
    }

    private void PlayNewHighScoreAnimation()
    {
        newHighScoreTween?.Kill();

        if (newHighScoreText == null)
        {
            return;
        }

        newHighScoreText.gameObject.SetActive(true);
        newHighScoreText.transform.DOKill();
        newHighScoreText.DOKill();

        newHighScoreText.transform.localScale = Vector3.one;
        newHighScoreText.color = newHighScoreColorA;

        Sequence sequence = DOTween.Sequence()
            .SetUpdate(true);

        sequence.Append(
            newHighScoreText.transform
                .DOScale(Vector3.one * newHighScoreScale, newHighScoreAnimationDuration)
                .SetEase(Ease.OutBack)
        );

        sequence.Append(
            newHighScoreText.transform
                .DOScale(Vector3.one, newHighScoreAnimationDuration)
                .SetEase(Ease.InOutSine)
        );

        sequence.Join(
            newHighScoreText
                .DOColor(newHighScoreColorB, newHighScoreAnimationDuration)
                .SetEase(Ease.InOutSine)
        );

        sequence.SetLoops(-1, LoopType.Yoyo);

        newHighScoreTween = sequence;
    }
}