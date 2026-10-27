using UnityEngine;

[CreateAssetMenu(menuName = "Tiny RPG/Ability")]
public class AbilityDefinition : ScriptableObject
{
    public enum TargetingType { None, Enemy }
    public enum EffectType { HeavySlash, BattleCharge, PiercingArrow, MultiShot, ArcaneBolt, ArcaneExplosion }

    [SerializeField] private string abilityId;
    [SerializeField] private string displayName;
    [TextArea, SerializeField] private string description;
    [Min(0), SerializeField] private float cooldown = 5f;
    [Min(0), SerializeField] private int damage;
    [Min(0), SerializeField] private float range;
    [Min(0), SerializeField] private float radius;
    [Min(0), SerializeField] private float castTime;
    [SerializeField] private TargetingType targeting;
    [SerializeField] private EffectType effect;
    [SerializeField] private string animationTrigger;
    [SerializeField] private GameObject visualEffect;
    [SerializeField] private AudioClip soundEffect;

    public string Id => abilityId;
    public string DisplayName => displayName;
    public string Description => description;
    public float Cooldown => cooldown;
    public int Damage => damage;
    public float Range => range;
    public float Radius => radius;
    public float CastTime => castTime;
    public TargetingType Targeting => targeting;
    public EffectType Effect => effect;
    public string AnimationTrigger => animationTrigger;
    public GameObject VisualEffect => visualEffect;
    public AudioClip SoundEffect => soundEffect;
}
