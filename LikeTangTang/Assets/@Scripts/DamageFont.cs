using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using TMPro;
using DG.Tweening;

public class DamageFont : MonoBehaviour
{
    private const int DamageSortingOrder = 1400;
    private static readonly Color NormalDamageColor = new Color(1f, 1f, 1f, 1f);
    private static readonly Color CriticalDamageColor = new Color(1f, 0.06f, 0.04f, 1f);
    private static readonly Color CriticalHotColor = new Color(1f, 0.42f, 0.12f, 1f);
    private static readonly Color HealColor = new Color(0.31f, 1f, 0.43f, 1f);
    private static readonly Color OutlineColor = new Color(0.08f, 0.06f, 0.05f, 1f);
    private static readonly Color CriticalOutlineColor = new Color(0.12f, 0f, 0f, 1f);

    TextMeshPro damageText;
    bool isCritical;

    public void SetInfo(Vector2 _pos, float _damage, float _heal, Transform _parent, bool _isCritical = false)
    {
        damageText = GetComponent<TextMeshPro>();
        isCritical = _isCritical;
        transform.position = _pos + Vector2.up * (_isCritical ? 0.45f : 0.25f);
        ApplyPremiumStyle(_isCritical);

        if (_heal > 0)
        {
            damageText.text = $"{Mathf.RoundToInt(_heal)}";
            damageText.color = HealColor;
        }
        else if (_isCritical)
        {
            damageText.text = $"CRIT! {Mathf.RoundToInt(_damage)}";
            damageText.color = CriticalDamageColor;
        }
        else
        {
            damageText.text = $"{Mathf.RoundToInt(_damage)}";
            damageText.color = NormalDamageColor;
        }

        damageText.alpha = 1;
        MeshRenderer renderer = GetComponent<MeshRenderer>();
        if (renderer != null)
           renderer.sortingOrder = DamageSortingOrder;

        DoAnim();
    }

    void ApplyPremiumStyle(bool _isCritical)
    {
        damageText.alignment = TextAlignmentOptions.Center;
        damageText.fontStyle = FontStyles.Bold;
        damageText.fontSize = _isCritical ? 10.2f : 7.2f;
        damageText.characterSpacing = _isCritical ? 4f : 0f;
        damageText.outlineColor = _isCritical ? CriticalOutlineColor : OutlineColor;
        damageText.outlineWidth = _isCritical ? 0.38f : 0.24f;
        damageText.enableWordWrapping = false;
        damageText.overflowMode = TextOverflowModes.Overflow;
    }

    void DoAnim()
    {
        var tr = transform;
        var text = tr.GetComponent<TMP_Text>();
        Sequence sq = DOTween.Sequence();
        transform.localScale = Vector3.zero;
        float popScale = isCritical ? 2.55f : 1.65f;
        float settleScale = isCritical ? 1.55f : 1.15f;
        Vector3 drift = Vector3.up * (isCritical ? 2.25f : 1.35f);
        drift.x = Random.Range(-0.35f, 0.35f);

        sq.Append(tr.DOScale(popScale, 0.16f).SetEase(Ease.OutBack))
            .Join(tr.DOMove(tr.position + drift * 0.35f, 0.16f).SetEase(Ease.OutQuad));

        if (isCritical)
            sq.Join(text.DOColor(CriticalHotColor, 0.12f).SetEase(Ease.OutQuad));

        sq.Append(tr.DOScale(settleScale, 0.18f).SetEase(Ease.OutQuad))
            .Join(tr.DOMove(tr.position + drift, 0.42f).SetEase(Ease.OutCubic));

        if (isCritical)
            sq.Join(text.DOColor(CriticalDamageColor, 0.18f).SetEase(Ease.InOutSine));

        sq
            .AppendInterval(isCritical ? 0.08f : 0.02f)
            .Append(text.DOFade(0, 0.22f).SetEase(Ease.InQuint))
            .OnComplete(() =>
            {
                Manager.ResourceM.Destory(gameObject);
            });
    }
}
