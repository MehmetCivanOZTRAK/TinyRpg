using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.UI;

public class MissionManager : MonoBehaviour
{
    public enum Result { InProgress, Completed, Failed }

    private Health[] enemies;
    private Health[] squad;
    [SerializeField] private GameObject objectivePanel;
    [SerializeField] private Text objectiveText;
    [SerializeField] private GameObject resultPanel;
    [SerializeField] private Text resultTitle;
    [SerializeField] private Text resultDetails;
    [SerializeField] private Text resultStars;
    [SerializeField] private Button retryButton;
    [SerializeField] private Button mapButton;
    private bool started;
    private float startTime;
    private int squadDeaths;

    public Result State { get; private set; }
    public int Total { get; private set; }
    public int Remaining { get; private set; }
    public int StarsEarned { get; private set; }

    private void Awake()
    {
        if (objectivePanel != null)
            objectivePanel.SetActive(false);
        if (resultPanel != null)
            resultPanel.SetActive(false);
        retryButton.onClick.AddListener(Retry);
        mapButton.onClick.AddListener(() => GetComponent<SquadPreparation>()?.ReturnToMap());
    }

    private void Start()
    {
        if (GetComponent<SquadPreparation>() == null)
            Begin();
    }

    public void Begin(EnemyTarget[] targets = null)
    {
        if (started)
            return;
        started = true;
        startTime = Time.time;
        if (targets == null)
            targets = FindObjectsByType<EnemyTarget>(FindObjectsSortMode.None);
        enemies = new Health[targets.Length];
        Total = Remaining = targets.Length;
        for (int i = 0; i < targets.Length; i++)
        {
            enemies[i] = targets[i].GetComponent<Health>();
            enemies[i].Died += OnEnemyDied;
        }

        CharacterCombat[] members = FindObjectsByType<CharacterCombat>(FindObjectsSortMode.None);
        squad = new Health[members.Length];
        for (int i = 0; i < members.Length; i++)
        {
            squad[i] = members[i].GetComponent<Health>();
            squad[i].Died += OnSquadDied;
        }

        if (Total == 0 || squad.Length == 0)
            Finish(Result.Failed);
        RefreshUI();
    }

    private void OnEnemyDied(Health _)
    {
        if (State != Result.InProgress)
            return;
        Remaining--;
        if (Remaining == 0)
            Finish(Result.Completed);
        else
            RefreshUI();
    }

    private void OnSquadDied(Health _)
    {
        if (State != Result.InProgress)
            return;
        squadDeaths++;
        foreach (Health member in squad)
            if (member != null && member.IsAlive)
                return;
        Finish(Result.Failed);
    }

    private void Finish(Result result)
    {
        State = result;
        if (result == Result.Completed)
        {
            OperationDefinition operation = GetComponent<SquadPreparation>()?.Operation;
            if (operation != null)
            {
                StarsEarned = operation.EvaluateStars(Time.time - startTime, squadDeaths);
                OperationProgress.Record(operation.MissionId, StarsEarned);
            }
        }
        PlayerController player = FindFirstObjectByType<PlayerController>();
        if (player != null)
            player.enabled = false;
        Time.timeScale = 0f;
        RefreshUI();
    }

    public void Retry()
    {
        GetComponent<SquadPreparation>()?.QueueRetry();
        Time.timeScale = 1f;
        SceneManager.LoadScene(SceneManager.GetActiveScene().name);
    }

    private void RefreshUI()
    {
        if (objectivePanel == null || resultPanel == null)
            return;
        objectivePanel.SetActive(started);
        resultPanel.SetActive(started && State != Result.InProgress);
        OperationDefinition current = GetComponent<SquadPreparation>()?.Operation;
        string objective = current != null && current.MissionObjective == OperationDefinition.Objective.BossEncounter ?
            "Defeat Shadow Warden: " :
            current != null && current.MissionObjective == OperationDefinition.Objective.ClearArea ?
            "Clear marked area: " : "Eliminate enemies: ";
        objectiveText.text = (current != null ? current.DisplayName : "Operation 1") + "\n" +
            objective + (Total - Remaining) + "/" + Total;
        if (State != Result.InProgress)
        {
            resultTitle.text = State != Result.Completed ? "Mission Failed" :
                current != null && current.FinalOperation ? "Campaign Completed" : "Mission Completed";
            resultDetails.text = "Enemies eliminated: " + (Total - Remaining) + "/" + Total;
            resultStars.text = "Stars: " + StarsEarned + "   Best: " +
                (current != null ? OperationProgress.BestStars(current.MissionId) : 0);
        }
    }

    private void OnDestroy()
    {
        if (enemies != null)
            foreach (Health enemy in enemies)
                if (enemy != null)
                    enemy.Died -= OnEnemyDied;
        if (squad != null)
            foreach (Health member in squad)
                if (member != null)
                    member.Died -= OnSquadDied;
        if (State != Result.InProgress)
            Time.timeScale = 1f;
    }
}
