using UnityEngine;

public static class DifficultySelection
{
    private const string PrefKey = "RollingRush_SelectedDifficulty";

    public static void SetSelectedDifficulty(DifficultyProfile difficulty)
    {
        PlayerPrefs.SetInt(PrefKey, (int)difficulty);
        PlayerPrefs.Save();
    }

    public static DifficultyProfile GetSelectedDifficulty(DifficultyProfile defaultDifficulty = DifficultyProfile.Normal)
    {
        return (DifficultyProfile)PlayerPrefs.GetInt(PrefKey, (int)defaultDifficulty);
    }
}
