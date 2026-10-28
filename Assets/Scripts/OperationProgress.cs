using UnityEngine;

public static class OperationProgress
{
    private const string TotalKey = "TinyRpg.TotalStars";

    public static int TotalStars => Mathf.Max(0, PlayerPrefs.GetInt(TotalKey, 0));

    public static int BestStars(string missionId)
    {
        return string.IsNullOrWhiteSpace(missionId) ? 0 :
            Mathf.Clamp(PlayerPrefs.GetInt("TinyRpg.Stars." + missionId, 0), 0, 3);
    }

    public static bool IsCompleted(string missionId) => BestStars(missionId) > 0;

    public static bool Record(string missionId, int stars)
    {
        if (string.IsNullOrWhiteSpace(missionId) || stars < 1 || stars > 3)
            return false;

        int previous = BestStars(missionId);
        if (stars <= previous)
            return false;

        PlayerPrefs.SetInt("TinyRpg.Stars." + missionId, stars);
        PlayerPrefs.SetInt(TotalKey, TotalStars + stars - previous);
        PlayerPrefs.Save();
        return true;
    }
}
