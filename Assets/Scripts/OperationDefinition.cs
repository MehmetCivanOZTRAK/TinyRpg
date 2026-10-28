using UnityEngine;

[CreateAssetMenu(menuName = "Tiny RPG/Operation")]
public class OperationDefinition : ScriptableObject
{
    [SerializeField] private string missionId = "operation-1";
    [SerializeField] private string displayName = "Operation 1";
    [SerializeField, Min(1)] private int maxSquadSize = 1;
    [SerializeField] private CharacterCombat.Class[] availableCharacters = { CharacterCombat.Class.Warrior };

    public string MissionId => missionId;
    public string DisplayName => displayName;
    public int MaxSquadSize => Mathf.Max(1, maxSquadSize);

    public bool Allows(CharacterCombat.Class character)
    {
        foreach (CharacterCombat.Class available in availableCharacters)
            if (available == character)
                return true;
        return false;
    }
}
