using UnityEngine;
using UnityEngine.AI;
using UnityEngine.SceneManagement;
using System.Collections.Generic;

public class SquadPreparation : MonoBehaviour
{
    [SerializeField] private OperationDefinition operation;
    [SerializeField] private OperationDefinition[] operations;
    [SerializeField] private EnemyTarget enemyPrefab;
    [SerializeField] private CharacterCombat archerPrefab;
    [SerializeField] private CharacterCombat magePrefab;
    [SerializeField, Min(0)] private int archerUnlockStars = 3;
    [SerializeField, Min(0)] private int mageUnlockStars = 6;

    private bool archerSelected;
    private bool mageSelected;
    private bool started;
    private bool choosingOperation = true;
    private static string pendingMissionId;
    private PlayerController player;
    private MissionManager mission;
    private GUIStyle label;
    private GUIStyle button;

    public bool Started => started;
    public OperationDefinition Operation => operation;
    public int SelectedCount => 1 + (archerSelected ? 1 : 0) + (mageSelected ? 1 : 0);

    private void Awake()
    {
        player = FindFirstObjectByType<PlayerController>();
        mission = GetComponent<MissionManager>();
        if (player != null)
            player.enabled = false;
        Time.timeScale = 0f;
        if (!string.IsNullOrEmpty(pendingMissionId))
        {
            string retryId = pendingMissionId;
            pendingMissionId = null;
            foreach (OperationDefinition candidate in operations)
                if (candidate != null && candidate.MissionId == retryId)
                    TryChooseOperation(candidate);
        }
    }

    public bool IsOperationUnlocked(OperationDefinition candidate) => candidate != null &&
        (string.IsNullOrEmpty(candidate.PrerequisiteMissionId) ||
         OperationProgress.IsCompleted(candidate.PrerequisiteMissionId));

    public bool TryChooseOperation(OperationDefinition candidate)
    {
        if (started || !IsOperationUnlocked(candidate) || System.Array.IndexOf(operations, candidate) < 0)
            return false;
        operation = candidate;
        archerSelected = mageSelected = false;
        choosingOperation = false;
        return true;
    }

    public void QueueRetry()
    {
        if (operation != null)
            pendingMissionId = operation.MissionId;
    }

    public void ReturnToMap()
    {
        pendingMissionId = null;
        Time.timeScale = 1f;
        SceneManager.LoadScene(SceneManager.GetActiveScene().name);
    }

    public bool IsUnlocked(CharacterCombat.Class character)
    {
        int stars = OperationProgress.TotalStars;
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
        if (started || choosingOperation || operation == null || player == null || mission == null || enemyPrefab == null ||
            operation.EnemySpawns == null || operation.EnemySpawns.Length == 0 ||
            !operation.Allows(CharacterCombat.Class.Warrior) ||
            SelectedCount > operation.MaxSquadSize ||
            archerSelected && (archerPrefab == null || !IsUnlocked(CharacterCombat.Class.Archer)) ||
            mageSelected && (magePrefab == null || !IsUnlocked(CharacterCombat.Class.Mage)))
            return false;

        Vector3[] positions = new Vector3[operation.EnemySpawns.Length];
        int requiredCount = 0;
        for (int i = 0; i < positions.Length; i++)
        {
            if (!NavMesh.SamplePosition(operation.EnemySpawns[i].position, out NavMeshHit hit, 1f, NavMesh.AllAreas))
                return false;
            positions[i] = hit.position;
            if (operation.MissionObjective == OperationDefinition.Objective.EliminateAll ||
                operation.EnemySpawns[i].inObjectiveArea)
                requiredCount++;
        }
        if (requiredCount == 0)
            return false;

        if (archerSelected)
            Spawn(archerPrefab, new Vector3(-1.4f, 0f, -1.4f));
        if (mageSelected)
            Spawn(magePrefab, new Vector3(1.4f, 0f, -1.4f));
        List<EnemyTarget> required = new List<EnemyTarget>(requiredCount);
        for (int i = 0; i < positions.Length; i++)
        {
            EnemyTarget enemy = Instantiate(enemyPrefab, positions[i], Quaternion.identity);
            bool objective = operation.MissionObjective == OperationDefinition.Objective.EliminateAll ||
                operation.EnemySpawns[i].inObjectiveArea;
            if (objective)
                required.Add(enemy);
            if (operation.MissionObjective == OperationDefinition.Objective.ClearArea && objective)
            {
                Renderer renderer = enemy.GetComponentInChildren<Renderer>();
                if (renderer != null)
                    renderer.material.color = Color.yellow;
            }
        }
        started = true;
        mission.Begin(required.ToArray());
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
        if (started)
            return;

        if (label == null)
        {
            label = new GUIStyle(GUI.skin.label) { fontSize = 20, alignment = TextAnchor.MiddleCenter };
            button = new GUIStyle(GUI.skin.button) { fontSize = 17 };
        }
        Matrix4x4 previousMatrix = GUI.matrix;
        GUI.matrix = Matrix4x4.Scale(new Vector3(Screen.width / 960f, Screen.height / 540f, 1f)) * previousMatrix;
        Color previousColor = GUI.color;
        if (choosingOperation)
        {
            GUI.color = new Color(0.15f, 0.18f, 0.22f, 0.95f);
            GUI.Box(new Rect(55, 30, 850, 480), GUIContent.none);
            GUI.color = previousColor;
            GUI.Label(new Rect(180, 42, 600, 42), "Map 1 — Operations", label);
            for (int i = 0; i < operations.Length; i++)
            {
                OperationDefinition candidate = operations[i];
                if (candidate == null)
                    continue;
                bool unlocked = IsOperationUnlocked(candidate);
                int best = OperationProgress.BestStars(candidate.MissionId);
                bool wasEnabled = GUI.enabled;
                GUI.enabled = unlocked;
                int column = i / 5;
                int row = i % 5;
                string status = best > 0 ? "  " + best + "/3 stars" : unlocked ? "  Available" : "  Locked";
                if (GUI.Button(new Rect(85 + column * 420, 105 + row * 72, 370, 52), candidate.DisplayName + status, button))
                    TryChooseOperation(candidate);
                GUI.enabled = wasEnabled;
            }
            GUI.matrix = previousMatrix;
            return;
        }
        GUI.color = new Color(0.15f, 0.18f, 0.22f, 0.95f);
        GUI.Box(new Rect(270, 100, 420, 360), GUIContent.none);
        GUI.color = previousColor;
        GUI.Label(new Rect(290, 130, 380, 40),
            operation.DisplayName + "  |  Squad " + SelectedCount + "/" + operation.MaxSquadSize, label);
        GUI.Label(new Rect(305, 180, 350, 34), "Warrior — selected", label);
        DrawChoice(CharacterCombat.Class.Archer, archerSelected, 225);
        DrawChoice(CharacterCombat.Class.Mage, mageSelected, 270);
        if (GUI.Button(new Rect(375, 350, 210, 48), "Start Operation", button))
            StartOperation();
        if (GUI.Button(new Rect(375, 405, 210, 38), "Back to Map", button))
            choosingOperation = true;
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
