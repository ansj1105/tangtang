using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using TMPro;
using UnityEngine.UI;
using Unity.VisualScripting;
using DG.Tweening;
using System.Linq;
using System;


public class UI_GameScene : UI_Scene
{
    private const string RunGoldObjectName = "RunGoldObject";
    private static readonly Color ExpFillColor = new Color(0.58f, 1f, 0f, 1f);
    private static readonly Color ExpGlowColor = new Color(0.86f, 1f, 0.16f, 1f);
    private static readonly Color ExpBackgroundColor = new Color(0.055f, 0.07f, 0.095f, 1f);
    private static readonly Color ExpFlashColor = new Color(0.94f, 1f, 0.34f, 1f);
    private static readonly Color ExpBorderColor = new Color(0f, 0f, 0f, 1f);
    private TextMeshProUGUI runGoldValueText;
    private Slider expSlider;
    private Image expFillImage;
    private Image expBackgroundImage;
    private Image expSparkImage;
    private DG.Tweening.Tween expSliderTween;
    private DG.Tweening.Sequence expGlowSequence;
    private int topHudReapplyFrames;

    enum GameObjects
    {
        WaveObject,
        BossInfoObject,
        EliteInfoObject,
        MonsterAlarmObject,
        BossAlarmObject,

        #region Test
        DevMenu,
        #endregion
    }

    enum Images
    {
        WhiteFlash,
        OnDamaged,
        BattleSkilI_Icon_1,
        BattleSkilI_Icon_2,
        BattleSkilI_Icon_3,
        BattleSkilI_Icon_4,
        BattleSkilI_Icon_5,
        BattleSkilI_Icon_6,
        EvolutionItem_Icon_1,
        EvolutionItem_Icon_2,
        EvolutionItem_Icon_3,
        EvolutionItem_Icon_4,
        EvolutionItem_Icon_5,
        EvolutionItem_Icon_6,
    }

    public enum Texts
    {
        KillValueText,
        CharacterLevelValueText,
        WaveValueText,
        TimeLimitValueText,
        BossNameValueText,
        EliteNameValueText
    }
    public enum Sliders
    {
        ExpSliderObject,
        BossHpSliderObject,
        EliteHpSliderObject,
    }

    public enum Buttons
    {
        PauseButton,
        HealButton,
        #region Test
        HiddenButton,
        MonsterAllKillButton,
        NextWaveButton,
        LevelUpButton
        #endregion
    }

    enum AlramType
    {
        Wave,
        Boss
    }

    private void Awake()
    {
        Init();
        Manager.GameM.player.Skills.UpdateSkillUI -= OnLevelUpSkillUI;
        Manager.GameM.player.Skills.UpdateSkillUI += OnLevelUpSkillUI;
    }

    private void OnDestroy()
    {
        expSliderTween?.Kill();
        expGlowSequence?.Kill();
    }

    private void OnEnable()
    {
        if (isInit)
            topHudReapplyFrames = 30;
    }

    private void LateUpdate()
    {
        if (topHudReapplyFrames <= 0)
            return;

        topHudReapplyFrames--;
        ConfigureBattleCounterLayout();
    }

    public override bool Init()
    {
        if (!base.Init()) return false;

        TextsType = typeof(Texts);
        ButtonsType = typeof(Buttons);
        SlidersType = typeof(Sliders);
        gameObjectsType = typeof(GameObjects);
        ImagesType = typeof(Images);

        BindText(TextsType);
        BindButton(ButtonsType);
        BindSlider(SlidersType);
        BindObject(gameObjectsType);
        BindImage(ImagesType);

        GetButton(ButtonsType, (int)Buttons.PauseButton).gameObject.BindEvent(OnClickPauseButton);
        GetButton(ButtonsType, (int)Buttons.HealButton).gameObject.BindEvent(OnClickHealButton);
        #region Test
        GetButton(ButtonsType, (int)Buttons.HiddenButton).gameObject.BindEvent(OnClickHiddenButton);
        GetButton(ButtonsType, (int)Buttons.MonsterAllKillButton).gameObject.BindEvent(OnClickMonsterAllKillButton);
        GetButton(ButtonsType, (int)Buttons.NextWaveButton).gameObject.BindEvent(OnClickNextWaveButton);
        GetButton(ButtonsType, (int)Buttons.LevelUpButton).gameObject.BindEvent(OnClickLevelUpButton);
        #endregion
        GetObject(gameObjectsType, (int)GameObjects.BossInfoObject).SetActive(false);
        GetObject(gameObjectsType, (int)GameObjects.EliteInfoObject).SetActive(false);
        GetObject(gameObjectsType, (int)GameObjects.MonsterAlarmObject).SetActive(false);
        GetObject(gameObjectsType, (int)GameObjects.BossAlarmObject).SetActive(false);
        GetObject(gameObjectsType, (int)GameObjects.DevMenu).SetActive(false);
        GetObject(gameObjectsType, (int)GameObjects.WaveObject).SetActive(false);
        HideBattleSkillHud();
        EnsureRunGoldUI();
        ConfigureBattleCounterLayout();
        ConfigureTopTimeLayout();
        MakeTopBarTransparent();
        ConfigureExpSliderLayout();
        ConfigurePremiumTopHud();
        OnPlayerDataUpdated();
        topHudReapplyFrames = 30;

        Manager.GameM.player.OnPlayerDataUpdated = OnPlayerDataUpdated;
        Manager.GameM.player.OnPlayerLevelUp = OnPlayerLevelUp;
        Manager.GameM.player.OnPlayerDamaged = OnDamaged;

        GetButton(ButtonsType, (int)Buttons.HealButton).interactable = false;
        Refresh();

        return true;
    }

    void Refresh()
    {
        GameObject waveObject = GetObject(gameObjectsType, (int)GameObjects.WaveObject);
        if (waveObject != null && waveObject.activeInHierarchy)
            LayoutRebuilder.ForceRebuildLayoutImmediate(waveObject.GetComponent<RectTransform>());
    }
    public void OnWaveStart(int _currentStageIndex)
    {
        GetObject(gameObjectsType, (int)GameObjects.WaveObject).SetActive(false);
    }

    public void OnWaveEnd()
    {
        GetObject(gameObjectsType, (int)GameObjects.MonsterAlarmObject).SetActive(false);
    }

    public void OnChangeSecond(int _minute, int _second)
    {
        if (_second == 3 && Manager.GameM.CurrentWaveIndex < 9)
        {
            //TOOD : 알람
            StartCoroutine(SwitchAlarm(AlramType.Wave));
        }

        if (Manager.GameM.CurrentWaveData?.BossMonsterID != null &&
            Manager.GameM.CurrentWaveData.BossMonsterID.Count > 0)
        {
            int bossGenTime = Define.BOSS_GEN_TIME;
            if (_second == bossGenTime)
                StartCoroutine(SwitchAlarm(AlramType.Boss));
        }

        //TODO : 이거 지우던가, 수정하던가 하기(그냥 남은 시간만 표기하게 하는게 나을거같음)
        int elapsedSeconds = Mathf.FloorToInt(Manager.GameM.ElapsedTime);
        int elapsedMinute = elapsedSeconds / 60;
        int elapsedSecond = elapsedSeconds % 60;
        GetText(typeof(Texts), (int)Texts.TimeLimitValueText).text = $"{elapsedMinute}:{elapsedSecond:D2}";
    }

    public void OnPlayerDataUpdated()
    {
        SetExpSliderImmediate(Manager.GameM.player.ExpRatio);
        GetText(typeof(Texts), (int)Texts.KillValueText).text = $"{Manager.GameM.player.KillCount}";
        GetText(typeof(Texts), (int)Texts.CharacterLevelValueText).text = $"LV.{Manager.GameM.player.Level}";
        RefreshRunGoldUI();
    }

    void EnsureRunGoldUI()
    {
        if (runGoldValueText != null)
            return;

        TextMeshProUGUI killText = GetText(typeof(Texts), (int)Texts.KillValueText);
        if (killText == null || killText.transform.parent == null)
            return;

        Transform killGroup = killText.transform.parent;
        Transform battleInfo = killGroup.parent;
        Transform parent = battleInfo != null ? battleInfo : killGroup;
        Transform existing = parent.Find(RunGoldObjectName);
        if (existing != null)
        {
            runGoldValueText = existing.GetComponentInChildren<TextMeshProUGUI>();
            ConfigureRunGoldObject(existing.gameObject);
            existing.SetSiblingIndex(0);
            return;
        }

        GameObject goldObject = new GameObject(RunGoldObjectName, typeof(RectTransform), typeof(HorizontalLayoutGroup), typeof(ContentSizeFitter));
        goldObject.transform.SetParent(parent, false);
        goldObject.transform.SetSiblingIndex(0);

        CreateRunGoldIcon(goldObject.transform);
        runGoldValueText = CreateRunGoldValueText(goldObject.transform, killText);
        ConfigureRunGoldObject(goldObject);

        if (parent is RectTransform parentRect)
            LayoutRebuilder.ForceRebuildLayoutImmediate(parentRect);
    }

    void ConfigureRunGoldObject(GameObject goldObject)
    {
        if (goldObject == null)
            return;

        RectTransform goldRect = goldObject.GetComponent<RectTransform>();
        if (goldRect != null)
        {
            goldRect.anchorMin = new Vector2(1f, 1f);
            goldRect.anchorMax = new Vector2(1f, 1f);
            goldRect.pivot = new Vector2(1f, 1f);
            goldRect.sizeDelta = new Vector2(152f, 46f);
        }

        Image background = EnsureImage(goldObject);
        background.color = new Color(0f, 0f, 0f, 0.92f);
        background.raycastTarget = false;

        HorizontalLayoutGroup layout = goldObject.GetComponent<HorizontalLayoutGroup>();
        if (layout == null)
            layout = goldObject.AddComponent<HorizontalLayoutGroup>();

        layout.childAlignment = TextAnchor.MiddleRight;
        layout.padding = new RectOffset(3, 5, 2, 2);
        layout.childControlWidth = false;
        layout.childControlHeight = false;
        layout.childForceExpandWidth = false;
        layout.childForceExpandHeight = false;
        layout.spacing = 3f;

        ContentSizeFitter fitter = goldObject.GetComponent<ContentSizeFitter>();
        if (fitter == null)
            fitter = goldObject.AddComponent<ContentSizeFitter>();

        fitter.horizontalFit = ContentSizeFitter.FitMode.Unconstrained;
        fitter.verticalFit = ContentSizeFitter.FitMode.Unconstrained;

        LayoutElement layoutElement = goldObject.GetComponent<LayoutElement>();
        if (layoutElement == null)
            layoutElement = goldObject.AddComponent<LayoutElement>();

        layoutElement.preferredWidth = 152f;
        layoutElement.preferredHeight = 46f;

        foreach (Image image in goldObject.GetComponentsInChildren<Image>(true))
        {
            if (image.gameObject == goldObject)
                continue;

            RectTransform imageRect = image.GetComponent<RectTransform>();
            if (imageRect != null)
                imageRect.sizeDelta = new Vector2(48f, 48f);

            LayoutElement imageLayout = image.GetComponent<LayoutElement>();
            if (imageLayout == null)
                imageLayout = image.gameObject.AddComponent<LayoutElement>();
            imageLayout.preferredWidth = 48f;
            imageLayout.preferredHeight = 48f;
        }

        foreach (TextMeshProUGUI text in goldObject.GetComponentsInChildren<TextMeshProUGUI>(true))
            ConfigureCounterValueText(text, Color.white);

        ConfigureCounterGroupManual(goldObject.transform, null, runGoldValueText, true);
    }

    void CreateRunGoldIcon(Transform parent)
    {
        GameObject iconObject = new GameObject("RunGoldImage", typeof(RectTransform), typeof(CanvasRenderer), typeof(Image), typeof(LayoutElement));
        iconObject.transform.SetParent(parent, false);

        Image icon = iconObject.GetComponent<Image>();
        icon.raycastTarget = false;
        icon.preserveAspect = true;
        icon.sprite = Manager.ResourceM.Load<Sprite>(Manager.DataM.MaterialDic[Define.ID_GOLD].SpriteName);
        icon.color = new Color(1f, 0.92f, 0.34f, 1f);

        LayoutElement layout = iconObject.GetComponent<LayoutElement>();
        layout.preferredWidth = 48f;
        layout.preferredHeight = 48f;
    }

    TextMeshProUGUI CreateRunGoldValueText(Transform parent, TextMeshProUGUI sourceText)
    {
        GameObject textObject = new GameObject("RunGoldValueText", typeof(RectTransform), typeof(CanvasRenderer), typeof(TextMeshProUGUI), typeof(LayoutElement));
        textObject.transform.SetParent(parent, false);

        TextMeshProUGUI text = textObject.GetComponent<TextMeshProUGUI>();
        text.font = sourceText.font;
        text.fontSharedMaterial = sourceText.fontSharedMaterial;
        text.raycastTarget = false;
        ConfigureCounterValueText(text, Color.white);

        return text;
    }

    void ConfigureCounterValueText(TextMeshProUGUI text, Color color)
    {
        if (text == null)
            return;

        text.fontSize = 29f;
        text.fontStyle = FontStyles.Bold;
        text.alignment = TextAlignmentOptions.MidlineLeft;
        text.color = color;
        text.enableAutoSizing = true;
        text.fontSizeMin = 18f;
        text.fontSizeMax = 29f;
        text.enableWordWrapping = false;
        text.overflowMode = TextOverflowModes.Overflow;
        text.outlineColor = new Color32(0, 0, 0, 255);
        text.outlineWidth = 0.16f;

        LayoutElement layout = text.GetComponent<LayoutElement>();
        if (layout == null)
            layout = text.gameObject.AddComponent<LayoutElement>();
        layout.preferredWidth = 92f;
        layout.preferredHeight = 40f;
    }

    void ConfigureCounterGroupManual(Transform group, Image icon, TextMeshProUGUI valueText, bool isGold)
    {
        if (group == null)
            return;

        if (group.TryGetComponent(out HorizontalLayoutGroup horizontalLayout))
            horizontalLayout.enabled = false;

        if (group is RectTransform groupRect)
        {
            groupRect.anchorMin = new Vector2(1f, 1f);
            groupRect.anchorMax = new Vector2(1f, 1f);
            groupRect.pivot = new Vector2(1f, 1f);
            groupRect.sizeDelta = new Vector2(152f, 46f);
        }

        Image background = EnsureImage(group.gameObject);
        if (background != null)
        {
            background.color = new Color(0f, 0f, 0f, 0.92f);
            background.raycastTarget = false;
        }

        if (icon == null)
        {
            icon = group.GetComponentsInChildren<Image>(true)
                .FirstOrDefault(image => image.gameObject != group.gameObject);
        }

        if (valueText == null)
            valueText = group.GetComponentInChildren<TextMeshProUGUI>(true);

        if (icon != null)
        {
            icon.color = isGold ? new Color(1f, 0.86f, 0.12f, 1f) : Color.white;
            icon.preserveAspect = true;

            RectTransform iconRect = icon.GetComponent<RectTransform>();
            if (iconRect != null)
            {
                iconRect.anchorMin = new Vector2(0f, 0.5f);
                iconRect.anchorMax = new Vector2(0f, 0.5f);
                iconRect.pivot = new Vector2(0.5f, 0.5f);
                iconRect.anchoredPosition = new Vector2(27f, 0f);
                iconRect.sizeDelta = new Vector2(48f, 48f);
            }
        }

        if (valueText != null)
        {
            ConfigureCounterValueText(valueText, Color.white);

            RectTransform textRect = valueText.GetComponent<RectTransform>();
            if (textRect != null)
            {
                textRect.anchorMin = new Vector2(0f, 0.5f);
                textRect.anchorMax = new Vector2(1f, 0.5f);
                textRect.pivot = new Vector2(0f, 0.5f);
                textRect.anchoredPosition = new Vector2(56f, 0f);
                textRect.sizeDelta = new Vector2(-62f, 40f);
            }
        }
    }

    void RefreshRunGoldUI()
    {
        if (runGoldValueText == null)
            return;

        runGoldValueText.text = Manager.GameM.GetCurrentRunGold().ToString("N0");
    }

    void ConfigureBattleCounterLayout()
    {
        TextMeshProUGUI killText = GetText(typeof(Texts), (int)Texts.KillValueText);
        if (killText == null || killText.transform.parent == null)
            return;

        Transform killGroup = killText.transform.parent;
        Transform battleInfo = killGroup.parent;

        if (battleInfo is RectTransform battleRect)
        {
            battleRect.anchorMin = new Vector2(1f, 1f);
            battleRect.anchorMax = new Vector2(1f, 1f);
            battleRect.pivot = new Vector2(1f, 1f);
            battleRect.anchoredPosition = new Vector2(-10f, -43f);
            battleRect.sizeDelta = new Vector2(152f, 94f);
        }

        if (battleInfo != null)
        {
            HorizontalLayoutGroup horizontalBattleLayout = battleInfo.GetComponent<HorizontalLayoutGroup>();
            if (horizontalBattleLayout != null)
                horizontalBattleLayout.enabled = false;

            VerticalLayoutGroup battleLayout = battleInfo.GetComponent<VerticalLayoutGroup>();
            if (battleLayout != null)
                battleLayout.enabled = false;

            Transform goldGroup = battleInfo.Find(RunGoldObjectName);
            if (goldGroup != null)
            {
                ConfigureRunGoldObject(goldGroup.gameObject);
                goldGroup.SetSiblingIndex(0);
                if (goldGroup is RectTransform goldGroupRect)
                    goldGroupRect.anchoredPosition = Vector2.zero;
            }
            killGroup.SetSiblingIndex(goldGroup != null ? 1 : 0);
        }

        if (killGroup is RectTransform killRect)
        {
            killRect.anchorMin = new Vector2(1f, 1f);
            killRect.anchorMax = new Vector2(1f, 1f);
            killRect.pivot = new Vector2(1f, 1f);
            killRect.anchoredPosition = new Vector2(0f, -48f);
            killRect.sizeDelta = new Vector2(152f, 46f);
        }

        Image killBackground = EnsureImage(killGroup.gameObject);
        killBackground.color = new Color(0f, 0f, 0f, 0.92f);
        killBackground.raycastTarget = false;

        if (killGroup.TryGetComponent(out HorizontalLayoutGroup killLayout))
        {
            killLayout.childAlignment = TextAnchor.MiddleRight;
            killLayout.padding = new RectOffset(3, 5, 2, 2);
            killLayout.spacing = 3f;
            killLayout.childControlWidth = false;
            killLayout.childControlHeight = false;
            killLayout.childForceExpandWidth = false;
            killLayout.childForceExpandHeight = false;
        }

        ConfigureCounterValueText(killText, new Color(0.94f, 0.97f, 1f, 1f));

        LayoutElement killGroupLayout = killGroup.GetComponent<LayoutElement>();
        if (killGroupLayout == null)
            killGroupLayout = killGroup.gameObject.AddComponent<LayoutElement>();
        killGroupLayout.preferredWidth = 152f;
        killGroupLayout.preferredHeight = 46f;

        Image killImage = killGroup.GetComponentsInChildren<Image>(true)
            .FirstOrDefault(image => image.gameObject != killGroup.gameObject);
        if (killImage != null)
        {
            RectTransform imageRect = killImage.GetComponent<RectTransform>();
            if (imageRect != null)
                imageRect.sizeDelta = new Vector2(48f, 48f);

            killImage.color = Color.white;

            LayoutElement imageLayout = killImage.GetComponent<LayoutElement>();
            if (imageLayout == null)
                imageLayout = killImage.gameObject.AddComponent<LayoutElement>();
            imageLayout.preferredWidth = 48f;
            imageLayout.preferredHeight = 48f;
        }

        ConfigureCounterGroupManual(killGroup, killImage, killText, false);

        if (battleInfo is RectTransform parentRect)
            LayoutRebuilder.ForceRebuildLayoutImmediate(parentRect);
    }

    void HideBattleSkillHud()
    {
        string[] hudObjectNames =
        {
            "BattleObject",
            "BattleList",
            "BattleButton",
            "BattleSkill",
            "BattleSkillImage",
            "BattleSkillCountValueText",
            "BattleSkilISlot_1",
            "BattleSkilISlot_2",
            "BattleSkilISlot_3",
            "BattleSkilISlot_4",
            "BattleSkilISlot_5",
            "BattleSkilISlot_6",
            "EvolutionItem",
            "EvolutionItemImage",
            "EvolutionItemSlot_1",
            "EvolutionItemSlot_2",
            "EvolutionItemSlot_3",
            "EvolutionItemSlot_4",
            "EvolutionItemSlot_5",
            "EvolutionItemSlot_6",
        };

        foreach (string objectName in hudObjectNames)
            Utils.FindChild(gameObject, objectName, true)?.SetActive(false);
    }

    void ConfigureTopTimeLayout()
    {
        TextMeshProUGUI timeText = GetText(typeof(Texts), (int)Texts.TimeLimitValueText);
        if (timeText == null)
            return;

        RectTransform timeParentRect = timeText.transform.parent as RectTransform;
        if (timeParentRect != null)
        {
            timeParentRect.anchorMin = new Vector2(0.5f, 1f);
            timeParentRect.anchorMax = new Vector2(0.5f, 1f);
            timeParentRect.pivot = new Vector2(0.5f, 0.5f);
            timeParentRect.anchoredPosition = new Vector2(0f, -84f);
            timeParentRect.sizeDelta = new Vector2(340f, 104f);
        }

        RectTransform timeRect = timeText.GetComponent<RectTransform>();
        if (timeRect != null)
        {
            timeRect.anchorMin = Vector2.zero;
            timeRect.anchorMax = Vector2.one;
            timeRect.pivot = new Vector2(0.5f, 0.5f);
            timeRect.anchoredPosition = Vector2.zero;
            timeRect.sizeDelta = new Vector2(-10f, 0f);
        }

        timeText.text = "0:00";
        timeText.fontSize = 96f;
        timeText.enableAutoSizing = true;
        timeText.fontSizeMin = 64f;
        timeText.fontSizeMax = 96f;
        timeText.fontStyle = FontStyles.Bold;
        timeText.alignment = TextAlignmentOptions.Midline;
        timeText.color = new Color(1f, 1f, 0.96f, 1f);
        timeText.outlineColor = new Color32(0, 5, 18, 255);
        timeText.outlineWidth = 0.22f;

        Transform brokenOutline = timeText.transform.parent.Find("TimeLimitOutlineText");
        if (brokenOutline != null)
            Destroy(brokenOutline.gameObject);

        foreach (UnityEngine.UI.Outline outline in timeText.GetComponents<UnityEngine.UI.Outline>())
            outline.enabled = false;

        Shadow timeShadow = timeText.GetComponents<Shadow>()
            .FirstOrDefault(effect => effect.GetType() == typeof(Shadow));
        if (timeShadow == null)
            timeShadow = timeText.gameObject.AddComponent<Shadow>();

        timeShadow.effectColor = new Color(0f, 0f, 0f, 0.9f);
        timeShadow.effectDistance = new Vector2(3.5f, -4.5f);
        timeShadow.useGraphicAlpha = false;
    }

    void ConfigurePremiumTopHud()
    {
        ConfigurePauseButtonStyle();
        ConfigureTopTimePanel();
        ConfigureBattleCounterLayout();
    }

    void ConfigurePauseButtonStyle()
    {
        Button pauseButton = GetButton(ButtonsType, (int)Buttons.PauseButton);
        if (pauseButton == null)
            return;

        RectTransform pauseRect = pauseButton.GetComponent<RectTransform>();
        if (pauseRect != null)
        {
            pauseRect.anchorMin = new Vector2(0f, 1f);
            pauseRect.anchorMax = new Vector2(0f, 1f);
            pauseRect.pivot = new Vector2(0.5f, 0.5f);
            pauseRect.anchoredPosition = new Vector2(82f, -84f);
            pauseRect.sizeDelta = new Vector2(96f, 96f);
        }

        Image background = EnsureImage(pauseButton.gameObject);
        background.color = new Color(0.16f, 0.13f, 0.26f, 0.94f);
        background.raycastTarget = true;

        UnityEngine.UI.Outline outline = pauseButton.GetComponent<UnityEngine.UI.Outline>();
        if (outline == null)
            outline = pauseButton.gameObject.AddComponent<UnityEngine.UI.Outline>();

        outline.effectColor = new Color(0.66f, 0.59f, 0.92f, 0.95f);
        outline.effectDistance = new Vector2(3f, -3f);
        outline.useGraphicAlpha = false;

        Shadow shadow = pauseButton.GetComponents<Shadow>()
            .FirstOrDefault(effect => effect.GetType() == typeof(Shadow));
        if (shadow == null)
            shadow = pauseButton.gameObject.AddComponent<Shadow>();

        shadow.effectColor = new Color(0.02f, 0.015f, 0.045f, 0.75f);
        shadow.effectDistance = new Vector2(0f, -5f);
        shadow.useGraphicAlpha = false;

        Image[] childImages = pauseButton.GetComponentsInChildren<Image>(true);
        foreach (Image image in childImages)
        {
            if (image.gameObject == pauseButton.gameObject)
                continue;

            image.color = new Color(1f, 0.98f, 0.92f, 1f);
            RectTransform imageRect = image.GetComponent<RectTransform>();
            if (imageRect != null)
            {
                imageRect.anchorMin = new Vector2(0.5f, 0.5f);
                imageRect.anchorMax = new Vector2(0.5f, 0.5f);
                imageRect.pivot = new Vector2(0.5f, 0.5f);
                imageRect.anchoredPosition = Vector2.zero;
                imageRect.sizeDelta = new Vector2(58f, 58f);
            }
        }
    }

    void ConfigureTopTimePanel()
    {
        TextMeshProUGUI timeText = GetText(typeof(Texts), (int)Texts.TimeLimitValueText);
        if (timeText == null || timeText.transform.parent == null)
            return;

        Image background = EnsureImage(timeText.transform.parent.gameObject);
        background.color = new Color(0.05f, 0.07f, 0.12f, 0f);
        background.raycastTarget = false;
    }

    Image EnsureImage(GameObject target)
    {
        Image image = target.GetComponent<Image>();
        if (image == null)
            image = target.AddComponent<Image>();

        image.enabled = true;
        return image;
    }

    void MakeTopBarTransparent()
    {
        Transform topMenu = FindTransformIncludingInactive(transform, "TopMenu");
        if (topMenu != null)
        {
            SetImageAlpha(topMenu.gameObject, 0f);
            SetNamedDescendantImagesAlpha(topMenu, "BackgroundImage", 0f);
            SetNamedDescendantImagesAlpha(topMenu, "LevelBackgroundImage", 0f);
            SetNamedDescendantImagesAlpha(topMenu, "LevelUPImage", 0f);
        }

        foreach (Transform topBackground in FindTransformsIncludingInactive(transform, "UpBackgroundImage"))
        {
            SetImageAlpha(topBackground.gameObject, 0f);
            topBackground.gameObject.SetActive(false);
        }

        SetAllNamedImagesAlpha("BackgroundImage", 0f);
        SetAllNamedImagesAlpha("LevelBackgroundImage", 0f);
        SetAllNamedImagesAlpha("LevelUPImage", 0f);
    }

    Transform FindTransformIncludingInactive(Transform root, string targetName)
    {
        if (root == null)
            return null;

        if (root.name == targetName)
            return root;

        for (int i = 0; i < root.childCount; i++)
        {
            Transform found = FindTransformIncludingInactive(root.GetChild(i), targetName);
            if (found != null)
                return found;
        }

        return null;
    }

    IEnumerable<Transform> FindTransformsIncludingInactive(Transform root, string targetName)
    {
        if (root == null)
            yield break;

        if (root.name == targetName)
            yield return root;

        for (int i = 0; i < root.childCount; i++)
        {
            foreach (Transform found in FindTransformsIncludingInactive(root.GetChild(i), targetName))
                yield return found;
        }
    }

    void SetImageAlpha(GameObject target, float alpha)
    {
        if (target == null || !target.TryGetComponent(out Image image))
            return;

        Color color = image.color;
        color.a = alpha;
        image.color = color;
    }

    void SetNamedDescendantImagesAlpha(Transform root, string objectName, float alpha)
    {
        foreach (Image image in root.GetComponentsInChildren<Image>(true))
            if (image.gameObject.name == objectName)
                SetImageAlpha(image.gameObject, alpha);
    }

    void SetAllNamedImagesAlpha(string objectName, float alpha)
    {
        foreach (Image image in GetComponentsInChildren<Image>(true))
            if (image.gameObject.name == objectName)
                SetImageAlpha(image.gameObject, alpha);
    }

    void ConfigureExpSliderLayout()
    {
        expSlider = GetSlider(typeof(Sliders), (int)Sliders.ExpSliderObject);
        if (expSlider == null)
            return;

        RectTransform expRect = expSlider.GetComponent<RectTransform>();
        if (expRect != null)
        {
            expRect.anchorMin = new Vector2(0f, 1f);
            expRect.anchorMax = new Vector2(1f, 1f);
            expRect.pivot = new Vector2(0.5f, 1f);
            expRect.anchoredPosition = new Vector2(0f, -4f);
            expRect.sizeDelta = new Vector2(-26f, 50f);
            expRect.offsetMin = new Vector2(13f, expRect.offsetMin.y);
            expRect.offsetMax = new Vector2(-6f, expRect.offsetMax.y);
        }

        if (expSlider.transform.parent is RectTransform parentRect)
        {
            parentRect.anchorMin = new Vector2(0.5f, 1f);
            parentRect.anchorMax = new Vector2(0.5f, 1f);
            parentRect.pivot = new Vector2(0.5f, 1f);
            parentRect.anchoredPosition = new Vector2(0f, -176f);
            parentRect.sizeDelta = new Vector2(GetMobileFrameUiWidth() - 16f, 96f);
        }

        Image sliderFrame = EnsureImage(expSlider.gameObject);
        sliderFrame.color = ExpBackgroundColor;
        sliderFrame.raycastTarget = false;

        UnityEngine.UI.Outline sliderBorder = expSlider.GetComponent<UnityEngine.UI.Outline>();
        if (sliderBorder == null)
            sliderBorder = expSlider.gameObject.AddComponent<UnityEngine.UI.Outline>();

        sliderBorder.effectColor = ExpBorderColor;
        sliderBorder.effectDistance = new Vector2(5f, -5f);
        sliderBorder.useGraphicAlpha = false;

        Shadow sliderShadow = expSlider.GetComponents<Shadow>()
            .FirstOrDefault(effect => effect.GetType() == typeof(Shadow));
        if (sliderShadow == null)
            sliderShadow = expSlider.gameObject.AddComponent<Shadow>();

        sliderShadow.effectColor = new Color(0f, 0.015f, 0.08f, 0.9f);
        sliderShadow.effectDistance = new Vector2(0f, -5f);
        sliderShadow.useGraphicAlpha = false;

        expFillImage = expSlider.fillRect != null ? expSlider.fillRect.GetComponent<Image>() : null;
        if (expFillImage != null)
        {
            expFillImage.color = ExpFillColor;

            Shadow fillGlow = expFillImage.GetComponent<Shadow>();
            if (fillGlow == null)
                fillGlow = expFillImage.gameObject.AddComponent<Shadow>();

            fillGlow.effectColor = new Color(0.82f, 1f, 0.08f, 0.85f);
            fillGlow.effectDistance = new Vector2(0f, -2f);
            fillGlow.useGraphicAlpha = false;

            EnsureExpGainSpark();
        }

        ConfigureExpInnerGaugeLayout();

        expBackgroundImage = expSlider.GetComponentsInChildren<Image>(true)
            .FirstOrDefault(image => image.gameObject.name.Contains("Background"));
        if (expBackgroundImage != null)
        {
            expBackgroundImage.color = ExpBackgroundColor;

            UnityEngine.UI.Outline border = expBackgroundImage.GetComponent<UnityEngine.UI.Outline>();
            if (border == null)
                border = expBackgroundImage.gameObject.AddComponent<UnityEngine.UI.Outline>();

            border.effectColor = ExpBorderColor;
            border.effectDistance = new Vector2(3f, -3f);
            border.useGraphicAlpha = false;

            Shadow trackShadow = expBackgroundImage.GetComponents<Shadow>()
                .FirstOrDefault(effect => effect.GetType() == typeof(Shadow));
            if (trackShadow == null)
                trackShadow = expBackgroundImage.gameObject.AddComponent<Shadow>();

            trackShadow.effectColor = new Color(0f, 0.01f, 0.045f, 0.72f);
            trackShadow.effectDistance = new Vector2(0f, -3f);
            trackShadow.useGraphicAlpha = false;
        }

        ConfigureExpLevelBadgeLayout();
    }

    void ConfigureExpInnerGaugeLayout()
    {
        if (expSlider == null)
            return;

        ConfigureExpInsetChild(expSlider.transform.Find("BackgroundImage") as RectTransform, 6f, 82f);
        ConfigureExpInsetChild(expSlider.transform.Find("Fill Area") as RectTransform, 6f, 82f);
    }

    void ConfigureExpInsetChild(RectTransform rect, float leftInset, float rightInset)
    {
        if (rect == null)
            return;

        rect.anchorMin = new Vector2(0f, 0.25f);
        rect.anchorMax = new Vector2(1f, 0.75f);
        rect.pivot = new Vector2(0.5f, 0.5f);
        rect.offsetMin = new Vector2(leftInset, 0f);
        rect.offsetMax = new Vector2(-rightInset, 0f);
    }

    float GetMobileFrameUiWidth()
    {
        RectTransform canvasRect = GetComponentInParent<Canvas>()?.GetComponent<RectTransform>();
        if (canvasRect == null)
            return 450f;

        float canvasWidth = canvasRect.rect.width;
        float canvasHeight = canvasRect.rect.height;
        float mobileFrameWidth = canvasHeight * Utils.MobileFrameAspect;
        return Mathf.Min(canvasWidth, mobileFrameWidth);
    }

    void EnsureExpGainSpark()
    {
        if (expFillImage == null)
            return;

        Transform existing = expFillImage.transform.Find("ExpGainSpark");
        if (existing != null)
            expSparkImage = existing.GetComponent<Image>();

        if (expSparkImage == null)
        {
            GameObject sparkObject = new GameObject("ExpGainSpark", typeof(RectTransform), typeof(CanvasRenderer), typeof(Image));
            sparkObject.transform.SetParent(expFillImage.transform, false);
            expSparkImage = sparkObject.GetComponent<Image>();
        }

        RectTransform sparkRect = expSparkImage.GetComponent<RectTransform>();
        sparkRect.anchorMin = new Vector2(1f, 0f);
        sparkRect.anchorMax = new Vector2(1f, 1f);
        sparkRect.pivot = new Vector2(0.5f, 0.5f);
        sparkRect.anchoredPosition = new Vector2(0f, 0f);
        sparkRect.sizeDelta = new Vector2(30f, 18f);

        expSparkImage.sprite = expFillImage.sprite;
        expSparkImage.type = expFillImage.type;
        expSparkImage.raycastTarget = false;
        expSparkImage.color = new Color(0.9f, 1f, 1f, 0f);
        expSparkImage.gameObject.SetActive(false);
    }

    void ConfigureExpLevelBadgeLayout()
    {
        TextMeshProUGUI levelText = GetText(typeof(Texts), (int)Texts.CharacterLevelValueText);
        if (levelText == null)
            return;

        RectTransform levelRect = levelText.GetComponent<RectTransform>();
        if (levelRect != null)
        {
            levelRect.anchorMin = new Vector2(1f, 1f);
            levelRect.anchorMax = new Vector2(1f, 1f);
            levelRect.pivot = new Vector2(1f, 1f);
            levelRect.anchoredPosition = new Vector2(-13f, -58f);
            levelRect.sizeDelta = new Vector2(74f, 30f);
        }

        levelText.fontSize = 26f;
        levelText.enableAutoSizing = true;
        levelText.fontSizeMin = 18f;
        levelText.fontSizeMax = 26f;
        levelText.fontStyle = FontStyles.Bold;
        levelText.alignment = TextAlignmentOptions.Midline;
        levelText.color = Color.white;
        levelText.outlineColor = new Color32(0, 0, 0, 255);
        levelText.outlineWidth = 0.2f;

        Transform badge = FindTransformIncludingInactive(levelText.transform.parent, "LevelBackgroundImage");
        if (badge == null)
        {
            GameObject badgeObject = new GameObject("LevelBackgroundImage", typeof(RectTransform), typeof(CanvasRenderer), typeof(Image));
            badgeObject.transform.SetParent(levelText.transform.parent, false);
            badge = badgeObject.transform;
        }

        badge.SetParent(levelText.transform.parent, false);
        badge.SetSiblingIndex(levelText.transform.GetSiblingIndex());

        if (badge is RectTransform badgeRect)
        {
            badgeRect.anchorMin = new Vector2(1f, 1f);
            badgeRect.anchorMax = new Vector2(1f, 1f);
            badgeRect.pivot = new Vector2(1f, 1f);
            badgeRect.anchoredPosition = new Vector2(-13f, -54f);
            badgeRect.sizeDelta = new Vector2(78f, 34f);
        }

        Image badgeImage = EnsureImage(badge.gameObject);
        badgeImage.color = new Color(0.02f, 0.025f, 0.04f, 0.92f);
        badgeImage.raycastTarget = false;

        UnityEngine.UI.Outline badgeOutline = badge.GetComponent<UnityEngine.UI.Outline>();
        if (badgeOutline == null)
            badgeOutline = badge.gameObject.AddComponent<UnityEngine.UI.Outline>();

        badgeOutline.effectColor = new Color(0f, 0f, 0f, 1f);
        badgeOutline.effectDistance = new Vector2(3f, -3f);
        badgeOutline.useGraphicAlpha = false;

        Shadow badgeShadow = badge.GetComponents<Shadow>()
            .FirstOrDefault(effect => effect.GetType() == typeof(Shadow));
        if (badgeShadow == null)
            badgeShadow = badge.gameObject.AddComponent<Shadow>();

        badgeShadow.effectColor = new Color(0f, 0f, 0f, 0.75f);
        badgeShadow.effectDistance = new Vector2(0f, -3f);
        badgeShadow.useGraphicAlpha = false;

        levelText.transform.SetAsLastSibling();
    }

    void UpdateExpSlider(float targetValue)
    {
        if (expSlider == null)
            expSlider = GetSlider(typeof(Sliders), (int)Sliders.ExpSliderObject);
        if (expSlider == null)
            return;

        float clampedValue = Mathf.Clamp01(targetValue);
        float previousValue = expSlider.value;
        expSlider.value = clampedValue;

        if (clampedValue > previousValue + 0.001f)
            PlayExpGaugeGlow();
    }

    void SetExpSliderImmediate(float targetValue)
    {
        if (expSlider == null)
            expSlider = GetSlider(typeof(Sliders), (int)Sliders.ExpSliderObject);
        if (expSlider == null)
            return;

        float clampedValue = Mathf.Clamp01(targetValue);
        float previousValue = expSlider.value;
        expSliderTween?.Kill();
        expSlider.value = clampedValue;

        if (clampedValue > previousValue + 0.001f)
            PlayExpGaugeGlow();
    }

    void PlayExpGaugeGlow()
    {
        if (expFillImage == null)
            return;

        expGlowSequence?.Kill();
        if (expSparkImage != null)
        {
            expSparkImage.gameObject.SetActive(true);
            expSparkImage.transform.localScale = Vector3.one;
            expSparkImage.color = new Color(0.9f, 1f, 1f, 0f);
        }

        expGlowSequence = DOTween.Sequence()
            .Append(expFillImage.DOColor(ExpFlashColor, 0.08f).SetEase(Ease.OutQuad))
            .Join(expFillImage.transform.DOScale(new Vector3(1.04f, 1.55f, 1f), 0.08f).SetEase(Ease.OutQuad));

        if (expSparkImage != null)
        {
            expGlowSequence
                .Join(expSparkImage.DOFade(1f, 0.08f).SetEase(Ease.OutQuad))
                .Join(expSparkImage.transform.DOScale(new Vector3(1.35f, 1.8f, 1f), 0.16f).SetEase(Ease.OutQuad));
        }

        expGlowSequence.Append(expFillImage.DOColor(ExpGlowColor, 0.11f).SetEase(Ease.OutQuad));

        if (expSparkImage != null)
            expGlowSequence.Join(expSparkImage.DOFade(0.15f, 0.11f).SetEase(Ease.InQuad));

        expGlowSequence
            .Append(expFillImage.DOColor(ExpFillColor, 0.26f).SetEase(Ease.OutQuad))
            .Join(expFillImage.transform.DOScale(Vector3.one, 0.26f).SetEase(Ease.OutBack));

        if (expSparkImage != null)
        {
            expGlowSequence
                .Join(expSparkImage.DOFade(0f, 0.18f).SetEase(Ease.InQuad))
                .OnComplete(() => expSparkImage.gameObject.SetActive(false));
        }
    }

    public void OnPlayerLevelUp()
    {
        if (Manager.GameM.isGameEnd) return;

        EnsurePlayerSkillCandidates();
        Manager.UiM.ShowPopup<UI_SkillSelectPopup>();

        SetExpSliderImmediate(Manager.GameM.player.ExpRatio);
        GetText(typeof(Texts), (int)Texts.CharacterLevelValueText).text = $"LV.{Manager.GameM.ContinueDatas.Level}";
    }

    void EnsurePlayerSkillCandidates()
    {
        PlayerController player = Manager.GameM.player;
        if (player == null || player.Skills == null)
            return;

        if (player.Skills.skillList.Count == 0)
            player.InitSkill();
    }

    public void MonsterInfoUpdate(MonsterController _mc)
    {
        if (_mc.objType == Define.ObjectType.EliteMonster)
        {
            if (_mc.CreatureState != Define.CreatureState.Dead)
            {
                GetObject(gameObjectsType, (int)GameObjects.EliteInfoObject).SetActive(true);
                GetSlider(SlidersType, (int)Sliders.EliteHpSliderObject).value = _mc.Hp / _mc.MaxHp;
                GetText(TextsType, (int)Texts.EliteNameValueText).text = _mc.creatureData.NameKR;
            }
            else
                GetObject(gameObjectsType, (int)GameObjects.EliteInfoObject).SetActive(false);

        }
        else if (_mc.objType == Define.ObjectType.Boss)
        {
            if (_mc.CreatureState != Define.CreatureState.Dead)
            {
                GetObject(gameObjectsType, (int)GameObjects.BossInfoObject).SetActive(true);
                GetSlider(SlidersType, (int)Sliders.BossHpSliderObject).value = _mc.Hp / _mc.MaxHp;
                GetText(TextsType, (int)Texts.BossNameValueText).text = _mc.creatureData.NameKR;
            }
            else
                GetObject(gameObjectsType, (int)GameObjects.BossInfoObject).SetActive(false);
        }
    }


    void OnClickPauseButton()
    {
        Manager.SoundM.PlayButtonClick();
        Manager.UiM.ShowPopup<UI_PausePopup>();
    }

    void OnClickHealButton()
    {
        Manager.SoundM.PlayButtonClick();
        if (Manager.GameM.player.SpecialSkillHealCount > 0)
        {
            Manager.GameM.player.SpecialSkillHealCount--;
            Manager.GameM.player.Healing(1);
            OnLevelUpSkillUI();
        }
    }

    void OnLevelUpSkillUI()
    {
        List<SkillBase> activeSkills = Manager.GameM.player.Skills.skillList.Where(skill => skill.isLearnSkill).ToList();

        for (int i = 0; i < activeSkills.Count; i++)
        {
            SetCurrentSkill(i, activeSkills[i]);
        }

        List<int> GetEvoloutionItems = Manager.GameM.player.Skills.evolutionItemList.ToList();
        for (int i = 0; i < GetEvoloutionItems.Count; i++)
        {
            SetEvolutionItem(i, GetEvoloutionItems[i]);
        }

        if (Manager.GameM.player.SpecialSkillHealCount > 0)
            GetButton(ButtonsType, (int)Buttons.HealButton).interactable = true;
        else
            GetButton(ButtonsType, (int)Buttons.HealButton).interactable = false;
    }

    void SetCurrentSkill(int _num, SkillBase _skill)
    {
        string iconKey = _skill.SkillDatas.SkillIcon;
        Sprite icon = Manager.ResourceM.Load<Sprite>(iconKey);
        if (icon == null)
            Debug.LogError($"Missing battle skill icon sprite: {iconKey}");

        GetImage(ImagesType, (int)Images.BattleSkilI_Icon_1 + _num).sprite = icon;
        GetImage(ImagesType, (int)Images.BattleSkilI_Icon_1 + _num).enabled = true;
    }

    void SetEvolutionItem(int _num, int _evolutionItemID)
    {
        string iconKey = Manager.DataM.SkillEvolutionDic[_evolutionItemID].EvolutionItemIcon;
        Sprite icon = Manager.ResourceM.Load<Sprite>(iconKey);
        if (icon == null)
            Debug.LogError($"Missing evolution item icon sprite: {iconKey}");

        GetImage(ImagesType, (int)Images.EvolutionItem_Icon_1 + _num).sprite = icon;
        GetImage(ImagesType, (int)Images.EvolutionItem_Icon_1 + _num).enabled = true;
    }

    IEnumerator SwitchAlarm(AlramType _type)
    {
        switch (_type)
        {
            case AlramType.Wave:
                Manager.SoundM.Play(Define.Sound.Effect, "Warning_Wave");
                GetObject(gameObjectsType, (int)GameObjects.MonsterAlarmObject).SetActive(true);
                yield return new WaitForSeconds(3f);
                GetObject(gameObjectsType, (int)GameObjects.MonsterAlarmObject).SetActive(false);
                break;

            case AlramType.Boss:
                Manager.SoundM.Play(Define.Sound.Effect, "Warning_Boss");
                GetObject(gameObjectsType, (int)GameObjects.BossAlarmObject).SetActive(true);
                yield return new WaitForSeconds(3f);
                GetObject(gameObjectsType, (int)GameObjects.BossAlarmObject).SetActive(false);
                break;
        }

    }

    public void OnDamaged()
    {
        StartCoroutine(CoBloodScreen());
    }

    public void WhiteFlash()
    {
        StartCoroutine(CoWhiteScreen());
    }

    IEnumerator CoBloodScreen()
    {
        Color color = new Color(1, 0, 0, 0.3f);

        yield return null;

        DOTween.Sequence().
            Append(GetImage(ImagesType, (int)Images.OnDamaged).DOColor(color, 0.3f))
            .Append(GetImage(ImagesType, (int)Images.OnDamaged).DOColor(Color.clear, 0.3f)).OnComplete(() => { });

    }

    IEnumerator CoWhiteScreen()
    {
        Color color = new Color(1, 1, 1, 1f);

        yield return null;

        DOTween.Sequence().
            Append(GetImage(ImagesType, (int)Images.WhiteFlash).DOFade(1, 0.15f))
            .Append(GetImage(ImagesType, (int)Images.WhiteFlash).DOFade(0, 0.3f)).OnComplete(() => { });
    }


    #region Test

    bool isDevMenuActive = false;
    void OnClickHiddenButton()
    {
        GetObject(gameObjectsType, (int)GameObjects.DevMenu).SetActive(!isDevMenuActive);
        isDevMenuActive = !isDevMenuActive;
    }

    void OnClickMonsterAllKillButton()
    {
        Manager.ObjectM.KillAllMonsters();
        //Manager.UiM.ShowToast("모든 몬스터를 처치했습니다.(Test)");
    }

    void OnClickNextWaveButton()
    {
        if (Manager.SceneM.CurrentScene.SceneType == Define.SceneType.GameScene)
        {
            GameScene gs = Manager.SceneM.CurrentScene as GameScene;
            gs.WaveEnd();
            //Manager.UiM.ShowToast("다음 웨이브로 넘어갑니다.(Test)");
        }
    }
    void OnClickLevelUpButton()
    {
        Manager.DataM.LevelDic.TryGetValue(Manager.GameM.player.Level, out var CurrentLevelData);

        float needExp = CurrentLevelData.TotalExp - Manager.GameM.player.Exp;

        Manager.GameM.player.Exp += needExp;
        //Manager.UiM.ShowToast("레벨업 합니다.(Test)");
    }
    #endregion

}
