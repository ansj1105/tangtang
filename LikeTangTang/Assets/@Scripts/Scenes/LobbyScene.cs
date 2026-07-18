using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class LobbyScene : BaseScene
{

    Animator anim;
    public override void Init()
    {
        Debug.Log("LobbyScene.Init begin");
        base.Init();

        SceneType = Define.SceneType.LobbyScene;
        if (Manager.UiM.ShowSceneUI<UI_LobbyScene>() == null)
            Debug.LogError("LobbyScene failed to create UI_LobbyScene.");

        Screen.sleepTimeout = SleepTimeout.SystemSetting;

        RenderTexture rt = new RenderTexture(512, 512, 16);
        GameObject previewCameraObject = GameObject.Find("PreviewCamera");
        if (previewCameraObject == null || !previewCameraObject.TryGetComponent(out Camera cam))
        {
            Debug.LogError("LobbyScene missing PreviewCamera.");
            return;
        }

        cam.targetTexture = rt;
        var target = cam.targetTexture;
        GameObject characterObject = GameObject.Find("Character");
        if (characterObject == null || !characterObject.TryGetComponent(out anim))
        {
            Debug.LogError("LobbyScene missing Character animator.");
            return;
        }

        Character currentCharacter = Manager.GameM.CurrentCharacter;
        if (currentCharacter == null)
        {
            Debug.LogError("LobbyScene has no current character.");
            return;
        }

        int id = currentCharacter.DataId;
        if (!Manager.DataM.CreatureDic.TryGetValue(id, out Data.CreatureData creatureData))
        {
            Debug.LogError($"LobbyScene missing creature data id: {id}");
            return;
        }

        string anim_name = creatureData.CharacterAnimName;
        RuntimeAnimatorController controller = Manager.ResourceM.Load<RuntimeAnimatorController>(anim_name);
        if (controller == null)
        {
            Debug.LogError($"LobbyScene missing animator controller: {anim_name}");
            return;
        }

        anim.runtimeAnimatorController = controller;
        Manager.SceneM.Setup(cam, target, this);

        Manager.SoundM.Play(Define.Sound.Bgm, "Bgm_Lobby");
        Debug.Log("LobbyScene.Init complete");
        
    }

    public void ChangeCharacter()
    {
        Character currentCharacter = Manager.GameM.CurrentCharacter;
        if (currentCharacter == null || anim == null)
            return;

        int id = currentCharacter.DataId;
        if (!Manager.DataM.CreatureDic.TryGetValue(id, out Data.CreatureData creatureData))
            return;

        string anim_name = creatureData.CharacterAnimName;
        RuntimeAnimatorController controller = Manager.ResourceM.Load<RuntimeAnimatorController>(anim_name);
        if (controller != null)
            anim.runtimeAnimatorController = controller;
    }

    public override void Clear()
    {

    }
}
