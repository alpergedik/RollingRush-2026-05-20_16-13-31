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

    [SerializeField] private float defaultSpeed = 10f;
    [SerializeField] private float boostSpeed = 16f;
    [SerializeField] private float brakeSpeed = 5f;
    [SerializeField] private float speedChangeRate = 10f;

    [Header("Score")]
    public float distance = 0f;
    public int score = 0;
    public int scoreMultiplier = 10;
    public int collectedParts = 0;
    public int partScoreValue = 100;
    public int driftScore = 0;

    [Header("Gameplay UI")]
    public GameObject scoreText;

    [Header("Game Over UI")]
    [SerializeField] private GameObject gameOverPanel;
    [SerializeField] private Image gameOverDarkOverlay;
    [SerializeField] private TMP_Text gameOverStatsText;
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

    private int bonusScore = 0;

    private void Awake()
    {
        Instance = this;

        Time.timeScale = 1f;

        isGameStarted = false;
        isGameOver = false;

        currentSpeed = 0f;
        distance = 0f;
        score = 0;
        driftScore = 0;
        collectedParts = 0;
        bonusScore = 0;

        if (scoreText != null)
        {
            scoreText.SetActive(false);
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
    }

    private void Update()
    {
        if (!isGameStarted)
        {
            if (Input.GetKeyDown(KeyCode.Space))
            {
                StartGame();
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
        UpdateScore();
    }

    private void StartGame()
    {
        isGameStarted = true;
        currentSpeed = defaultSpeed;

        if (scoreText != null)
        {
            scoreText.SetActive(true);
        }

        UpdateScoreUI();
    }

    private void UpdateSpeed()
    {
        float targetSpeed = defaultSpeed;

        if (Input.GetKey(KeyCode.W) || Input.GetKey(KeyCode.UpArrow))
        {
            targetSpeed = boostSpeed;
        }
        else if (Input.GetKey(KeyCode.S) || Input.GetKey(KeyCode.DownArrow))
        {
            targetSpeed = brakeSpeed;
        }

        currentSpeed = Mathf.MoveTowards(
            currentSpeed,
            targetSpeed,
            speedChangeRate * Time.deltaTime
        );
    }

    private void UpdateScore()
    {
        distance += currentSpeed * Time.deltaTime;
        score = Mathf.FloorToInt(distance * scoreMultiplier) + bonusScore;

        UpdateScoreUI();
    }

    public void CollectPart()
    {
        if (!isGameStarted || isGameOver)
        {
            return;
        }

        collectedParts++;
        bonusScore += partScoreValue;

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
            "Parts: " + collectedParts + "\n" +
            "Score: " + score + "\n" +
            "Drift Score: " + driftScore + "\n";
    }

    public void GameOver(float delayBeforeFreeze = 0f)
    {
        if (isGameOver)
        {
            return;
        }

        isGameOver = true;
        currentSpeed = 0f;

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

        if (gameOverStatsText != null)
        {
            gameOverStatsText.text =
                "SCORE: " + score + "\n" +
                "DISTANCE: " + Mathf.FloorToInt(distance) + " m\n" +
                "DRIFT SCORE: " + driftScore + "\n" +
                "PARTS: " + collectedParts;
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
        Time.timeScale = 1f;
        SceneManager.LoadScene(mainMenuSceneName);
    }

    private void RestartGame()
    {
        Time.timeScale = 1f;
        SceneManager.LoadScene(SceneManager.GetActiveScene().buildIndex);
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
}