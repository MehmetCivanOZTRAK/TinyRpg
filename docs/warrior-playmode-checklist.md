# TINY-7 Warrior Play Mode checklist

Open `Assets/Scene/GameScene.unity` in Unity 6000.3.12f1 and enter Play Mode.

- Click reachable ground: Warrior runs, the click marker appears, and the camera continues to follow.
- Let Warrior reach the destination: the Animator returns to Idle without sliding.
- Set the `Attack` trigger in the Animator window: the normal sword attack plays and returns to Idle or Run as appropriate. Combat input is covered by a later task.
- Inspect the humanoid rig during Idle, Run and Attack: limbs do not twist, stretch or detach.
- Inspect the sword during the attack from several camera angles: it remains in the correct hand without visible offset or severe clipping.

Record the result of each visual check in TINY-7. Batchmode validation cannot establish visual alignment.
