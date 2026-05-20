using UnityEngine;
using UnityEngine.SceneManagement;
using TMPro;

public class GameManager : MonoBehaviour
{
    public static GameManager Instance;

    [Header("Game State")]
    public bool isGameStarted = false;
    public bool isGameOver = false;

    [Header("Speed")]
    public float currentSpeed = 0f;
    public float startSpeed = 8f;
    public float maxSpeed = 18f;
    public float speedIncreasePerSecond = 0.15f;

    [Header("Score")]
    public float distance = 0f;
    public int score = 0;
    public int scoreMultiplier = 10;
    public int collectedParts = 0;
    public int partScoreValue = 100;

    [Header("UI")]
    public TMP_Text scoreText;
    public GameObject startText;
    public GameObject gameOverText;

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
        collectedParts = 0;
        bonusScore = 0;

        if (startText != null)
        {
            startText.SetActive(true);
        }

        if (gameOverText != null)
        {
            gameOverText.SetActive(false);
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
        currentSpeed = startSpeed;

        if (startText != null)
        {
            startText.SetActive(false);
        }
    }

    private void UpdateSpeed()
    {
        currentSpeed += speedIncreasePerSecond * Time.deltaTime;
        currentSpeed = Mathf.Clamp(currentSpeed, startSpeed, maxSpeed);
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

        scoreText.text =
            "Distance: " + Mathf.FloorToInt(distance) + " m\n" +
            "Parts: " + collectedParts + "\n" +
            "Score: " + score;
    }

    public void GameOver()
    {
        if (isGameOver)
        {
            return;
        }

        isGameOver = true;
        currentSpeed = 0f;

        if (gameOverText != null)
        {
            gameOverText.SetActive(true);

            TMP_Text gameOverTMP = gameOverText.GetComponent<TMP_Text>();

            if (gameOverTMP != null)
            {
                gameOverTMP.text =
                    "GAME OVER\n" +
                    "Distance: " + Mathf.FloorToInt(distance) + " m\n" +
                    "Parts: " + collectedParts + "\n" +
                    "Score: " + score + "\n" +
                    "Press R to Restart";
            }
        }

        Time.timeScale = 0f;
    }

    private void RestartGame()
    {
        Time.timeScale = 1f;
        SceneManager.LoadScene(SceneManager.GetActiveScene().buildIndex);
    }
}