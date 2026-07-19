using System.Collections;
using UnityEngine;

public class NHBasicAlienMonsterController : MonsterController
{
    private static readonly int DeadHash = Animator.StringToHash("Dead");

    private const float DeathClipDuration = 1.4f;

    public override bool Init()
    {
        bool result = base.Init();
        if (CreatureAnim != null)
        {
            CreatureAnim.SetBool(DeadHash, false);
            CreatureAnim.Play("Move", 0, 0f);
        }

        return result;
    }

    public override void OnDead()
    {
        CreatureState = Define.CreatureState.Dead;
        isStartDamageAnim = false;
        StopAllCoroutines();
        transform.localScale = Vector3.one * MonsterScale;

        if (Rigid != null)
        {
            Rigid.velocity = Vector2.zero;
            Rigid.simulated = false;
        }

        if (CreatureAnim != null)
        {
            CreatureAnim.SetBool(DeadHash, true);
        }

        InvokeMonsterData();
        Manager.GameM.RegisterMonsterKill();

        if (objType == Define.ObjectType.Monster && Manager.GameM.CurrentWaveData != null && UnityEngine.Random.value >= Manager.GameM.CurrentWaveData.NonDropRate)
        {
            GemController gem = Manager.ObjectM.Spawn<GemController>(transform.position, _prefabName: Define.DROPITEMNAME);
            gem.SetInfo(isRedTintedMonster ? Manager.GameM.GetEpicRedGemInfo() : Manager.GameM.GetGemInfo());
        }

        StartCoroutine(CoDespawnAfterDeath());
    }

    private IEnumerator CoDespawnAfterDeath()
    {
        yield return new WaitForSeconds(DeathClipDuration);
        Manager.ObjectM.DeSpawn(this);
    }
}
