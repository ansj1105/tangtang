using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
using TMPro;
using Unity.VisualScripting;
using DG.Tweening;
using Cysharp.Threading.Tasks;

public class UI_TitleScene : UI_Scene
{
    public enum Buttons
    {
        StartButton
    }

    public enum Sliders
    {
        Slider
    }

    public enum Texts
    {
        StartText,
        CountText
    }
    bool isLoadEnd = false;
    private GameObject loadingIndicatorObject;
    private GameObject loadingProgressObject;
    private Image loadingProgressFillImage;
    private RectTransform loadingProgressFillRect;
    private TextMeshProUGUI loadingStatusText;
    private Tween loadingSpinnerTween;
    private Tween startTextTween;

    public override bool Init()
    {
        if (!base.Init()) return false;

        ButtonsType = typeof(Buttons);
        SlidersType = typeof(Sliders);
        TextsType = typeof(Texts);
        BindButton(ButtonsType);
        BindSlider(SlidersType);
        BindText(TextsType);


        GetButton(typeof(Buttons), (int)Buttons.StartButton).gameObject.BindEvent(() =>
        {
            OnClickStartButton().Forget();
        });
        GetButton(typeof(Buttons), (int)Buttons.StartButton).gameObject.SetActive(false);

        GetText(typeof(Texts), (int)Texts.StartText).gameObject.SetActive(false);
        ShowLoadingIndicator();

        SetInfo().Forget();
        return true;
    }

    private int loadingLoadedAssetCount = 0;
    private int loadingTotalAssetCount = 0;

    async UniTask SetInfo()
    {
        try
        {
            string alwaysKeepLabel = "AlwaysKeep";
            string NeedReleaseLabel = "NeedRelease";

            loadingLoadedAssetCount = 0;
            loadingTotalAssetCount = 0;
            AddLoadingTotal(await Manager.ResourceM.CountGroupAsync<Object>(alwaysKeepLabel));
            AddLoadingTotal(await Manager.ResourceM.CountGroupAsync<Object>(NeedReleaseLabel));
            AddLoadingTotal(await Manager.ResourceM.CountGroupAsync<Sprite>(alwaysKeepLabel));
            AddLoadingTotal(await Manager.ResourceM.CountGroupAsync<Sprite>(NeedReleaseLabel));
            AddLoadingTotal(await Manager.ResourceM.CountGroupAsync<RuntimeAnimatorController>(alwaysKeepLabel));
            AddLoadingTotal(await Manager.ResourceM.CountGroupAsync<RuntimeAnimatorController>(NeedReleaseLabel));
            UpdateLoadingProgress(loadingLoadedAssetCount, loadingTotalAssetCount);

            await Manager.ResourceM.LoadGroupAsync<Object>(alwaysKeepLabel, (key, count, max) =>
            {
                MarkLoadingAssetLoaded();
            });

            await Manager.ResourceM.LoadGroupAsync<Object>(NeedReleaseLabel, (key, count, max) =>
            {
                MarkLoadingAssetLoaded();
            });

            await Manager.ResourceM.LoadGroupByTypeAsync<Sprite>(alwaysKeepLabel, (key, count, max) => MarkLoadingAssetLoaded());
            await Manager.ResourceM.LoadGroupByTypeAsync<Sprite>(NeedReleaseLabel, (key, count, max) => MarkLoadingAssetLoaded());
            await Manager.ResourceM.LoadGroupByTypeAsync<RuntimeAnimatorController>(alwaysKeepLabel, (key, count, max) => MarkLoadingAssetLoaded());
            await Manager.ResourceM.LoadGroupByTypeAsync<RuntimeAnimatorController>(NeedReleaseLabel, (key, count, max) => MarkLoadingAssetLoaded());

            await EnsureRequiredTitleResourcesLoaded();

            Manager.DataM.Init();
            await EnsureDataDrivenSpritesLoaded();
            Manager.GameM.Init();
            Manager.TimeM.Init();
            Manager.AdM.Init();

            isLoadEnd = true;
            HideLoadingIndicator();
            GetButton(typeof(Buttons), (int)Buttons.StartButton).gameObject.SetActive(true);
            GetText(typeof(Texts), (int)Texts.StartText).gameObject.SetActive(true);
            StartAnim();
        }
        catch (System.Exception e)
        {
            Debug.LogError($"SetInfo async error: {e}");
        }

    }

    private void UpdateLoadingProgress(int current, int total)
    {
        if (total <= 0)
            return;

        Slider slider = GetSlider(typeof(Sliders), (int)Sliders.Slider);
        TextMeshProUGUI countText = GetText(typeof(Texts), (int)Texts.CountText);

        if (slider != null)
            slider.value = (float)current / total;

        float progress = Mathf.Clamp01(current / (float)total);
        UpdateGeneratedProgressBar(progress);

        if (countText != null)
            countText.text = $"{current} / {total}";

        if (loadingStatusText != null)
            loadingStatusText.text = $"로딩중입니다\n{current} / {total}";
    }

    private void AddLoadingTotal(int count)
    {
        if (count <= 0)
            return;

        loadingTotalAssetCount += count;
    }

    private void MarkLoadingAssetLoaded()
    {
        loadingLoadedAssetCount++;
        UpdateLoadingProgress(loadingLoadedAssetCount, loadingTotalAssetCount);
    }

    private void ShowLoadingIndicator()
    {
        Slider slider = GetSlider(typeof(Sliders), (int)Sliders.Slider);
        TextMeshProUGUI countText = GetText(typeof(Texts), (int)Texts.CountText);

        if (slider != null)
            slider.gameObject.SetActive(false);

        if (countText != null)
            countText.gameObject.SetActive(false);

        if (loadingIndicatorObject == null)
        {
            loadingIndicatorObject = new GameObject("LoadingIndicator", typeof(RectTransform));
            loadingIndicatorObject.transform.SetParent(transform, false);

            RectTransform indicatorRect = loadingIndicatorObject.GetComponent<RectTransform>();
            indicatorRect.anchorMin = new Vector2(0.5f, 0f);
            indicatorRect.anchorMax = new Vector2(0.5f, 0f);
            indicatorRect.pivot = new Vector2(0.5f, 0.5f);
            indicatorRect.anchoredPosition = new Vector2(0f, 260f);
            indicatorRect.sizeDelta = new Vector2(360f, 140f);

            Image sourceImage = null;
            if (slider != null && slider.fillRect != null)
                sourceImage = slider.fillRect.GetComponent<Image>();

            GameObject spinnerObject = new GameObject("LoadingSpinner", typeof(RectTransform), typeof(CanvasRenderer), typeof(Image));
            spinnerObject.transform.SetParent(loadingIndicatorObject.transform, false);

            RectTransform spinnerRect = spinnerObject.GetComponent<RectTransform>();
            spinnerRect.anchorMin = new Vector2(0.5f, 0.5f);
            spinnerRect.anchorMax = new Vector2(0.5f, 0.5f);
            spinnerRect.pivot = new Vector2(0.5f, 0.5f);
            spinnerRect.anchoredPosition = new Vector2(0f, 36f);
            spinnerRect.sizeDelta = new Vector2(72f, 72f);

            Image spinnerImage = spinnerObject.GetComponent<Image>();
            if (sourceImage != null)
            {
                spinnerImage.sprite = sourceImage.sprite;
                spinnerImage.color = sourceImage.color;
            }
            spinnerImage.type = Image.Type.Filled;
            spinnerImage.fillMethod = Image.FillMethod.Radial360;
            spinnerImage.fillAmount = 0.72f;
            spinnerImage.raycastTarget = false;

            GameObject textObject = new GameObject("LoadingStatusText", typeof(RectTransform), typeof(CanvasRenderer), typeof(TextMeshProUGUI));
            textObject.transform.SetParent(loadingIndicatorObject.transform, false);

            RectTransform textRect = textObject.GetComponent<RectTransform>();
            textRect.anchorMin = new Vector2(0.5f, 0.5f);
            textRect.anchorMax = new Vector2(0.5f, 0.5f);
            textRect.pivot = new Vector2(0.5f, 0.5f);
            textRect.anchoredPosition = new Vector2(0f, -42f);
            textRect.sizeDelta = new Vector2(360f, 72f);

            loadingStatusText = textObject.GetComponent<TextMeshProUGUI>();
            loadingStatusText.text = "로딩중입니다";
            loadingStatusText.fontSize = 30f;
            loadingStatusText.alignment = TextAlignmentOptions.Center;
            loadingStatusText.raycastTarget = false;

            if (countText != null)
            {
                loadingStatusText.font = countText.font;
                loadingStatusText.fontSharedMaterial = countText.fontSharedMaterial;
                loadingStatusText.color = countText.color;
            }
        }

        EnsureGeneratedProgressBar();

        loadingIndicatorObject.SetActive(true);
        if (loadingProgressObject != null)
            loadingProgressObject.SetActive(true);

        UpdateGeneratedProgressBar(0f);
        loadingSpinnerTween?.Kill();
        loadingSpinnerTween = loadingIndicatorObject.transform.Find("LoadingSpinner")
            .DOLocalRotate(new Vector3(0f, 0f, -360f), 0.9f, RotateMode.FastBeyond360)
            .SetEase(Ease.Linear)
            .SetLoops(-1, LoopType.Restart);
    }

    private void HideLoadingIndicator()
    {
        loadingSpinnerTween?.Kill();
        loadingSpinnerTween = null;

        if (loadingIndicatorObject != null)
            loadingIndicatorObject.SetActive(false);

        if (loadingProgressObject != null)
            loadingProgressObject.SetActive(false);

        Slider slider = GetSlider(typeof(Sliders), (int)Sliders.Slider);
        TextMeshProUGUI countText = GetText(typeof(Texts), (int)Texts.CountText);

        if (slider != null)
            slider.gameObject.SetActive(false);

        if (countText != null)
            countText.gameObject.SetActive(false);
    }

    private void EnsureGeneratedProgressBar()
    {
        if (loadingProgressObject != null)
            return;

        loadingProgressObject = new GameObject("GeneratedLoadingProgress", typeof(RectTransform));
        loadingProgressObject.transform.SetParent(transform, false);

        RectTransform progressRect = loadingProgressObject.GetComponent<RectTransform>();
        progressRect.anchorMin = new Vector2(0f, 0f);
        progressRect.anchorMax = new Vector2(1f, 0f);
        progressRect.pivot = new Vector2(0.5f, 0.5f);
        progressRect.anchoredPosition = new Vector2(0f, 170f);
        progressRect.offsetMin = new Vector2(80f, progressRect.offsetMin.y);
        progressRect.offsetMax = new Vector2(-80f, progressRect.offsetMax.y);
        progressRect.sizeDelta = new Vector2(progressRect.sizeDelta.x, 18f);

        GameObject trackObject = new GameObject("Track", typeof(RectTransform), typeof(CanvasRenderer), typeof(Image));
        trackObject.transform.SetParent(loadingProgressObject.transform, false);
        RectTransform trackRect = trackObject.GetComponent<RectTransform>();
        trackRect.anchorMin = Vector2.zero;
        trackRect.anchorMax = Vector2.one;
        trackRect.offsetMin = Vector2.zero;
        trackRect.offsetMax = Vector2.zero;
        Image trackImage = trackObject.GetComponent<Image>();
        trackImage.color = new Color(0f, 0f, 0f, 0.55f);
        trackImage.raycastTarget = false;

        GameObject fillObject = new GameObject("Fill", typeof(RectTransform), typeof(CanvasRenderer), typeof(Image));
        fillObject.transform.SetParent(trackObject.transform, false);
        loadingProgressFillRect = fillObject.GetComponent<RectTransform>();
        loadingProgressFillRect.anchorMin = Vector2.zero;
        loadingProgressFillRect.anchorMax = new Vector2(0f, 1f);
        loadingProgressFillRect.pivot = new Vector2(0f, 0.5f);
        loadingProgressFillRect.offsetMin = Vector2.zero;
        loadingProgressFillRect.offsetMax = Vector2.zero;
        loadingProgressFillImage = fillObject.GetComponent<Image>();
        loadingProgressFillImage.color = new Color(0.18f, 1f, 0.05f, 1f);
        loadingProgressFillImage.type = Image.Type.Filled;
        loadingProgressFillImage.fillMethod = Image.FillMethod.Horizontal;
        loadingProgressFillImage.fillOrigin = (int)Image.OriginHorizontal.Left;
        loadingProgressFillImage.fillAmount = 0f;
        loadingProgressFillImage.raycastTarget = false;
    }

    private void UpdateGeneratedProgressBar(float progress)
    {
        if (loadingProgressFillRect == null)
            return;

        loadingProgressFillRect.anchorMax = new Vector2(Mathf.Clamp01(progress), 1f);
        loadingProgressFillRect.offsetMin = Vector2.zero;
        loadingProgressFillRect.offsetMax = Vector2.zero;

        if (loadingProgressFillImage != null)
        {
            loadingProgressFillImage.fillAmount = Mathf.Clamp01(progress);
            loadingProgressFillImage.enabled = progress > 0f;
        }
    }

    private async UniTask EnsureRequiredTitleResourcesLoaded()
    {
        string[] requiredPrefabKeys =
        {
            "UI_JoyStick",
            "UI_SkillSelectPopup",
            "UI_SkillCardItem",
            "UI_GameScene",
            "UI_GameResultPopup",
            "UI_LobbyScene",
            "UI_BattlePopup",
            "UI_ShopPopup",
            "UI_EquipmentPopup",
            "UI_EvolutionPopup",
            "UI_MaterialItem",
            "UI_GachaResultsPopup",
            "UI_EquipmentInfoPopup",
            "UI_EquipItem",
            "UI_Toast",
            "UI_MergePopup",
            "UI_MergeEquipItem",
            "UI_MergeResultPopup",
            "UI_MergeAllResultPopup",
            "UI_PausePopup",
            "UI_UserInfoItem",
            "UI_EquipmentResetPopup",
            "UI_RewardPopup",
            "UI_ToolTipItem",
            "UI_BuyItemPopup",
            "UI_GachaRateItem",
            "UI_GachaListPopup",
            "UI_DiaChargePopup",
            "UI_TotalDamagePopup",
            "UI_AchievementPopup",
            "UI_SkillDamageItem",
            "UI_CheckOutPopup",
            "UI_OfflineRewardPopup",
            "UI_MissionPopup",
            "UI_StaminaChargePopup",
            "UI_StageSelectPopup",
            "UI_BackToHomePopup",
            "UI_SettingPopup",
            "UI_CheckOutItem",
            "UI_MissionItem",
            "UI_AchievementItem",
            "UI_BeginnerSupportRewardPopup",
            "UI_MonsterInfoItem",
            "UI_StageInfoItem",
            "UI_GameoverPopup",
            "UI_ContinuePopup",
            "UI_FastRewardPopup",
            "UI_BackToBattlePopup",
            "UI_CharacterLevelupPopup",
            "UI_CharacterSelectPopup",
            "UI_CharacterItem",
            "UI_EvolutioninfoPopup",
            "SceneChangeAnimation_In",
            "SceneChangeAnimation_Out",
            "Player",
            "Monster",
            "NH_BasicAlien",
            "NH_KongKongi",
            "NH_KkuBeogi",
            "NH_MeongAli",
            "NH_SaengseonPpyeoByeong",
            "NH_PokeByeong",
            "EliteMonster",
            "Boss",
            "Map_01",
            "Map_02",
            "Map_03",
            "Map_04",
            "Map_05",
            "DropItem",
            "DropBox_Effect_Normal",
            "DropBox_Effect_Rare",
            "DropBox_Effect_Unique",
            "HealEffect",
            "Revival",
            "DamageFont",
            "CriticalDamageFont",
            "SkillRange",
            "BossSkill",
            "BossSkillHitEffect",
            "SlimeFall",
            "MagneticField",
        };

        AddLoadingTotal(requiredPrefabKeys.Length);
        UpdateLoadingProgress(loadingLoadedAssetCount, loadingTotalAssetCount);

        foreach (string key in requiredPrefabKeys)
        {
            await LoadRequiredAsset<GameObject>(key);
            MarkLoadingAssetLoaded();
        }

        string[] requiredDataKeys =
        {
            "SkillData.json",
            "SkillEvolutionData.json",
            "StageData.json",
            "CreatureData.json",
            "LevelData.json",
            "EquipmentLevelData.json",
            "EquipmentData.json",
            "MaterialData.json",
            "SpecialSkillData.json",
            "DropItemData.json",
            "GachaData.json",
            "AttendanceCheckData.json",
            "MissionData.json",
            "AchievementData.json",
            "OfflineRewardData.json",
            "CharacterLevelData.json",
            "EvolutionData.json",
        };

        AddLoadingTotal(requiredDataKeys.Length);
        UpdateLoadingProgress(loadingLoadedAssetCount, loadingTotalAssetCount);

        foreach (string key in requiredDataKeys)
        {
            await LoadRequiredAsset<TextAsset>(key);
            MarkLoadingAssetLoaded();
        }

        string[] requiredSpriteKeys =
        {
            "MapSprite_01.sprite",
            "MapSprite_02.sprite",
            "MapSprite_03.sprite",
            "MapSprite_04.sprite",
            "MapSprite_05.sprite",
            "Alpha.sprite",
            "Beta.sprite",
            "Gamma.sprite",
            "Delta.sprite",
            "Epsilon.sprite",
            "Gold.sprite",
            "Dia.sprite",
            "Stamina.sprite",
            "LevelUpCoupon.sprite",
            "RedGem.sprite",
            "GreenGem.sprite",
            "BlueGem.sprite",
            "GoldGem.sprite",
        };

        AddLoadingTotal(requiredSpriteKeys.Length);
        UpdateLoadingProgress(loadingLoadedAssetCount, loadingTotalAssetCount);

        foreach (string key in requiredSpriteKeys)
        {
            await LoadRequiredAsset<Sprite>(key);
            MarkLoadingAssetLoaded();
        }

        string[] requiredAnimatorKeys =
        {
            "Player_Alpha_Anim",
            "Player_Beta_Anim",
            "Player_Gamma_Anim",
            "Player_Delta_Anim",
            "Player_Epsilon_Anim",
            "Character_Alpha",
            "Character_Beta",
            "Character_Gamma",
            "Character_Delta",
            "Character_Epsilon",
            "Normal_Potion_Anim",
            "Good_Potion_Anim",
            "Best_Potion_Anim",
        };

        AddLoadingTotal(requiredAnimatorKeys.Length);
        UpdateLoadingProgress(loadingLoadedAssetCount, loadingTotalAssetCount);

        foreach (string key in requiredAnimatorKeys)
        {
            await LoadRequiredAsset<RuntimeAnimatorController>(key);
            MarkLoadingAssetLoaded();
        }

        foreach (string key in requiredPrefabKeys)
        {
            if (Manager.ResourceM.Load<GameObject>(key) == null)
                throw new System.Exception($"Required resource missing after preload: {key}");
        }

        foreach (string key in requiredDataKeys)
        {
            if (Manager.ResourceM.Load<TextAsset>(key) == null)
                throw new System.Exception($"Required resource missing after preload: {key}");
        }

        foreach (string key in requiredSpriteKeys)
        {
            if (Manager.ResourceM.Load<Sprite>(key) == null)
                throw new System.Exception($"Required resource missing after preload: {key}");
        }

        foreach (string key in requiredAnimatorKeys)
        {
            if (Manager.ResourceM.Load<RuntimeAnimatorController>(key) == null)
                throw new System.Exception($"Required resource missing after preload: {key}");
        }
    }

    private async UniTask LoadRequiredAsset<T>(string key) where T : Object
    {
        if (Manager.ResourceM.Load<T>(key) != null)
            return;

        await Manager.ResourceM.LoadAsync<T>(key);
    }

    private async UniTask EnsureDataDrivenSpritesLoaded()
    {
        HashSet<string> spriteKeys = new HashSet<string>();

        foreach (var data in Manager.DataM.StageDic.Values)
            AddSpriteKey(spriteKeys, data.StageImage);

        foreach (var data in Manager.DataM.CreatureDic.Values)
            AddSpriteKey(spriteKeys, data.Image_Name);

        foreach (var data in Manager.DataM.SkillDic.Values)
            AddSpriteKey(spriteKeys, data.SkillIcon);

        foreach (var data in Manager.DataM.SkillEvolutionDic.Values)
            AddSpriteKey(spriteKeys, data.EvolutionItemIcon);

        foreach (var data in Manager.DataM.EquipmentDic.Values)
            AddSpriteKey(spriteKeys, data.SpriteName);

        foreach (var data in Manager.DataM.MaterialDic.Values)
            AddSpriteKey(spriteKeys, data.SpriteName);

        foreach (var data in Manager.DataM.DropItemDic.Values)
            AddSpriteKey(spriteKeys, data.SpriteName);

        foreach (var data in Manager.DataM.EvolutionDataDic.Values)
            AddSpriteKey(spriteKeys, data.SpriteName);

        AddSpriteKey(spriteKeys, "RedGem.sprite");
        AddSpriteKey(spriteKeys, "GreenGem.sprite");
        AddSpriteKey(spriteKeys, "BlueGem.sprite");
        AddSpriteKey(spriteKeys, "GoldGem.sprite");

        AddLoadingTotal(spriteKeys.Count);
        UpdateLoadingProgress(loadingLoadedAssetCount, loadingTotalAssetCount);

        foreach (string key in spriteKeys)
        {
            await LoadRequiredAsset<Sprite>(key);
            MarkLoadingAssetLoaded();

            if (Manager.ResourceM.Load<Sprite>(key) == null)
                Debug.LogError($"Data-driven sprite missing after preload: {key}");
        }
    }

    private void AddSpriteKey(HashSet<string> spriteKeys, string key)
    {
        if (!string.IsNullOrWhiteSpace(key))
            spriteKeys.Add(key);
    }


    //Manager.ResourceM.LoadAllAsync<Sprite>("Sprite", (key, loadCount, maxCount) =>
    //{
    //    if (loadCount == 1)
    //    {
    //        totalExpectedAssetCount += maxCount;
    //    }
    //    currentLoadedAssetCount++;
    //    UpdateLoadingUI(currentLoadedAssetCount, totalExpectedAssetCount);

    //    if (loadCount == maxCount)
    //    {
    //        completedLoadOperations++;
    //        CheckAllLoadsCompleted();
    //    }
    //});

    //// 2. 일반 Object 에셋 로드 시작
    //// "PrevLoad" 라벨에 할당된 모든 Object 에셋을 로드합니다.
    //Manager.ResourceM.LoadAllAsync<Object>("PrevLoad", (key, loadCount, maxCount) =>
    //{
    //    // 첫 번째 콜백에서 해당 라벨의 총 에셋 수를 totalExpectedAssetCount에 더해줍니다.
    //    if (loadCount == 1)
    //    {
    //        totalExpectedAssetCount += maxCount;
    //    }
    //    // 현재 로드된 에셋 수를 증가시키고 UI를 업데이트합니다.
    //    currentLoadedAssetCount++;
    //    UpdateLoadingUI(currentLoadedAssetCount, totalExpectedAssetCount);

    //    // 해당 라벨의 모든 에셋 로드가 완료되면 완료된 작업 수를 증가시키고 전체 완료 여부를 확인합니다.
    //    if (loadCount == maxCount)
    //    {
    //        completedLoadOperations++;
    //        CheckAllLoadsCompleted();
    //    }
    //});

    public void StartAnim()
    {
        Vector3 OriginPos = GetText(typeof(Texts), (int)Texts.StartText).transform.localScale;

        startTextTween?.Kill();
        startTextTween = GetText(typeof(Texts), (int)Texts.StartText).transform.DOScale(OriginPos * 1.5f, 0.5f)
        .SetLoops(-1, LoopType.Yoyo)
        .SetEase(Ease.InOutSine);
    }

    private async UniTaskVoid OnClickStartButton()
    {
        try
        {
            GetButton(typeof(Buttons), (int)Buttons.StartButton).interactable = false;
            Debug.Log("TitleScene StartButton clicked. Loading LobbyScene.");
            await Manager.SceneM.LoadSceneAsync(Define.SceneType.LobbyScene);
        }
        catch (System.Exception e)
        {
            Debug.LogError($"Failed to load LobbyScene from title: {e}");
            GetButton(typeof(Buttons), (int)Buttons.StartButton).interactable = true;
        }
    }

}
