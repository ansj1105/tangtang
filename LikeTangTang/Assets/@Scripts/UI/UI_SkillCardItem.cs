using System.Collections;
using System.Collections.Generic;
using Unity.VisualScripting;
using UnityEngine;
using UnityEngine.UI;
using DG.Tweening;

public class UI_SkillCardItem : UI_Base
{
    const string PremiumStarGlowName = "PremiumStarGlow";
    static readonly Color StarOnColor = new Color(0.982f, 1f, 0f, 1f);
    static readonly Color PremiumStarColor = new Color(1f, 0.95f, 0.28f, 1f);
    static readonly Color PremiumStarHotColor = new Color(1f, 0.58f, 0.05f, 1f);
    static readonly Color PremiumGlowColor = new Color(1f, 0.72f, 0.08f, 0.55f);

    /*[x] : 어떤 스킬?, 몇 레벨?, 데이트 시트, Set, ClickItem
    */

    enum Images
    {
        EvoSkillINeedITemImage,
        SkillImage,

    }

    enum Texts
    {
        SkillDescriptionText,
        CardNameText
    }

    enum GameObjects
    {
        StarOn_0,
        StarOn_1,
        StarOn_2,
        StarOn_3,
        StarOn_4,
        StarOff_0,
        StarOff_1,
        StarOff_2,
        StarOff_3,
        StarOff_4,
        NewIImageObject,
        EvoSkillInfoObject
    }

    enum Buttons
    {
        SkillCardBackgroundImage
    }

    public int templateID;
    public Data.SkillData skillData;
    GameManager gm;
    readonly List<Tween> premiumStarTweens = new List<Tween>();
    public override bool Init()
    {
        if(!base.Init()) return false;
        gm = Manager.GameM;
        gameObjectsType = typeof(GameObjects);
        ButtonsType = typeof(Buttons);
        TextsType = typeof(Texts);
        ImagesType = typeof(Images);


        BindObject(gameObjectsType);
        BindButton(ButtonsType);
        BindText(TextsType);
        BindImage(ImagesType);

        GetObject(gameObjectsType, (int)GameObjects.EvoSkillInfoObject).SetActive(false);
        GetObject(gameObjectsType, (int)GameObjects.NewIImageObject).gameObject.SetActive(false);
        StopPremiumStarEffect();

        for(int i =0; i<Define.MAX_SKILL_LEVEL; i++)
        {
            GetObject(gameObjectsType, (int)GameObjects.StarOn_0 + i).SetActive(false);
            GetObject(gameObjectsType, (int)GameObjects.StarOff_0 + i).SetActive(true);
        }
        
        GetButton(ButtonsType, (int)Buttons.SkillCardBackgroundImage).gameObject.BindEvent(OnClickItem);

        transform.localScale = Vector3.one;

        skill = null;
        evolutionItemID = 0;
        return true;
    }


    //NOTE : 버튼을 찾아서 동적으로 클릭함수를 넣어주는 코드(인자가 있으면 람다로 해줘야함)
    public void Click()
    {

    }
    public void Click(int a)
    {

    }
    SkillBase skill;
    int evolutionItemID;
    public void SetInfo(SkillBase _skill = null, int _evolutionItemID = 0)
    {
        StopPremiumStarEffect();
        skill = null;
        evolutionItemID = 0;
        if (_skill != null)
        {
            skill = _skill;

            GetObject(gameObjectsType, (int)GameObjects.NewIImageObject).gameObject.SetActive(false);
            for(int i =0; i<Define.MAX_SKILL_LEVEL; i++)
            {
                GetObject(gameObjectsType, (int)GameObjects.StarOn_0 + i).SetActive(false);
            }


            if (skill.SkillLevel == 0)
            {
                GetObject(gameObjectsType, (int)GameObjects.NewIImageObject).gameObject.SetActive(true);
            }

            GetText(TextsType, (int)Texts.CardNameText).text = $"{skill.SkillDatas.SkillName}";
            GetText(TextsType, (int)Texts.SkillDescriptionText).text = $"{skill.SkillDatas.SkillDescription}";
            GetImage(ImagesType, (int)Images.SkillImage).sprite = Manager.ResourceM.Load<Sprite>(skill.SkillDatas.SkillIcon);
            GetObject(gameObjectsType, (int)GameObjects.EvoSkillInfoObject).gameObject.SetActive(true);
            GetImage(ImagesType, (int)Images.EvoSkillINeedITemImage).sprite = Manager.ResourceM.Load<Sprite>(Manager.DataM.SkillEvolutionDic[skill.SkillDatas.EvolutionItemID].EvolutionItemIcon);

            for (int i =0; i< Define.MAX_SKILL_LEVEL; i++)
            {
                GetObject(gameObjectsType, (int)GameObjects.StarOff_0 + i).SetActive(true);
            }
            for (int i = 0; i < skill.SkillLevel; i++)
            {
                GetObject(gameObjectsType, (int)GameObjects.StarOn_0 + i).SetActive(true);
            }

            StartPremiumStarEffect(skill.SkillLevel);
        }
        else if(_evolutionItemID != 0)
        {
            evolutionItemID = _evolutionItemID;
            Data.SkillEvolutionData evoData = Manager.DataM.SkillEvolutionDic[_evolutionItemID];


            GetObject(gameObjectsType, (int)GameObjects.NewIImageObject).gameObject.SetActive(true);
            GetImage(ImagesType, (int)Images.SkillImage).sprite = Manager.ResourceM.Load<Sprite>(Manager.DataM.SkillEvolutionDic[_evolutionItemID].EvolutionItemIcon);
            GetText(TextsType, (int)Texts.CardNameText).text = $"{evoData.EvolutionItemName}";
            GetText(TextsType, (int)Texts.SkillDescriptionText).text = $"{evoData.EvolutionItemDescription}";


            for(int i =0; i< Define.MAX_SKILL_LEVEL; i++)
            {
                GetObject(gameObjectsType, (int)GameObjects.StarOn_0 + i).SetActive(false);
                GetObject(gameObjectsType, (int)GameObjects.StarOff_0 + i).SetActive(false);
            }
        }
    }

    void StartPremiumStarEffect(int nextStarIndex)
    {
        if (nextStarIndex < 0 || nextStarIndex >= Define.MAX_SKILL_LEVEL)
            return;

        GameObject starOn = GetObject(gameObjectsType, (int)GameObjects.StarOn_0 + nextStarIndex);
        GameObject starOff = GetObject(gameObjectsType, (int)GameObjects.StarOff_0 + nextStarIndex);
        if (starOn == null)
            return;

        starOff?.SetActive(true);
        starOn.SetActive(true);

        Image starImage = starOn.GetComponent<Image>();
        if (starImage != null)
        {
            starImage.color = PremiumStarColor;
            premiumStarTweens.Add(starImage.DOColor(PremiumStarHotColor, 0.42f)
                .SetLoops(-1, LoopType.Yoyo)
                .SetEase(Ease.InOutSine)
                .SetUpdate(true));
        }

        Transform starTransform = starOn.transform;
        Vector3 baseScale = Vector3.one;
        starTransform.localScale = baseScale;
        starTransform.localRotation = Quaternion.identity;

        premiumStarTweens.Add(starTransform.DOScale(baseScale * 1.34f, 0.42f)
            .SetLoops(-1, LoopType.Yoyo)
            .SetEase(Ease.InOutSine)
            .SetUpdate(true));

        premiumStarTweens.Add(starTransform.DOLocalRotate(new Vector3(0f, 0f, 8f), 0.56f)
            .SetLoops(-1, LoopType.Yoyo)
            .SetEase(Ease.InOutSine)
            .SetUpdate(true));

        CreatePremiumStarGlow(starOn, starImage);
    }

    void CreatePremiumStarGlow(GameObject starOn, Image sourceImage)
    {
        if (sourceImage == null)
            return;

        Transform oldGlow = starOn.transform.Find(PremiumStarGlowName);
        if (oldGlow != null)
            Destroy(oldGlow.gameObject);

        GameObject glowObject = new GameObject(PremiumStarGlowName, typeof(RectTransform), typeof(CanvasRenderer), typeof(Image));
        glowObject.transform.SetParent(starOn.transform, false);

        RectTransform sourceRect = starOn.GetComponent<RectTransform>();
        RectTransform glowRect = glowObject.GetComponent<RectTransform>();
        glowRect.anchorMin = new Vector2(0.5f, 0.5f);
        glowRect.anchorMax = new Vector2(0.5f, 0.5f);
        glowRect.pivot = new Vector2(0.5f, 0.5f);
        glowRect.anchoredPosition = Vector2.zero;
        glowRect.sizeDelta = sourceRect != null ? sourceRect.sizeDelta * 1.95f : new Vector2(80f, 80f);

        Image glowImage = glowObject.GetComponent<Image>();
        glowImage.raycastTarget = false;
        glowImage.sprite = sourceImage.sprite;
        glowImage.preserveAspect = true;
        glowImage.color = PremiumGlowColor;

        premiumStarTweens.Add(glowImage.DOFade(0.12f, 0.42f)
            .SetLoops(-1, LoopType.Yoyo)
            .SetEase(Ease.InOutSine)
            .SetUpdate(true));

        premiumStarTweens.Add(glowObject.transform.DOScale(Vector3.one * 1.25f, 0.42f)
            .SetLoops(-1, LoopType.Yoyo)
            .SetEase(Ease.InOutSine)
            .SetUpdate(true));
    }

    void StopPremiumStarEffect()
    {
        for (int i = 0; i < premiumStarTweens.Count; i++)
        {
            if (premiumStarTweens[i] != null && premiumStarTweens[i].IsActive())
                premiumStarTweens[i].Kill();
        }
        premiumStarTweens.Clear();

        if (gameObjectsType == null)
            return;

        for (int i = 0; i < Define.MAX_SKILL_LEVEL; i++)
        {
            GameObject starOn = GetObject(gameObjectsType, (int)GameObjects.StarOn_0 + i);
            if (starOn == null)
                continue;

            starOn.transform.localScale = Vector3.one;
            starOn.transform.localRotation = Quaternion.identity;

            Image starImage = starOn.GetComponent<Image>();
            if (starImage != null)
                starImage.color = StarOnColor;

            Transform glow = starOn.transform.Find(PremiumStarGlowName);
            if (glow != null)
                Destroy(glow.gameObject);
        }
    }

    void OnDisable()
    {
        StopPremiumStarEffect();
    }

    public void OnClickItem()
    {
        Manager.SoundM.PlayButtonClick();
        if (skill != null)
        {
            Manager.GameM.player.Skills.LevelUpSkill(skill.Skilltype);
        }
        else if (evolutionItemID != 0)
        {
            Manager.GameM.player.Skills.TryEvolveSkill(evolutionItemID);
        }

        Manager.TimeM.TimeReStart();
        Manager.UiM.ClosePopup();
    }

}
