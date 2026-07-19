using UnityEngine;

[RequireComponent(typeof(Animator))]
public class NH_BasicAlienAnimatorBridge : MonoBehaviour
{
    private static readonly int SpeedHash = Animator.StringToHash("Speed");
    private static readonly int AttackHash = Animator.StringToHash("Attack");
    private static readonly int HitHash = Animator.StringToHash("Hit");
    private static readonly int DeadHash = Animator.StringToHash("Dead");
    private static readonly int SpawnDoneHash = Animator.StringToHash("SpawnDone");

    private Animator animator;

    private void Awake()
    {
        animator = GetComponent<Animator>();
    }

    public void SetSpeed(float speed)
    {
        animator.SetFloat(SpeedHash, speed);
    }

    public void PlayAttack()
    {
        animator.SetTrigger(AttackHash);
    }

    public void PlayHit()
    {
        animator.SetTrigger(HitHash);
    }

    public void SetDead(bool dead)
    {
        animator.SetBool(DeadHash, dead);
    }

    public void SetSpawnDone(bool spawnDone)
    {
        animator.SetBool(SpawnDoneHash, spawnDone);
    }
}
