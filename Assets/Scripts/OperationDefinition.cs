using System;
using UnityEngine;

[CreateAssetMenu(menuName = "Tiny RPG/Operation")]
public class OperationDefinition : ScriptableObject
{
    public enum Objective { EliminateAll, ClearArea, BossEncounter }

    [Serializable]
    public struct EnemySpawn
    {
        public Vector3 position;
        public bool inObjectiveArea;
        public EnemyTarget prefabOverride;
    }

    [SerializeField] private string missionId = "operation-1";
    [SerializeField] private string displayName = "Operation 1";
    [SerializeField] private string prerequisiteMissionId;
    [SerializeField] private string mapScene;
    [SerializeField] private string missionScene;
    [SerializeField] private bool finalOperation;
    [SerializeField, Min(1)] private int maxSquadSize = 1;
    [SerializeField] private CharacterCombat.Class[] availableCharacters = { CharacterCombat.Class.Warrior };
    [SerializeField] private Objective objective;
    [SerializeField, HideInInspector] private EnemySpawn[] enemySpawns;
    [SerializeField, Min(1f)] private float twoStarTimeSeconds = 120f;
    [SerializeField, Min(0)] private int threeStarMaxSquadDeaths;

    public string MissionId => missionId;
    public string DisplayName => displayName;
    public string PrerequisiteMissionId => prerequisiteMissionId;
    public string MapScene => mapScene;
    public string MissionScene => missionScene;
    public bool FinalOperation => finalOperation;
    public int MaxSquadSize => Mathf.Max(1, maxSquadSize);
    public Objective MissionObjective => objective;
    public EnemySpawn[] EnemySpawns => enemySpawns;
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
