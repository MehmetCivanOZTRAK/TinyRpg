using UnityEngine;
using UnityEngine.AI;

public class SquadPreparation : MonoBehaviour
{
    [SerializeField] private OperationDefinition operation;
    [SerializeField] private CharacterCombat archerPrefab;
    [SerializeField] private CharacterCombat magePrefab;
    [SerializeField, Min(0)] private int archerUnlockStars = 3;
    [SerializeField, Min(0)] private int mageUnlockStars = 6;

    private bool archerSelected;
    private bool mageSelected;
    private bool started;
    private PlayerController player;
    private MissionManager mission;
    private GUIStyle label;
    private GUIStyle button;

    public bool Started => started;
    public int SelectedCount => 1 + (archerSelected ? 1 : 0) + (mageSelected ? 1 : 0);

    private void Awake()
    {
        player = FindFirstObjectByType<PlayerController>();
        mission = GetComponent<MissionManager>();
        if (player != null)
            player.enabled = false;
        Time.timeScale = 0f;
    }

    public bool IsUnlocked(CharacterCombat.Class character)
    {
        // shortcut: TINY-22 will write the earned star total after each operation.
        int stars = PlayerPrefs.GetInt("TinyRpg.TotalStars", 0);
        return character == CharacterCombat.Class.Warrior ||
            character == CharacterCombat.Class.Archer && stars >= archerUnlockStars ||
            character == CharacterCombat.Class.Mage && stars >= mageUnlockStars;
    }

    public bool TrySelect(CharacterCombat.Class character)
    {
        if (started || operation == null || character == CharacterCombat.Class.Warrior ||
            !operation.Allows(character) || !IsUnlocked(character))
            return false;

        bool selected = character == CharacterCombat.Class.Archer ? archerSelected : mageSelected;
        if (!selected && SelectedCount >= operation.MaxSquadSize)
            return false;

        if (character == CharacterCombat.Class.Archer)
            archerSelected = !archerSelected;
        else
            mageSelected = !mageSelected;
        return true;
    }

    public bool StartOperation()
    {
        if (started || operation == null || player == null || mission == null ||
            !operation.Allows(CharacterCombat.Class.Warrior) ||
            SelectedCount > operation.MaxSquadSize ||
            archerSelected && (archerPrefab == null || !IsUnlocked(CharacterCombat.Class.Archer)) ||
            mageSelected && (magePrefab == null || !IsUnlocked(CharacterCombat.Class.Mage)))
            return false;

        if (archerSelected)
            Spawn(archerPrefab, new Vector3(-1.4f, 0f, -1.4f));
        if (mageSelected)
            Spawn(magePrefab, new Vector3(1.4f, 0f, -1.4f));
        started = true;
        mission.Begin();
        player.enabled = true;
        Time.timeScale = 1f;
        return true;
    }

    private void Spawn(CharacterCombat prefab, Vector3 offset)
    {
        Vector3 position = player.transform.position + offset;
        if (NavMesh.SamplePosition(position, out NavMeshHit hit, 3f, NavMesh.AllAreas))
            position = hit.position;
        Instantiate(prefab, position, player.transform.rotation);
    }

    private void OnGUI()
    {
        if (started || operation == null)
            return;

        if (label == null)
        {
            label = new GUIStyle(GUI.skin.label) { fontSize = 20, alignment = TextAnchor.MiddleCenter };
            button = new GUIStyle(GUI.skin.button) { fontSize = 17 };
        }
        Matrix4x4 previousMatrix = GUI.matrix;
        GUI.matrix = Matrix4x4.Scale(new Vector3(Screen.width / 960f, Screen.height / 540f, 1f)) * previousMatrix;
        Color previousColor = GUI.color;
        GUI.color = new Color(0.15f, 0.18f, 0.22f, 0.95f);
        GUI.Box(new Rect(270, 110, 420, 320), GUIContent.none);
        GUI.color = previousColor;
        GUI.Label(new Rect(290, 130, 380, 40),
            operation.DisplayName + "  |  Squad " + SelectedCount + "/" + operation.MaxSquadSize, label);
        GUI.Label(new Rect(305, 180, 350, 34), "Warrior — selected", label);
        DrawChoice(CharacterCombat.Class.Archer, archerSelected, 225);
        DrawChoice(CharacterCombat.Class.Mage, mageSelected, 270);
        if (GUI.Button(new Rect(375, 350, 210, 48), "Start Operation", button))
            StartOperation();
        GUI.matrix = previousMatrix;
    }

    private void DrawChoice(CharacterCombat.Class character, bool selected, int y)
    {
        bool available = operation.Allows(character) && IsUnlocked(character);
        string caption = !operation.Allows(character) ? character + " — unavailable here" :
            !IsUnlocked(character) ? character + " — locked" :
            character + (selected ? " — selected" : " — select");
        bool previousEnabled = GUI.enabled;
        GUI.enabled = available && (selected || SelectedCount < operation.MaxSquadSize);
        if (GUI.Button(new Rect(305, y, 350, 38), caption, button))
            TrySelect(character);
        GUI.enabled = previousEnabled;
    }

    private void OnDestroy()
    {
        if (!started)
            Time.timeScale = 1f;
    }
}
