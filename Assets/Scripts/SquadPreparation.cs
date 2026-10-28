using System.Collections.Generic;
using UnityEngine;
using UnityEngine.AI;
using UnityEngine.SceneManagement;
using UnityEngine.UI;

public class SquadPreparation : MonoBehaviour
{
    [SerializeField] private OperationDefinition operation;
    [SerializeField] private OperationDefinition[] operations;
    [SerializeField] private EnemyTarget enemyPrefab;
    [SerializeField] private CharacterCombat archerPrefab;
    [SerializeField] private CharacterCombat magePrefab;
    [SerializeField, Min(0)] private int archerUnlockStars = 3;
    [SerializeField, Min(0)] private int mageUnlockStars = 6;
    [SerializeField] private GameObject mapPanel;
    [SerializeField] private Button[] operationButtons;
    [SerializeField] private GameObject preparationPanel;
    [SerializeField] private Text preparationTitle;
    [SerializeField] private Button archerButton;
    [SerializeField] private Button mageButton;
    [SerializeField] private Button startButton;
    [SerializeField] private Button backButton;

    private bool archerSelected;
    private bool mageSelected;
    private bool started;
    private bool choosingOperation = true;
    private static string pendingMissionId;
    private PlayerController player;
    private MissionManager mission;

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
        for (int i = 0; i < operationButtons.Length && i < operations.Length; i++)
        {
            OperationDefinition candidate = operations[i];
            if (operationButtons[i] != null)
                operationButtons[i].onClick.AddListener(() => TryChooseOperation(candidate));
        }
        archerButton.onClick.AddListener(() => TrySelect(CharacterCombat.Class.Archer));
        mageButton.onClick.AddListener(() => TrySelect(CharacterCombat.Class.Mage));
        startButton.onClick.AddListener(() => StartOperation());
        backButton.onClick.AddListener(() => { choosingOperation = true; RefreshUI(); });
        RefreshUI();
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
        RefreshUI();
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
        RefreshUI();
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
        RefreshUI();
        return true;
    }

    private void Spawn(CharacterCombat prefab, Vector3 offset)
    {
        Vector3 position = player.transform.position + offset;
        if (NavMesh.SamplePosition(position, out NavMeshHit hit, 3f, NavMesh.AllAreas))
            position = hit.position;
        Instantiate(prefab, position, player.transform.rotation);
    }

    private void RefreshUI()
    {
        if (mapPanel == null || preparationPanel == null)
            return;
        mapPanel.SetActive(!started && choosingOperation);
        preparationPanel.SetActive(!started && !choosingOperation);
        if (!started && choosingOperation)
        {
            for (int i = 0; i < operations.Length && i < operationButtons.Length; i++)
            {
                OperationDefinition candidate = operations[i];
                Button button = operationButtons[i];
                if (candidate == null || button == null)
                    continue;
                bool unlocked = IsOperationUnlocked(candidate);
                int best = OperationProgress.BestStars(candidate.MissionId);
                string status = best > 0 ? "  " + best + "/3 stars" : unlocked ? "  Available" : "  Locked";
                button.interactable = unlocked;
                button.GetComponentInChildren<Text>().text = candidate.DisplayName + status;
            }
        }
        if (started || choosingOperation || operation == null)
            return;
        preparationTitle.text = operation.DisplayName + "  |  Squad " + SelectedCount + "/" + operation.MaxSquadSize;
        RefreshChoice(archerButton, CharacterCombat.Class.Archer, archerSelected);
        RefreshChoice(mageButton, CharacterCombat.Class.Mage, mageSelected);
    }

    private void RefreshChoice(Button button, CharacterCombat.Class character, bool selected)
    {
        bool available = operation.Allows(character) && IsUnlocked(character);
        string caption = !operation.Allows(character) ? character + " — unavailable here" :
            !IsUnlocked(character) ? character + " — locked" :
            character + (selected ? " — selected" : " — select");
        button.interactable = available && (selected || SelectedCount < operation.MaxSquadSize);
        button.GetComponentInChildren<Text>().text = caption;
    }

    private void OnDestroy()
    {
        if (!started)
            Time.timeScale = 1f;
    }
}
