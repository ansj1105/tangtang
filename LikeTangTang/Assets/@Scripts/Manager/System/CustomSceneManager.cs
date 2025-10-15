using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.SceneManagement;
using DG.Tweening;
using Cysharp.Threading.Tasks;
using Unity.VisualScripting;

public class CustomSceneManager
{
    public BaseScene CurrentScene { get { return GameObject.FindObjectOfType<BaseScene>(); } }

    public Camera cam;
    public RenderTexture cam_target;
    public LobbyScene lobbyScene;


    public void Setup(Camera _cam, RenderTexture _cam_target, LobbyScene _lobbyScene)
    {
        cam = _cam;
        cam_target = _cam_target;
        lobbyScene = _lobbyScene;

    }

    public async UniTask LoadSceneAsync(Define.SceneType _type, Transform _tr = null)
    {
        Manager.UpdateM.PauseTicking(true);
        await UniTask.Yield();

        if (CurrentScene.SceneType == Define.SceneType.TitleScene)
        {
            Manager.Clear();
            await SceneManager.LoadSceneAsync(GetScene(_type));

            Manager.UpdateM.PauseTicking(false);
            return;
        }
        else if (CurrentScene.SceneType == Define.SceneType.LobbyScene)
        {
            await Manager.ResourceM.LoadGroupAsync<UnityEngine.Object>("NeedRelease");
            await PlaySceneChangeAnimation(_type, _tr);
        }
        else if (CurrentScene.SceneType == Define.SceneType.GameScene)
        {
            Manager.ResourceM.UnLoadGroup("NeedRelease");

            await PlaySceneChangeAnimation(_type, _tr);
        }
        Manager.UpdateM.PauseTicking(false);
    }

    // public void LoadScene(Define.SceneType _type, Transform _tr = null) //씬 이동 애니메이션()
    // {
    //     if (CurrentScene.SceneType == Define.SceneType.TitleScene)
    //     {
    //         Manager.Clear();
    //         SceneManager.LoadScene(GetScene(_type));
    //         return;
    //     }

    //     if (CurrentScene.SceneType == Define.SceneType.LobbyScene)
    //     {
    //         Manager.ResourceM.LoadGroupAsync<UnityEngine.Object>("NeedRelease", (key, loadCount, maxCount) =>
    //         {
    //             if (loadCount == maxCount)
    //             {
    //                 PlaySceneChangeAnimation(_type, _tr);
    //             }

    //         });
    //     }
    //     else if (CurrentScene.SceneType == Define.SceneType.GameScene)
    //     {
    //         Manager.ResourceM.UnLoadGroup("NeedRelease");
    //         PlaySceneChangeAnimation(_type, _tr);
    //     }
    // }

    private async UniTask PlaySceneChangeAnimation(Define.SceneType _type, Transform _parent)
    {
        var animGo = Manager.ResourceM.Instantiate("SceneChangeAnimation_In");
        var anim = animGo.GetOrAddComponent<SceneChangeAnimation_In>();
        animGo.transform.SetParent(_parent, worldPositionStays: false);

        Time.timeScale = 1f;



        anim.SetInfo(_type, () =>
        {
            DOTween.KillAll();
            DOTween.Clear();
            Manager.UpdateM.Clear();
            Manager.ResourceM.Destory(Manager.UiM.SceneUI.gameObject);
            Manager.Clear();
            switch (_type)
            {
                case Define.SceneType.LobbyScene:
                    break;

                case Define.SceneType.GameScene:
                    break;
            }
            SceneManager.LoadSceneAsync(GetScene(_type));
        });
    }

    public string GetScene(Define.SceneType _type)
    {
        string sceneName = Enum.GetName(typeof(Define.SceneType), _type);
        return sceneName;
    }

    public void Clear()
    {
        CurrentScene.Clear();
    }
}

