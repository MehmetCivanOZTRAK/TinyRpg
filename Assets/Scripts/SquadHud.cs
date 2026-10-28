using UnityEngine;
using UnityEngine.UI;

public class SquadHud : MonoBehaviour
{
    [SerializeField] private GameObject hudRoot;
    [SerializeField] private GameObject[] rows;
    [SerializeField] private Text[] healthLabels;
    [SerializeField] private Image[] healthFills;
    [SerializeField] private Text[] abilityLabels;

    private SquadPreparation preparation;

    private void Awake()
    {
        preparation = FindFirstObjectByType<SquadPreparation>();
    }

    private void Update()
    {
        if (hudRoot == null)
            return;
        bool visible = preparation == null || preparation.Started;
        if (hudRoot.activeSelf != visible)
            hudRoot.SetActive(visible);
        if (!visible)
            return;

        int activeRows = 0;
        foreach (CharacterAbilityController abilities in FindObjectsByType<CharacterAbilityController>(FindObjectsSortMode.None))
        {
            if (!abilities.isActiveAndEnabled)
                continue;
            Health health = abilities.GetComponent<Health>();
            CharacterCombat combat = abilities.GetComponent<CharacterCombat>();
            if (health == null || combat == null)
                continue;
            int index = (int)combat.CharacterClass;
            if (index >= rows.Length)
                continue;

            activeRows |= 1 << index;
            healthLabels[index].text = combat.CharacterClass + "  " +
                health.CurrentHealth + "/" + health.MaxHealth + " HP";
            healthFills[index].rectTransform.localScale =
                new Vector3((float)health.CurrentHealth / health.MaxHealth, 1f, 1f);
            abilityLabels[index].text = "Q: " + Status(abilities, CharacterAbilityController.Slot.Q) +
                "   E: " + Status(abilities, CharacterAbilityController.Slot.E);
        }
        for (int i = 0; i < rows.Length; i++)
            if (rows[i].activeSelf != ((activeRows & (1 << i)) != 0))
                rows[i].SetActive((activeRows & (1 << i)) != 0);
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
