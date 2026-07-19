using System.Collections;
using UnityEngine;

public class NHBasicAlienMonsterController : MonsterController
{
    private static readonly int SpeedHash = Animator.StringToHash("Speed");
    private static readonly int AttackHash = Animator.StringToHash("Attack");
    private static readonly int HitHash = Animator.StringToHash("Hit");
    private static readonly int DeadHash = Animator.StringToHash("Dead");
    private static readonly int SpawnDoneHash = Animator.StringToHash("SpawnDone");

    private const float AttackAnimInterval = 0.5f;
    private const float SpawnClipDuration = 0.34f;
    private const float DeathClipDuration = 0.65f;
    private float nextAttackAnimTime;

    public override bool Init()
    {
        bool result = base.Init();
        if (CreatureAnim != null)
        {
            CreatureAnim.SetBool(DeadHash, false);
            CreatureAnim.ResetTrigger(AttackHash);
            CreatureAnim.ResetTrigger(HitHash);
            CreatureAnim.SetBool(SpawnDoneHash, false);
            CreatureAnim.Play("Spawn", 0, 0f);
            StartCoroutine(CoFinishSpawn());
        }

        return result;
    }

    public override void Tick(float deltaTime)
    {
        base.Tick(deltaTime);

        if (CreatureAnim != null && Rigid != null)
            CreatureAnim.SetFloat(SpeedHash, Rigid.velocity.sqrMagnitude);
    }

    public override void OnCollisionEnter2D(Collision2D collision)
    {
        PlayAttackAnim();
        base.OnCollisionEnter2D(collision);
    }

    private void OnCollisionStay2D(Collision2D collision)
    {
        if (collision.gameObject.GetComponent<PlayerController>() == null)
            return;

        PlayAttackAnim();
    }

    public override void OnDamaged(BaseController attacker, SkillBase skill = null, float damage = 0)
    {
        if (CreatureAnim != null && Hp > 0)
        {
            CreatureAnim.ResetTrigger(HitHash);
            CreatureAnim.SetTrigger(HitHash);
        }

        base.OnDamaged(attacker, skill, damage);
    }

    public override void OnDead()
    {
        CreatureState = Define.CreatureState.Dead;
        isStartDamageAnim = false;
        StopAllCoroutines();

        if (Rigid != null)
        {
            Rigid.velocity = Vector2.zero;
            Rigid.simulated = false;
        }

        if (CreatureAnim != null)
        {
            CreatureAnim.ResetTrigger(HitHash);
            CreatureAnim.SetBool(DeadHash, true);
        }

        InvokeMonsterData();
        Manager.GameM.player.KillCount++;
        Manager.GameM.TotalMonsterKillCount++;

        if (objType == Define.ObjectType.Monster && UnityEngine.Random.value >= Manager.GameM.CurrentWaveData.NonDropRate)
        {
            GemController gem = Manager.ObjectM.Spawn<GemController>(transform.position, _prefabName: Define.DROPITEMNAME);
            gem.SetInfo(Manager.GameM.GetGemInfo());
        }

        StartCoroutine(CoDespawnAfterDeath());
    }

    private IEnumerator CoDespawnAfterDeath()
    {
        yield return new WaitForSeconds(DeathClipDuration);
        Manager.ObjectM.DeSpawn(this);
    }

    private IEnumerator CoFinishSpawn()
    {
        yield return new WaitForSeconds(SpawnClipDuration);
        if (CreatureAnim != null)
            CreatureAnim.SetBool(SpawnDoneHash, true);
    }

    private void PlayAttackAnim()
    {
        if (CreatureAnim == null || Hp <= 0 || Time.time < nextAttackAnimTime)
            return;

        nextAttackAnimTime = Time.time + AttackAnimInterval;
        CreatureAnim.ResetTrigger(AttackHash);
        CreatureAnim.SetTrigger(AttackHash);
    }
}
