using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using Cysharp.Threading.Tasks;

public class SceneChangeAnimation_In : UI_Popup
{

    public async UniTask RunTimeAnimation(Define.SceneType _nextScene)
    {
        transform.localScale = Vector3.one;

        await UniTask.Delay(TimeSpan.FromSeconds(1f), ignoreTimeScale: true);
    }

}
