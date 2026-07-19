using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using static Utils;
using DG.Tweening;
public class GemInfo
{   

    public enum GemType
    {
        Red = 1,
        Green = 2,
        Blue = 5,
        Gold = 10
    }

    public GemType gemType;
    public string SpriteName;
    public Vector3 GemScale;
    public int ExpAmount;

    public GemInfo(GemType _gemType, Vector3 _gemScale, string _spriteName = null, int? _expAmount = null)
    {
        gemType = _gemType;
        SpriteName = string.IsNullOrEmpty(_spriteName) ? $"{_gemType}Gem.sprite" : _spriteName;
        GemScale = _gemScale;
        switch(_gemType)
        {
            case GemType.Red :
                ExpAmount = Define.SMALL_GEM_EXP;
            break;
            case GemType.Green :
                ExpAmount = Define.GREEN_GEM_EXP;
            break;
            case GemType.Blue :
                ExpAmount = Define.BLUE_GEM_EXP;
            break;
            case GemType.Gold :
                ExpAmount = Define.YELLOW_GEM_EXP;
            break;
        }

        if (_expAmount.HasValue)
            ExpAmount = _expAmount.Value;
    }
}

public class GemController : DropItemController
{
    private const int GemSortingOrder = 150;
    private const float KickBackDistance = 3.0f;
    private const float KickBackDuration = 0.48f;
    private const float ReturnMoveSpeed = 22.0f;
    private const float CollectCompleteDistance = 0.85f;
    private const float ContactSparkleDuration = 0.22f;
    private static readonly Color SparkleColor = new Color(1.65f, 1.5f, 0.85f, 1f);
    private static readonly Color ContactSparkleColor = new Color(1.8f, 1.65f, 0.7f, 0.9f);

    GemInfo gemInfo;
    Coroutine coMoveToPlayer;
    bool isCollecting;
    bool hasGrantedExp;

    public override bool Init()
    {  
        itemType = Define.ItemType.Gem;
        if (!base.Init()) return false;

        return true;
    }

    public override void OnDisable()
    {
        base.OnDisable();

        if(coMoveToPlayer != null)
        {
            StopCoroutine(coMoveToPlayer);
            coMoveToPlayer = null;
        }

        isCollecting = false;
        hasGrantedExp = false;
    }

    public void SetInfo(GemInfo _gemInfo)
    {
        Init();
        gemInfo = _gemInfo;
        CollectDist = 3.2f;
        if(ItemSprite != null)
        {
            ItemSprite.sprite = Manager.ResourceM.Load<Sprite>(gemInfo.SpriteName);
            ItemSprite.sortingOrder = GemSortingOrder;
            ItemSprite.color = Color.white;
        }
        if (anim != null) anim.runtimeAnimatorController = null;

        transform.localScale = _gemInfo.GemScale;
    }

    public override void GetItem()
    {
        if (isCollecting)
            return;

        isCollecting = true;
        base.GetItem();
        GrantExp();
        if (coMoveToPlayer == null && this.IsValid())
        {
            SpawnContactSparkle();

            Vector3 dir = (transform.position - Manager.GameM.player.transform.position).normalized;
            if (dir == Vector3.zero)
                dir = UnityEngine.Random.insideUnitCircle.normalized;

            Vector3 target = transform.position + dir * KickBackDistance;
            Sequence sequence = DOTween.Sequence()
                .Append(transform.DOMove(target, KickBackDuration).SetEase(Ease.OutQuad));

            if (ItemSprite != null)
            {
                Sequence sparkle = DOTween.Sequence()
                    .Append(ItemSprite.DOColor(SparkleColor, 0.08f))
                    .Append(ItemSprite.DOColor(Color.white, 0.08f))
                    .SetLoops(2);

                sequence.Join(sparkle);
            }

            sequence
                .OnComplete(() =>
                {
                    if(this.IsValid())
                        coMoveToPlayer = StartCoroutine(CoMoveToPlayer());
                });
         }
    }

    public void CollectByPlayer()
    {
        GetItem();
    }

    void SpawnContactSparkle()
    {
        if (ItemSprite == null || ItemSprite.sprite == null)
            return;

        GameObject sparkle = new GameObject("GemContactSparkle");
        sparkle.transform.position = transform.position;
        sparkle.transform.localScale = transform.localScale * 0.45f;

        SpriteRenderer sparkleRenderer = sparkle.AddComponent<SpriteRenderer>();
        sparkleRenderer.sprite = ItemSprite.sprite;
        sparkleRenderer.sortingLayerID = ItemSprite.sortingLayerID;
        sparkleRenderer.sortingOrder = GemSortingOrder + 1;
        sparkleRenderer.color = ContactSparkleColor;

        DOTween.Sequence()
            .Append(sparkle.transform.DOScale(transform.localScale * 1.45f, ContactSparkleDuration).SetEase(Ease.OutQuad))
            .Join(sparkleRenderer.DOFade(0f, ContactSparkleDuration).SetEase(Ease.OutQuad))
            .OnComplete(() =>
            {
                if (sparkle != null)
                    Destroy(sparkle);
            });
    }

    IEnumerator CoMoveToPlayer()
    {
        float elapsed = 0f;
        while(this.IsValid())
        {
            float dist = Vector3.Distance(transform.position, Manager.GameM.player.transform.position);

            transform.position = Vector3.MoveTowards(transform.position, Manager.GameM.player.transform.position, Time.deltaTime * ReturnMoveSpeed);
            elapsed += Time.deltaTime;

            if(dist < CollectCompleteDistance || elapsed >= 3f)
            {
                string soundName = UnityEngine.Random.value > 0.5f ? "ExpGet_01" : "ExpGet_02";
                Manager.SoundM.Play(Define.Sound.Effect, soundName);
                Manager.ObjectM.DeSpawn(this);
                yield break;
            }

            yield return new WaitForFixedUpdate();
        }
    }

    void GrantExp()
    {
        if (hasGrantedExp || gemInfo == null || Manager.GameM?.player == null)
            return;

        hasGrantedExp = true;
        int beforeLevel = Manager.GameM.player.Level;
        float expBonusRate = Mathf.Max(1f, Manager.GameM.player.ExpBounsRate);
        Manager.GameM.player.Exp += gemInfo.ExpAmount * expBonusRate;
        (Manager.UiM.SceneUI as UI_GameScene)?.OnPlayerDataUpdated();
        if (Manager.GameM.player.Level > beforeLevel && !Manager.GameM.isGameEnd && Time.timeScale > 0f && Manager.GameM.player.Skills.HasSelectableSkillCandidates())
            Manager.UiM.ShowPopup<UI_SkillSelectPopup>();
    }
}
