using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using System;
using System.Linq;
using UnityEngine.AddressableAssets;
using UnityEngine.ResourceManagement.AsyncOperations;
using Object = UnityEngine.Object;


public class ResourceManager
{
    Dictionary<string, Object> resourceDic = new Dictionary<string, Object>();
    Dictionary<string, string> keyToLabelDic = new Dictionary<string, string>();
    public Dictionary<string, Object> ResourceDic { get; }


    public T Load<T>(string _key) where T : Object
    {
        if (resourceDic.TryGetValue(_key, out Object resource))
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
    public void LoadAsync<T>(string _key, Action<T> _cb = null) where T : Object
    {

        if (resourceDic.TryGetValue(_key, out Object resource))
        {
            _cb?.Invoke(resource as T);
            return;
        }



        if (_key.Contains(".sprite"))
        {

            var spriteHandle = Addressables.LoadAssetAsync<Sprite>(_key);
            spriteHandle.Completed += (oper) =>
            {
                if (oper.Status == AsyncOperationStatus.Succeeded)
                {
                    if (oper.Result != null)
                    {
                        resourceDic.Add(_key, oper.Result);
                        _cb?.Invoke(oper.Result as T);
                    }
                    else
                    {
                        Debug.LogError($"LoadAsync 실패 : 에셋에 키값 없음 {_key}");
                        _cb?.Invoke(null);
                    }
                }
                else
                {
                    Debug.LogError($"LoadAsync 실패 : {_key} . Exception : {oper.OperationException}");
                }
            };

            return;
        }


        var asyncOperationHandle = Addressables.LoadAssetAsync<T>(_key);
        asyncOperationHandle.Completed += (oper) =>
        {
            if (oper.Status == AsyncOperationStatus.Succeeded)
            {
                if (oper.Result != null)
                {
                    resourceDic.Add(_key, oper.Result);
                    _cb?.Invoke(oper.Result as T);
                }
                else
                {
                    Debug.LogError($"LoadAsync 실패 : 에셋에 키값 없음 {_key}");
                    _cb?.Invoke(null);
                }
            }
            else
            {
                Debug.LogError($"LoadAsync 실패 : {_key} . Exception : {oper.OperationException}");
            }
        };
    }

    public void LoadAllAsync<T>(string _label, Action<string, int, int> _cb = null) where T : Object
    {
        var asyncOperationHandle = Addressables.LoadResourceLocationsAsync(_label, typeof(T));
        asyncOperationHandle.Completed += (oper) =>
        {
            if (oper.Status != AsyncOperationStatus.Succeeded)
            {
                Debug.LogError("LoadResourceLocationAsync 실패 : {_label}에서");
                return;
            }

            int loadCount = 0;
            int maxCount = oper.Result.Count;
            if (maxCount == 0)
            {
                _cb?.Invoke("", 0, 0);
                Addressables.Release(oper);
                return;
            }


            foreach (var result in oper.Result)
            {
                string key = result.PrimaryKey;


                if (!keyToLabelDic.ContainsKey(key))
                {
                    keyToLabelDic.Add(key, _label);
                }

                if (key.Contains(".sprite"))
                {
                    LoadAsync<Sprite>(result.PrimaryKey, (obj) =>
                                        {
                                            loadCount++;
                                            _cb?.Invoke(key, loadCount, maxCount);
                                            if (loadCount == maxCount)
                                            {
                                                Addressables.Release(oper);
                                            }
                                        });
                }
                else
                {
                    LoadAsync<T>(result.PrimaryKey, (obj) =>
                    {
                        loadCount++;
                        _cb?.Invoke(key, loadCount, maxCount);
                        if (loadCount == maxCount)
                        {
                            Addressables.Release(oper);
                        }
                    });
                }

            }
        };
    }

    public void LoadGroupAsync<T>(string _label, Action<string, int, int> _cb = null) where T : Object
    {
        var asyncOperationHandle = Addressables.LoadResourceLocationsAsync(_label, typeof(T));
        asyncOperationHandle.Completed += (oper) =>
        {
            if (oper.Status != AsyncOperationStatus.Succeeded)
            {
                Debug.LogError("LoadResourceLocationAsync 실패 : {_label}에서");
                return;
            }

            int loadCount = 0;
            int maxCount = oper.Result.Count;
            if (maxCount == 0)
            {
                _cb?.Invoke("", 0, 0);
                Addressables.Release(oper);
                return;
            }

            foreach (var result in oper.Result)
            {
                string key = result.PrimaryKey;
                if (!keyToLabelDic.ContainsKey(key))
                {
                    keyToLabelDic.Add(key, _label);
                }

                if (key.Contains(".sprite"))
                {
                    LoadAsync<Sprite>(result.PrimaryKey, (obj) =>
                                        {
                                            loadCount++;
                                            _cb?.Invoke(key, loadCount, maxCount);
                                            if (loadCount == maxCount)
                                            {
                                                Addressables.Release(oper);
                                            }
                                        });
                }
                else
                {
                    LoadAsync<T>(result.PrimaryKey, (obj) =>
                    {
                        loadCount++;
                        _cb?.Invoke(key, loadCount, maxCount);
                        if (loadCount == maxCount)
                        {
                            Addressables.Release(oper);
                        }
                    });
                }

            }
        };
    }


    public void UnLoadGroup(string _label)
    {
        var keysToUnLoad = keyToLabelDic.Where(x => x.Value == _label).Select(x => x.Key).ToList();
        if (keysToUnLoad.Count == 0) return;

        foreach (var key in keysToUnLoad)
        {
            UnLoad(key);
        }
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



