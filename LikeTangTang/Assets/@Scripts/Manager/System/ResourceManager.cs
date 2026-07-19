using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using System;
using System.Linq;
using UnityEngine.AddressableAssets;
using UnityEngine.ResourceManagement.AsyncOperations;
using UnityEngine.ResourceManagement.ResourceLocations;
using Object = UnityEngine.Object;
using Cysharp.Threading.Tasks;

public class ResourceManager
{
    Dictionary<string, Object> resourceDic = new Dictionary<string, Object>();
    Dictionary<string, string> keyToLabelDic = new Dictionary<string, string>();
    private static readonly Dictionary<string, string> resourceKeyAliasDic = new Dictionary<string, string>()
    {
        { "EnchanStone_Weapon.sprite", "EnchantStone_Weapon.sprite" },
        { "EnchanStone_Glove.sprite", "EnchantStone_Glove.sprite" },
        { "EnchanStone_Ring.sprite", "EnchantStone_Ring.sprite" },
        { "EnchanStone_Rign.sprite", "EnchantStone_Ring.sprite" },
        { "EnchanStone_Helmet.sprite", "EnchantStone_Helmet.sprite" },
        { "EnchanStone_Armor.sprite", "EnchantStone_Armor.sprite" },
        { "EnchanStone_Boots.sprite", "EnchantStone_Boots.sprite" },
        { "Grove_03.sprite", "Glove_03.sprite" },
        { "EquipmentBox_Random.sprite", "EqptBox_Icon.sprite" },
        { "EquipmentBox_AllRandom.sprite", "EqptBox_Icon.sprite" },
    };

    public Dictionary<string, Object> ResourceDic { get; }


    public T Load<T>(string _key) where T : Object
    {
        if (resourceDic.TryGetValue(_key, out Object resource))
        {
            return resource as T;
        }

        string resolvedKey = ResolveResourceKey(_key);
        if (resolvedKey != _key && resourceDic.TryGetValue(resolvedKey, out resource))
        {
            return resource as T;
        }

        return null;
    }

    public GameObject Instantiate(string _key, Transform _parent = null, bool _pooling = false)
    {
        GameObject prefab = Load<GameObject>(_key);
        if (prefab == null)
        {
            Debug.LogError("키에 맞는 프리팹이 없음!!, ResourceManager 29Line");
            return null;
        }

        if (_pooling)
            return Manager.PoolM.Pop(prefab);

        GameObject go = GameObject.Instantiate(prefab, _parent);
        go.name = prefab.name;
        return go;
    }

    public void Destory(GameObject _go)
    {
        if (_go == null) return;

        if (Manager.PoolM.Push(_go))
            return;

        Object.Destroy(_go);
    }

    #region 비동기 코드 로딩(Addressable)
    public async UniTask<T> LoadAsync<T>(string _key) where T : Object
    {

        string resolvedKey = ResolveResourceKey(_key);
        if (resourceDic.TryGetValue(resolvedKey, out Object resource))
        {
            return resource as T;
        }

        Object resultAseet = null;

        try
        {
            if (resolvedKey.Contains(".sprite"))
            {
                resultAseet = await Addressables.LoadAssetAsync<Sprite>(resolvedKey).ToUniTask();
            }
            else
            {
                resultAseet = await Addressables.LoadAssetAsync<T>(resolvedKey).ToUniTask();
            }

        }
        catch (Exception e)
        {
            Debug.LogError($"LoadAsync 실패 : {_key} -> {resolvedKey}. :  {e.Message}");
            return null;
        }


        if (resultAseet != null)
        {
            resourceDic[resolvedKey] = resultAseet;
            return resultAseet as T;
        }
        else
        {
            Debug.LogError($"LoadAsync 실패:  에셋에 키값 없음 {_key} -> {resolvedKey}");
            return null;
        }
    }

    private string ResolveResourceKey(string key)
    {
        if (string.IsNullOrEmpty(key))
            return key;

        if (resourceKeyAliasDic.TryGetValue(key, out string resolvedKey))
            return resolvedKey;

        return key;
    }

    // public void LoadAllAsync<T>(string _label, Action<string, int, int> _cb = null) where T : Object
    // {
    //     var asyncOperationHandle = Addressables.LoadResourceLocationsAsync(_label, typeof(T));
    //     asyncOperationHandle.Completed += (oper) =>
    //     {
    //         if (oper.Status != AsyncOperationStatus.Succeeded)
    //         {
    //             Debug.LogError("LoadResourceLocationAsync 실패 : {_label}에서");
    //             return;
    //         }

    //         int loadCount = 0;
    //         int maxCount = oper.Result.Count;
    //         if (maxCount == 0)
    //         {
    //             _cb?.Invoke("", 0, 0);
    //             Addressables.Release(oper);
    //             return;
    //         }


    //         foreach (var result in oper.Result)
    //         {
    //             string key = result.PrimaryKey;


    //             if (!keyToLabelDic.ContainsKey(key))
    //             {
    //                 keyToLabelDic.Add(key, _label);
    //             }

    //             if (key.Contains(".sprite"))
    //             {
    //                 LoadAsync<Sprite>(result.PrimaryKey, (obj) =>
    //                                     {
    //                                         loadCount++;
    //                                         _cb?.Invoke(key, loadCount, maxCount);
    //                                         if (loadCount == maxCount)
    //                                         {
    //                                             Addressables.Release(oper);
    //                                         }
    //                                     });
    //             }
    //             else
    //             {
    //                 LoadAsync<T>(result.PrimaryKey, (obj) =>
    //                 {
    //                     loadCount++;
    //                     _cb?.Invoke(key, loadCount, maxCount);
    //                     if (loadCount == maxCount)
    //                     {
    //                         Addressables.Release(oper);
    //                     }
    //                 });
    //             }

    //         }
    //     };
    // }

    public async UniTask LoadGroupAsync<T>(string _label, Action<string, int, int> _cb = null) where T : Object
    {

        var locationHandle = Addressables.LoadResourceLocationsAsync(_label);
        IList<UnityEngine.ResourceManagement.ResourceLocations.IResourceLocation> locations;

        try
        {
            locations = await locationHandle.ToUniTask();
        }
        catch (Exception e)
        {
            Debug.LogError($"LoadResourceLocationsAsync실패 : {_label}. : {e.Message}");
            Addressables.Release(locationHandle);
            return;
        }

        if (locations == null || locations.Count == 0)
        {
            _cb?.Invoke("", 0, 0);
            Addressables.Release(locationHandle);
            return;
        }

        int loadCount = 0;
        int maxCount = locations.Count;

        var loadTasks = new List<UniTask>();

        foreach (var result in locations)
        {
            string key = result.PrimaryKey;

            if (!keyToLabelDic.ContainsKey(key))
            {
                keyToLabelDic.Add(key, _label);
            }

            UniTask loadTask = LoadLocationAsync(result).AsUniTask();

            //콜백 및 카운터 증가 로직 포함 완료처리 unitask
            //continuewith으로 로드. 끝날때마다 콜백 호출
            var completionTask = loadTask.ContinueWith(() =>
            {
                lock (this)
                {
                    loadCount++;
                    _cb?.Invoke(key, loadCount, maxCount);
                }
            });

            loadTasks.Add(completionTask);
        }

        //모든 로딩 작업 될때까지 대기
        await UniTask.WhenAll(loadTasks);

        Addressables.Release(locationHandle);
    }

    public async UniTask<int> CountGroupAsync(string _label)
    {
        var locationHandle = Addressables.LoadResourceLocationsAsync(_label);

        try
        {
            IList<IResourceLocation> locations = await locationHandle.ToUniTask();
            return locations?.Count ?? 0;
        }
        catch (Exception e)
        {
            Debug.LogError($"LoadResourceLocationsAsync 카운트 실패 : {_label}. : {e.Message}");
            return 0;
        }
        finally
        {
            Addressables.Release(locationHandle);
        }
    }

    public async UniTask LoadGroupByTypeAsync<T>(string _label, Action<string, int, int> _cb = null) where T : Object
    {
        var locationHandle = Addressables.LoadResourceLocationsAsync(_label, typeof(T));
        IList<IResourceLocation> locations;

        try
        {
            locations = await locationHandle.ToUniTask();
        }
        catch (Exception e)
        {
            Debug.LogError($"LoadResourceLocationsAsync 타입 로드 실패 : {_label} ({typeof(T).Name}). : {e.Message}");
            Addressables.Release(locationHandle);
            return;
        }

        if (locations == null || locations.Count == 0)
        {
            _cb?.Invoke("", 0, 0);
            Addressables.Release(locationHandle);
            return;
        }

        int loadCount = 0;
        int maxCount = locations.Count;
        var loadTasks = new List<UniTask>();

        foreach (var result in locations)
        {
            string key = result.PrimaryKey;

            if (!keyToLabelDic.ContainsKey(key))
            {
                keyToLabelDic.Add(key, _label);
            }

            loadTasks.Add(LoadKeyWithProgressAsync<T>(key, () =>
            {
                loadCount++;
                _cb?.Invoke(key, loadCount, maxCount);
            }));
        }

        await UniTask.WhenAll(loadTasks);
        Addressables.Release(locationHandle);
    }

    public async UniTask<int> CountGroupByTypeAsync<T>(string _label) where T : Object
    {
        var locationHandle = Addressables.LoadResourceLocationsAsync(_label, typeof(T));

        try
        {
            IList<IResourceLocation> locations = await locationHandle.ToUniTask();
            return locations?.Count ?? 0;
        }
        catch (Exception e)
        {
            Debug.LogError($"LoadResourceLocationsAsync 타입 카운트 실패 : {_label} ({typeof(T).Name}). : {e.Message}");
            return 0;
        }
        finally
        {
            Addressables.Release(locationHandle);
        }
    }

    private async UniTask LoadKeyWithProgressAsync<T>(string key, Action onLoaded) where T : Object
    {
        await LoadAsync<T>(key);

        lock (this)
        {
            onLoaded?.Invoke();
        }
    }

    private async UniTask<Object> LoadLocationAsync(IResourceLocation location)
    {
        string key = location.PrimaryKey;
        if (resourceDic.TryGetValue(key, out Object resource))
        {
            return resource;
        }

        Object resultAsset = null;

        try
        {
            if (key.Contains(".sprite") || location.ResourceType == typeof(Sprite))
                resultAsset = await Addressables.LoadAssetAsync<Sprite>(location).ToUniTask();
            else if (location.ResourceType == typeof(TextAsset))
                resultAsset = await Addressables.LoadAssetAsync<TextAsset>(location).ToUniTask();
            else if (location.ResourceType == typeof(GameObject))
                resultAsset = await Addressables.LoadAssetAsync<GameObject>(location).ToUniTask();
            else if (location.ResourceType == typeof(AudioClip))
                resultAsset = await Addressables.LoadAssetAsync<AudioClip>(location).ToUniTask();
            else if (location.ResourceType == typeof(Material))
                resultAsset = await Addressables.LoadAssetAsync<Material>(location).ToUniTask();
            else if (location.ResourceType == typeof(AnimationClip))
                resultAsset = await Addressables.LoadAssetAsync<AnimationClip>(location).ToUniTask();
            else if (location.ResourceType == typeof(RuntimeAnimatorController))
                resultAsset = await Addressables.LoadAssetAsync<RuntimeAnimatorController>(location).ToUniTask();
            else
                resultAsset = await Addressables.LoadAssetAsync<Object>(location).ToUniTask();
        }
        catch (Exception e)
        {
            Debug.LogError($"LoadLocationAsync 실패 : {key} ({location.ResourceType}). : {e}");
            return null;
        }

        if (resultAsset != null)
        {
            resourceDic[key] = resultAsset;
            return resultAsset;
        }

        Debug.LogError($"LoadLocationAsync 실패: 에셋에 키값 없음 {key} ({location.ResourceType})");
        return null;
    }


    public async UniTask UnLoadGroup(string _label)
    {
        var keysToUnLoad = keyToLabelDic.
            Where(x => x.Value == _label).
            Select(x => x.Key).
            ToList();

        if (keysToUnLoad.Count == 0) return;

        foreach (var key in keysToUnLoad)
        {
            UnLoad(key);
        }

        await UniTask.Yield();
    }


    public void UnLoad(string _key)
    {
        if (resourceDic.TryGetValue(_key, out Object resouce))
        {
            resourceDic.Remove(_key);
            keyToLabelDic.Remove(_key);
            Addressables.Release(resouce);
        }
    }

    public void UnLoadAll()
    {
        List<string> keys = resourceDic.Keys.ToList();
        foreach (var key in keys)
        {
            UnLoad(key);
        }
    }
    #endregion
}




