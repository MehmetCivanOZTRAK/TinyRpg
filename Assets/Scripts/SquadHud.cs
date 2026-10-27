using UnityEngine;

public class SquadHud : MonoBehaviour
{
    private GUIStyle label;

    private void OnGUI()
    {
        Matrix4x4 previousMatrix = GUI.matrix;
        float scale = Mathf.Max(1f, Mathf.Min(Screen.width / 960f, Screen.height / 540f));
        GUI.matrix = Matrix4x4.Scale(new Vector3(scale, scale, 1f)) * previousMatrix;
        if (label == null)
        {
            label = new GUIStyle(GUI.skin.label);
            label.fontSize = 15;
            label.normal.textColor = Color.white;
        }

        int row = 0;
        foreach (CharacterAbilityController abilities in FindObjectsByType<CharacterAbilityController>(FindObjectsSortMode.None))
        {
            if (!abilities.isActiveAndEnabled)
                continue;
            Health health = abilities.GetComponent<Health>();
            CharacterCombat combat = abilities.GetComponent<CharacterCombat>();
            if (health == null || combat == null)
                continue;

            Rect panel = new Rect(16, 16 + row++ * 76, 270, 68);
            GUI.Box(panel, GUIContent.none);
            GUI.Label(new Rect(panel.x + 10, panel.y + 4, 245, 22),
                combat.CharacterClass + "  " + health.CurrentHealth + "/" + health.MaxHealth + " HP", label);
            Rect bar = new Rect(panel.x + 10, panel.y + 29, 250, 9);
            GUI.DrawTexture(bar, Texture2D.whiteTexture);
            GUI.color = health.IsAlive ? new Color(0.2f, 0.8f, 0.35f) : Color.gray;
            GUI.DrawTexture(new Rect(bar.x, bar.y, bar.width * health.CurrentHealth / health.MaxHealth, bar.height),
                Texture2D.whiteTexture);
            GUI.color = Color.white;
            GUI.Label(new Rect(panel.x + 10, panel.y + 42, 250, 22),
                "Q: " + Status(abilities, CharacterAbilityController.Slot.Q) +
                "   E: " + Status(abilities, CharacterAbilityController.Slot.E), label);
        }
        GUI.matrix = previousMatrix;
    }

    private static string Status(CharacterAbilityController abilities, CharacterAbilityController.Slot slot)
    {
        AbilityDefinition definition = slot == CharacterAbilityController.Slot.Q ? abilities.QAbility : abilities.EAbility;
        if (definition == null)
            return "Locked";
        float remaining = abilities.CooldownRemaining(slot);
        return remaining <= 0f ? "Ready" : remaining.ToString("0.0") + "s";
    }
}
