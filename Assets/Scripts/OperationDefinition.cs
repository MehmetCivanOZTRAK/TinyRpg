using UnityEngine;

[CreateAssetMenu(menuName = "Tiny RPG/Operation")]
public class OperationDefinition : ScriptableObject
{
    [SerializeField] private string missionId = "operation-1";
    [SerializeField] private string displayName = "Operation 1";
    [SerializeField, Min(1)] private int maxSquadSize = 1;
    [SerializeField] private CharacterCombat.Class[] availableCharacters = { CharacterCombat.Class.Warrior };
    [SerializeField, Min(1f)] private float twoStarTimeSeconds = 120f;
    [SerializeField, Min(0)] private int threeStarMaxSquadDeaths;

    public string MissionId => missionId;
    public string DisplayName => displayName;
    public int MaxSquadSize => Mathf.Max(1, maxSquadSize);
    public float TwoStarTimeSeconds => Mathf.Max(1f, twoStarTimeSeconds);
    public int ThreeStarMaxSquadDeaths => Mathf.Max(0, threeStarMaxSquadDeaths);

    public int EvaluateStars(float elapsedSeconds, int squadDeaths)
    {
        if (elapsedSeconds > TwoStarTimeSeconds)
            return 1;
        return squadDeaths <= ThreeStarMaxSquadDeaths ? 3 : 2;
    }

    public bool Allows(CharacterCombat.Class character)
    {
        foreach (CharacterCombat.Class available in availableCharacters)
            if (available == character)
                return true;
        return false;
    }
}
