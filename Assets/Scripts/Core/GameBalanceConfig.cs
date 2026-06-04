using UnityEngine;

public enum DifficultyProfile
{
    Easy,
    Normal,
    Hard
}

[System.Serializable]
public class BalanceSettings
{
    [Header("Speed Settings")]
    public float defaultSpeed = 10f;
    public float boostSpeed = 16f;
    public float brakeSpeed = 5f;
    public float speedChangeRate = 10f;

    [Header("Horizontal Movement")]
    public float maxHorizontalSpeed = 7f;
    public float horizontalAcceleration = 20f;
    public float horizontalDeceleration = 14f;

    [Header("Jump Settings")]
    [Tooltip("Hedef zıplama yüksekliği (metre). Jump velocity bu değere göre hesaplanacaktır.")]
    public float targetJumpHeight = 2.5f;

    [Header("Drift Settings")]
    public float minDriftForwardSpeed = 11f;
    public float fullDriftForwardSpeed = 16f;
    public float minDriftHorizontalSpeed = 2.5f;
    public float fullDriftHorizontalSpeed = 7f;
    public float driftBuildSpeed = 5f;
    public float driftFadeSpeed = 4f;
    
    [Header("Drift Visuals")]
    public float driftSideViewAngle = 28f;
    public float driftExtraLeanAngle = 8f;

    [Header("FX Settings")]
    public float speedLinesMinSpeed = 12f;
    public float speedLinesFullSpeed = 16f;

    [Header("Spawn Timing")]
    public float obstacleMinSpawnInterval = 1.0f;
    public float obstacleMaxSpawnInterval = 2.0f;
    public float collectibleMinSpawnInterval = 0.5f;
    public float collectibleMaxSpawnInterval = 1.2f;

    public void Validate()
    {
        if (brakeSpeed >= defaultSpeed || defaultSpeed >= boostSpeed)
            Debug.LogWarning("BalanceSettings: Ensure brakeSpeed < defaultSpeed < boostSpeed");
            
        if (minDriftForwardSpeed > fullDriftForwardSpeed)
            Debug.LogWarning("BalanceSettings: minDriftForwardSpeed should be <= fullDriftForwardSpeed");
            
        if (minDriftHorizontalSpeed > fullDriftHorizontalSpeed)
            Debug.LogWarning("BalanceSettings: minDriftHorizontalSpeed should be <= fullDriftHorizontalSpeed");
            
        if (fullDriftHorizontalSpeed > maxHorizontalSpeed)
            Debug.LogWarning("BalanceSettings: fullDriftHorizontalSpeed should be <= maxHorizontalSpeed");
            
        if (obstacleMinSpawnInterval >= obstacleMaxSpawnInterval)
            Debug.LogWarning("BalanceSettings: obstacleMinSpawnInterval should be < obstacleMaxSpawnInterval");
            
        if (collectibleMinSpawnInterval >= collectibleMaxSpawnInterval)
            Debug.LogWarning("BalanceSettings: collectibleMinSpawnInterval should be < collectibleMaxSpawnInterval");
            
        if (targetJumpHeight <= 0f)
            Debug.LogWarning("BalanceSettings: targetJumpHeight must be positive");
            
        if (defaultSpeed <= 0f || boostSpeed <= 0f || brakeSpeed <= 0f || maxHorizontalSpeed <= 0f)
            Debug.LogWarning("BalanceSettings: All base speed values must be positive");
    }
}

[CreateAssetMenu(fileName = "GameBalanceConfig", menuName = "RollingRush/Game Balance Config")]
public class GameBalanceConfig : ScriptableObject
{
    [Tooltip("Aktif zorluk profilini seçin. Runtime'da değerler buradan okunacaktır.")]
    public DifficultyProfile activeProfile = DifficultyProfile.Normal;

public BalanceSettings easyProfile = new BalanceSettings
{
    defaultSpeed = 3f,
    boostSpeed = 5f,
    brakeSpeed = 1f,
    speedChangeRate = 3f,

    maxHorizontalSpeed = 2.5f,
    horizontalAcceleration = 7f,
    horizontalDeceleration = 6f,

    targetJumpHeight = 0.8f,

    minDriftForwardSpeed = 4f,
    fullDriftForwardSpeed = 5f,
    minDriftHorizontalSpeed = 0.9f,
    fullDriftHorizontalSpeed = 2.5f,

    driftBuildSpeed = 3f,
    driftFadeSpeed = 2.5f,
    driftSideViewAngle = 16f,
    driftExtraLeanAngle = 4f,

    speedLinesMinSpeed = 4f,
    speedLinesFullSpeed = 5f,

    obstacleMinSpawnInterval = 3.5f,
    obstacleMaxSpawnInterval = 5f,

    collectibleMinSpawnInterval = 1.2f,
    collectibleMaxSpawnInterval = 2.2f
};

public BalanceSettings normalProfile = new BalanceSettings
{
    defaultSpeed = 5f,
    boostSpeed = 8f,
    brakeSpeed = 2f,
    speedChangeRate = 5f,

    maxHorizontalSpeed = 3.8f,
    horizontalAcceleration = 10f,
    horizontalDeceleration = 8f,

    targetJumpHeight = 0.8f,

    minDriftForwardSpeed = 6f,
    fullDriftForwardSpeed = 8f,
    minDriftHorizontalSpeed = 1.4f,
    fullDriftHorizontalSpeed = 3.8f,

    driftBuildSpeed = 3.5f,
    driftFadeSpeed = 3f,
    driftSideViewAngle = 18f,
    driftExtraLeanAngle = 5f,

    speedLinesMinSpeed = 6f,
    speedLinesFullSpeed = 8f,

    obstacleMinSpawnInterval = 2.4f,
    obstacleMaxSpawnInterval = 3.6f,

    collectibleMinSpawnInterval = 0.9f,
    collectibleMaxSpawnInterval = 1.8f
};

public BalanceSettings hardProfile = new BalanceSettings
{
    defaultSpeed = 8f,
    boostSpeed = 10f,
    brakeSpeed = 6f,
    speedChangeRate = 5f,

    maxHorizontalSpeed = 5f,
    horizontalAcceleration = 13f,
    horizontalDeceleration = 10f,

    targetJumpHeight = 0.8f,

    minDriftForwardSpeed = 9f,
    fullDriftForwardSpeed = 10f,
    minDriftHorizontalSpeed = 1.8f,
    fullDriftHorizontalSpeed = 5f,

    driftBuildSpeed = 4f,
    driftFadeSpeed = 3.5f,
    driftSideViewAngle = 22f,
    driftExtraLeanAngle = 6f,

    speedLinesMinSpeed = 9f,
    speedLinesFullSpeed = 10f,

    obstacleMinSpawnInterval = 1.8f,
    obstacleMaxSpawnInterval = 2.8f,

    collectibleMinSpawnInterval = 0.7f,
    collectibleMaxSpawnInterval = 1.5f
};

    public BalanceSettings Current
    {
        get
        {
            switch (activeProfile)
            {
                case DifficultyProfile.Easy: return easyProfile;
                case DifficultyProfile.Hard: return hardProfile;
                default: return normalProfile;
            }
        }
    }

    private void OnValidate()
    {
        easyProfile?.Validate();
        normalProfile?.Validate();
        hardProfile?.Validate();
    }
}
