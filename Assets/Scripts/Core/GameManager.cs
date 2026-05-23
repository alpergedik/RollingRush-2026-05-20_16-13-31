using UnityEngine;
using UnityEngine.SceneManagement;
using TMPro;
using System.Collections;

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

    [Header("UI")]
    public GameObject scoreText;
    public int driftScore = 0;
    public GameObject startTitleText;
    public GameObject startDescriptionText;
    public GameObject gameOverTitleText;
    public GameObject gameOverDescriptionText;

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

        if (startTitleText != null)
        {
            startTitleText.SetActive(true);
        }
        
        if (startDescriptionText != null)
        {
            startDescriptionText.SetActive(true);
        }
        
        if (scoreText != null)
        {
            scoreText.SetActive(false);
        }

        if (gameOverTitleText != null)
        {
            gameOverTitleText.SetActive(false);
        }
        
        if (gameOverTitleText != null)
        {
            gameOverTitleText.SetActive(false);
        }

        UpdateScoreUI();
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

        if (startTitleText != null)
        {
            startTitleText.SetActive(false);
        }
        
        if (startDescriptionText != null)
        {
            startDescriptionText.SetActive(false);
        }

        if (scoreText != null)
        {
            scoreText.SetActive(true);
        }
        
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

    private void UpdateScoreUI()
    {
        if (scoreText == null)
        {
            return;
        }
        
        TMP_Text scoreTextTMP = scoreText.GetComponent<TMP_Text>();
        scoreTextTMP.text =
            "Distance: " + Mathf.FloorToInt(distance) + " m\n" +
            "Parts: " + collectedParts + "\n" +
            "Score: " + score + "\n" +
            "Drift Score: " + driftScore + "\n";
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
        if (gameOverTitleText == null || gameOverDescriptionText == null)
        {
            return;
        }
        
        if (scoreText != null)
        {
            scoreText.SetActive(false);
        }

        gameOverTitleText.SetActive(true);
        gameOverDescriptionText.SetActive(true);

        TMP_Text gameOverTMP = gameOverDescriptionText.GetComponent<TMP_Text>();

        
        if (gameOverTMP != null) {
            gameOverTMP.text =
                "Press R to Restart\n\n" +
                "Distance: " + Mathf.FloorToInt(distance) + " m\n" +
                "Parts: " + collectedParts + "\n" +
                "Score: " + score + "\n" +
                "Drift Score: " + driftScore;
        }
    }

    private void RestartGame()
    {
        Time.timeScale = 1f;
        SceneManager.LoadScene(SceneManager.GetActiveScene().buildIndex);
    }
}