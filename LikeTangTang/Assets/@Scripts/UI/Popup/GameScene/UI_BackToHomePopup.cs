using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using Cysharp.Threading.Tasks;
using System.Threading.Tasks;
public class UI_BackToHomePopup : UI_Popup
{
    enum GameObjects
    {
        ContentObject,

    }

    enum Buttons
    {
        ResumeButton,
        QuitButton
    }

    private void Awake()
    {
        Init();
    }
    private void OnEnable()
    {
        PopupOpenAnim(GetObject(gameObjectsType, (int)GameObjects.ContentObject));
    }

    public override bool Init()
    {
        if (!base.Init()) return false;
        gameObjectsType = typeof(GameObjects);
        ButtonsType = typeof(Buttons);

        BindObject(gameObjectsType);
        BindButton(ButtonsType);

        GetButton(ButtonsType, (int)Buttons.ResumeButton).gameObject.BindEvent(OnClickResumButton);
        GetButton(ButtonsType, (int)Buttons.QuitButton).gameObject.BindEvent(() =>
        {
            OnClickQuitButton().Forget();

        });
        return true;
    }

    void OnClickResumButton()
    {
        Manager.SoundM.PlayButtonClick();
        Manager.UiM.ClosePopup(this);
    }

    async UniTask OnClickQuitButton()
    {
        Manager.SoundM.PlayButtonClick();

        Manager.GameM.isGameEnd = true;
        Manager.GameM.player.StopAllCoroutines();

        StageClearInfoData info;
        if (Manager.GameM.StageClearInfoDic.TryGetValue(Manager.GameM.CurrentStageData.StageIndex, out info))
        {
            if (Manager.GameM.CurrentWaveIndex > info.MaxWaveIndex)
            {
                info.MaxWaveIndex = Manager.GameM.CurrentWaveIndex;
                Manager.GameM.StageClearInfoDic[Manager.GameM.CurrentStageData.StageIndex] = info;
            }
        }

        Manager.GameM.ClearContinueData();
        await Manager.SceneM.LoadSceneAsync(Define.SceneType.LobbyScene, transform);
    }
}
