using System.Collections;
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
    [SerializeField] private Button switchMapButton;
    [SerializeField] private string otherMapScene;
    [SerializeField] private OperationDefinition switchMapUnlockOperation;
    [SerializeField] private Button nextMapButton;
    [SerializeField] private string nextMapScene;
    [SerializeField] private OperationDefinition nextMapUnlockOperation;
    [SerializeField] private GameObject mapPreview;

    private bool archerSelected;
    private bool mageSelected;
    private bool started;
    private bool loading;
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
        switchMapButton.onClick.AddListener(() => SwitchMap(switchMapButton, otherMapScene));
        if (nextMapButton != null)
            nextMapButton.onClick.AddListener(() => SwitchMap(nextMapButton, nextMapScene));
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

    private void SwitchMap(Button button, string scene)
    {
        if (button.interactable && !string.IsNullOrEmpty(scene))
        {
            Time.timeScale = 1f;
            SceneManager.LoadScene(scene);
        }
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
        if (started || loading || choosingOperation || operation == null || player == null || mission == null || enemyPrefab == null ||
            string.IsNullOrEmpty(operation.MissionScene) || !Application.CanStreamedLevelBeLoaded(operation.MissionScene) ||
            !string.IsNullOrEmpty(operation.MapScene) && operation.MapScene != SceneManager.GetActiveScene().name ||
            !operation.Allows(CharacterCombat.Class.Warrior) ||
            SelectedCount > operation.MaxSquadSize ||
            archerSelected && (archerPrefab == null || !IsUnlocked(CharacterCombat.Class.Archer)) ||
            mageSelected && (magePrefab == null || !IsUnlocked(CharacterCombat.Class.Mage)))
            return false;

        loading = true;
        startButton.interactable = false;
        StartCoroutine(LoadOperation());
        return true;
    }

    private IEnumerator LoadOperation()
    {
        AsyncOperation load = SceneManager.LoadSceneAsync(operation.MissionScene, LoadSceneMode.Additive);
        if (load == null)
        {
            FailStart(default(Scene));
            yield break;
        }
        yield return load;
        Scene scene = SceneManager.GetSceneByName(operation.MissionScene);
        if (!scene.IsValid() || !scene.isLoaded)
        {
            FailStart(scene);
            yield break;
        }
        OperationSceneLayout layout = null;
        foreach (GameObject root in scene.GetRootGameObjects())
        {
            layout = root.GetComponentInChildren<OperationSceneLayout>();
            if (layout != null)
                break;
        }
        if (layout == null || layout.PlayerSpawn == null || mapPreview == null)
        {
            FailStart(scene);
            yield break;
        }

        mapPreview.SetActive(false);
        OperationSpawnPoint[] spawns = layout.SpawnPoints;
        Vector3[] positions = new Vector3[spawns.Length];
        int requiredCount = 0;
        for (int i = 0; i < positions.Length; i++)
        {
            if (!NavMesh.SamplePosition(spawns[i].transform.position, out NavMeshHit hit, 2f, NavMesh.AllAreas))
            {
                FailStart(scene);
                yield break;
            }
            positions[i] = hit.position;
            if (operation.MissionObjective == OperationDefinition.Objective.EliminateAll ||
                spawns[i].RequiredTarget)
                requiredCount++;
        }
        NavMeshAgent leader = player.GetComponent<NavMeshAgent>();
        if (requiredCount == 0 || !NavMesh.SamplePosition(layout.PlayerSpawn.position, out NavMeshHit playerHit, 2f, NavMesh.AllAreas) ||
            leader == null || !leader.Warp(playerHit.position))
        {
            FailStart(scene);
            yield break;
        }

        if (archerSelected)
            Spawn(archerPrefab, new Vector3(-1.4f, 0f, -1.4f));
        if (mageSelected)
            Spawn(magePrefab, new Vector3(1.4f, 0f, -1.4f));
        List<EnemyTarget> required = new List<EnemyTarget>(requiredCount);
        for (int i = 0; i < positions.Length; i++)
        {
            EnemyTarget prefab = spawns[i].EnemyPrefab != null ? spawns[i].EnemyPrefab : enemyPrefab;
            EnemyTarget enemy = Instantiate(prefab, positions[i], Quaternion.identity);
            bool objective = operation.MissionObjective == OperationDefinition.Objective.EliminateAll ||
                spawns[i].RequiredTarget;
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
        loading = false;
        mission.Begin(required.ToArray());
        player.enabled = true;
        Time.timeScale = 1f;
        RefreshUI();
    }

    private void FailStart(Scene scene)
    {
        Debug.LogError("Operation scene is missing a valid layout or NavMesh: " + operation.MissionScene);
        if (scene.IsValid() && scene.isLoaded)
            SceneManager.UnloadSceneAsync(scene);
        if (mapPreview != null)
            mapPreview.SetActive(true);
        loading = false;
        RefreshUI();
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
            switchMapButton.interactable = switchMapUnlockOperation == null ||
                OperationProgress.IsCompleted(switchMapUnlockOperation.MissionId);
            if (nextMapButton != null)
                nextMapButton.interactable = nextMapUnlockOperation == null ||
                    OperationProgress.IsCompleted(nextMapUnlockOperation.MissionId);
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
        startButton.interactable = !loading;
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
