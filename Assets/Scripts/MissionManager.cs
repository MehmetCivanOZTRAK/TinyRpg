using UnityEngine;
using UnityEngine.SceneManagement;

public class MissionManager : MonoBehaviour
{
    public enum Result { InProgress, Completed, Failed }

    private Health[] enemies;
    private Health[] squad;
    private GUIStyle label;

    public Result State { get; private set; }
    public int Total { get; private set; }
    public int Remaining { get; private set; }

    private void Start()
    {
        EnemyTarget[] targets = FindObjectsByType<EnemyTarget>(FindObjectsSortMode.None);
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

        // shortcut: This first operation has scene-placed enemies; use spawn registration for dynamic missions.
        if (Total == 0 || squad.Length == 0)
            Finish(Result.Failed);
    }

    private void OnEnemyDied(Health _)
    {
        if (State != Result.InProgress)
            return;
        Remaining--;
        if (Remaining == 0)
            Finish(Result.Completed);
    }

    private void OnSquadDied(Health _)
    {
        if (State != Result.InProgress)
            return;
        foreach (Health member in squad)
            if (member != null && member.IsAlive)
                return;
        Finish(Result.Failed);
    }

    private void Finish(Result result)
    {
        State = result;
        PlayerController player = FindFirstObjectByType<PlayerController>();
        if (player != null)
            player.enabled = false;
        Time.timeScale = 0f;
    }

    public void Retry()
    {
        Time.timeScale = 1f;
        SceneManager.LoadScene(SceneManager.GetActiveScene().name);
    }

    private void OnGUI()
    {
        Matrix4x4 previousMatrix = GUI.matrix;
        float scale = Mathf.Max(1f, Mathf.Min(Screen.width / 960f, Screen.height / 540f));
        GUI.matrix = Matrix4x4.Scale(new Vector3(scale, scale, 1f)) * previousMatrix;
        if (label == null)
        {
            label = new GUIStyle(GUI.skin.label);
            label.fontSize = 18;
            label.alignment = TextAnchor.MiddleCenter;
            label.normal.textColor = Color.white;
        }

        GUI.Box(new Rect(16, 96, 300, 68), GUIContent.none);
        GUI.Label(new Rect(24, 101, 284, 58),
            "Operation 1\nEliminate enemies: " + (Total - Remaining) + "/" + Total, label);
        if (State != Result.InProgress)
        {
            GUI.Box(new Rect(300, 175, 360, 190), GUIContent.none);
            GUI.Label(new Rect(320, 193, 320, 45),
                State == Result.Completed ? "Mission Completed" : "Mission Failed", label);
            GUI.Label(new Rect(320, 245, 320, 35),
                "Enemies eliminated: " + (Total - Remaining) + "/" + Total, label);
            if (GUI.Button(new Rect(385, 300, 190, 42), "Retry"))
                Retry();
        }
        GUI.matrix = previousMatrix;
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
