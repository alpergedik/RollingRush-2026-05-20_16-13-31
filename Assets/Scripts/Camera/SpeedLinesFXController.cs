using UnityEngine;

public class SpeedLinesFXController : MonoBehaviour
{
    [Header("References")]
    [SerializeField] private ParticleSystem speedLinesParticle;

    [Header("Speed Threshold")]
    [SerializeField] private float minSpeedForLines = 12f;
    [SerializeField] private float fullSpeedForLines = 16f;

    [Header("Emission")]
    [SerializeField] private float maxEmissionRate = 45f;
    [SerializeField] private float emissionSmoothSpeed = 8f;

    private float currentEmissionRate;
    private ParticleSystem.EmissionModule emissionModule;

    private void Awake()
    {
        if (speedLinesParticle != null)
        {
            emissionModule = speedLinesParticle.emission;
            SetEmission(0f);
            StopSpeedLines(true);
        }
    }

    private void Update()
    {
        if (GameManager.Instance == null || !GameManager.Instance.isGameStarted || GameManager.Instance.isGameOver)
        {
            if (currentEmissionRate > 0f)
            {
                currentEmissionRate = 0f;
                SetEmission(0f);
            }
            StopSpeedLines(false);
            SoundManager.Instance?.SetWindIntensity(0f);
            return;
        }

        UpdateSpeedLines();
    }

    private void UpdateSpeedLines()
    {
        float currentSpeed = GameManager.Instance.currentSpeed;
        float speedFactor = Mathf.InverseLerp(minSpeedForLines, fullSpeedForLines, currentSpeed);
        float targetEmission = maxEmissionRate * speedFactor;

        SoundManager.Instance?.SetWindIntensity(speedFactor);

        currentEmissionRate = Mathf.MoveTowards(currentEmissionRate, targetEmission, emissionSmoothSpeed * Time.deltaTime);

        if (currentEmissionRate > 0.1f)
        {
            if (!speedLinesParticle.isPlaying)
            {
                speedLinesParticle.Play();
            }
            SetEmission(currentEmissionRate);
        }
        else
        {
            StopSpeedLines(false);
            SetEmission(0f);
        }
    }

    private void SetEmission(float rate)
    {
        if (speedLinesParticle != null)
        {
            emissionModule.rateOverTime = rate;
        }
    }

    private void StopSpeedLines(bool clear)
    {
        if (speedLinesParticle != null)
        {
            if (clear)
            {
                speedLinesParticle.Stop(true, ParticleSystemStopBehavior.StopEmittingAndClear);
            }
            else if (speedLinesParticle.isPlaying)
            {
                speedLinesParticle.Stop(true, ParticleSystemStopBehavior.StopEmitting);
            }
        }
    }

    private void OnDisable()
    {
        SoundManager.Instance?.SetWindIntensity(0f);
    }
}
