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

        SetInfo().Forget();
        return true;
    }

    private int completedLoadOperations = 0; // 완료된 LoadAllAsync 호출 수
    private int totalExpectedLoadOperations = 2; // 총 LoadAllAsync 호출 수 (Sprite + PrevLoad)

    private int currentLoadedAssetCount = 0; // 현재까지 로드된 총 에셋 수
    private int totalExpectedAssetCount = 0; // 로드할 총 에셋 수

    async UniTask SetInfo()
    {
        try
        {
            string alwaysKeepLabel = "AlwaysKeep";
            string NeedReleaseLabel = "NeedRelease";

            int alwaysKeepMax = 0;
            await Manager.ResourceM.LoadGroupAsync<Object>(alwaysKeepLabel, (key, count, max) =>
            {
                alwaysKeepMax = max;
                UpdateLoadingProgress(count, max);
            });

            await Manager.ResourceM.LoadGroupAsync<Object>(NeedReleaseLabel, (key, count, max) =>
            {
                UpdateLoadingProgress(alwaysKeepMax + count, alwaysKeepMax + max);
            });

            await Manager.ResourceM.LoadGroupByTypeAsync<Sprite>(alwaysKeepLabel);
            await Manager.ResourceM.LoadGroupByTypeAsync<Sprite>(NeedReleaseLabel);
            await Manager.ResourceM.LoadGroupByTypeAsync<RuntimeAnimatorController>(alwaysKeepLabel);
            await Manager.ResourceM.LoadGroupByTypeAsync<RuntimeAnimatorController>(NeedReleaseLabel);

            await EnsureRequiredTitleResourcesLoaded();

            Manager.DataM.Init();
            await EnsureDataDrivenSpritesLoaded();
            Manager.GameM.Init();
            Manager.TimeM.Init();
            Manager.AdM.Init();

            isLoadEnd = true;
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

        if (countText != null)
            countText.text = $"{current} / {total}";
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

        foreach (string key in requiredPrefabKeys)
        {
            await LoadRequiredAsset<GameObject>(key);
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

        foreach (string key in requiredDataKeys)
        {
            await LoadRequiredAsset<TextAsset>(key);
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

        foreach (string key in requiredSpriteKeys)
        {
            await LoadRequiredAsset<Sprite>(key);
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

        foreach (string key in requiredAnimatorKeys)
        {
            await LoadRequiredAsset<RuntimeAnimatorController>(key);
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

        foreach (string key in spriteKeys)
        {
            await LoadRequiredAsset<Sprite>(key);

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


    void UpdateLoadingUI(int current, int total)
    {
        GetSlider(typeof(Sliders), (int)Sliders.Slider).value = (float)current / total;
        GetText(typeof(Texts), (int)Texts.CountText).text = $"{current} / {total}";
    }

    // 모든 로드 작업이 완료되었는지 확인
    void CheckAllLoadsCompleted()
    {
        // 모든 LoadAllAsync 호출이 완료되었는지 확인
        if (completedLoadOperations == totalExpectedLoadOperations)
        {
            isLoadEnd = true;
            GetButton(typeof(Buttons), (int)Buttons.StartButton).gameObject.SetActive(true);
            GetText(typeof(Texts), (int)Texts.StartText).gameObject.SetActive(true);

            // 모든 초기화 작업 실행
            Manager.DataM.Init();
            Manager.GameM.Init();
            Manager.TimeM.Init();
            Manager.AdM.Init();

            StartAnim();
        }
    }


    public void StartAnim()
    {
        Vector3 OriginPos = GetText(typeof(Texts), (int)Texts.StartText).transform.localScale;

        GetText(typeof(Texts), (int)Texts.StartText).transform.DOScale(OriginPos * 1.5f, 0.5f)
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
