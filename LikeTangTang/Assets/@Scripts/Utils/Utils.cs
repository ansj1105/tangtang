
using System;
using System.Collections;
using System.Collections.Generic;
using System.Text.RegularExpressions;
using UnityEditor;
using UnityEngine;
using UnityEngine.EventSystems;


public static class Utils
{
    public const float MobileFrameAspect = 9.3f / 16f;

    public static Rect GetMobileGameplayFrame(Camera camera = null)
    {
        camera ??= Camera.main;
        if (camera == null || !camera.orthographic)
            return new Rect(float.NegativeInfinity, float.NegativeInfinity, float.PositiveInfinity, float.PositiveInfinity);

        Vector3 camPos = camera.transform.position;
        float halfHeight = camera.orthographicSize;
        float halfWidth = halfHeight * MobileFrameAspect;

        return Rect.MinMaxRect(
            camPos.x - halfWidth,
            camPos.y - halfHeight,
            camPos.x + halfWidth,
            camPos.y + halfHeight);
    }

    public static bool IsInsideMobileGameplayFrame(Vector3 worldPos, Camera camera = null)
    {
        camera ??= Camera.main;
        if (camera == null || !camera.orthographic)
            return true;

        Vector3 viewportPoint = camera.WorldToViewportPoint(worldPos);
        if (viewportPoint.z <= 0f)
            return false;

        return GetMobileGameplayFrame(camera).Contains(new Vector2(worldPos.x, worldPos.y));
    }

    //Get???대낯?ㅼ쓬 ?놁쑝硫?異붽?, ?덉쑝硫?由ы꽩
    public static T GetOrAddComponent<T>(this GameObject go) where T : Component
    {
        if (go == null) return null;
        T component = go.GetComponent<T>();
        if (component == null)
            component = go.AddComponent<T>();

        return component;
    }

    //?먯떇李얘린.
    public static GameObject FindChild(GameObject go, string name = null, bool recursive = false)
    {
        Transform tr = FindChild<Transform>(go, name, recursive);
        if (tr == null) return null;

        return tr.gameObject;
    }

    //怨꾩링援ъ“ ?고븯?먯꽌 臾쇱껜瑜?李얘퀬?띠쓣??
    public static T FindChild<T>(GameObject go, string name = null, bool recursive = false) where T : UnityEngine.Object
    {
        if (go == null) return null;

        if (recursive == false)
        {
            for (int i = 0; i < go.transform.childCount; i++)
            {
                Transform tr = go.transform.GetChild(i);
                if (string.IsNullOrEmpty(name) || tr.name == name)
                {
                    T component = tr.GetComponent<T>();
                    if (component != null) return component;
                }
            }
        }
        else
        {
            foreach (T component in go.GetComponentsInChildren<T>())
            {
                if (string.IsNullOrEmpty(name) || component.name == name)
                {
                    return component;
                }
            }
        }

        return null;
    }

    public static bool IsValid(this GameObject _go)
    {
        return _go != null && _go.activeSelf;
    }

    public static bool IsValid(this BaseController _bc)
    {
        return _bc != null && _bc.isActiveAndEnabled;
    }

    public static Vector2 CreateMonsterSpawnPoint(Vector2 _CharacterPos, float _minDist = 10.0f, float _maxDist = 20.0f)
    {
        Camera camera = Camera.main;
        if (camera == null || !camera.orthographic)
        {
            float angle = UnityEngine.Random.Range(0, 360) * Mathf.Deg2Rad;
            float dist = UnityEngine.Random.Range(_maxDist, _maxDist + 10f);
            return _CharacterPos + new Vector2(Mathf.Cos(angle), Mathf.Sin(angle)) * dist;
        }

        Vector3 camPos = camera.transform.position;
        Rect mobileFrame = GetMobileGameplayFrame(camera);
        float margin = Mathf.Max(_minDist, 8f);
        float extra = Mathf.Max(_maxDist - _minDist, 4f);

        float left = mobileFrame.xMin;
        float right = mobileFrame.xMax;
        float bottom = mobileFrame.yMin;
        float top = mobileFrame.yMax;

        Vector2 spawnPos;
        int side = UnityEngine.Random.Range(0, 4);
        switch (side)
        {
            case 0:
                spawnPos = new Vector2(left - UnityEngine.Random.Range(margin, margin + extra), UnityEngine.Random.Range(bottom - extra, top + extra));
                break;
            case 1:
                spawnPos = new Vector2(right + UnityEngine.Random.Range(margin, margin + extra), UnityEngine.Random.Range(bottom - extra, top + extra));
                break;
            case 2:
                spawnPos = new Vector2(UnityEngine.Random.Range(left - extra, right + extra), bottom - UnityEngine.Random.Range(margin, margin + extra));
                break;
            default:
                spawnPos = new Vector2(UnityEngine.Random.Range(left - extra, right + extra), top + UnityEngine.Random.Range(margin, margin + extra));
                break;
        }

        Vector2 rawSpawnPos = spawnPos;
        if (Manager.GameM?.CurrentMap != null)
        {
            Vector2 halfMap = Manager.GameM.CurrentMap.MapSize * 0.5f;
            spawnPos.x = Mathf.Clamp(spawnPos.x, -halfMap.x, halfMap.x);
            spawnPos.y = Mathf.Clamp(spawnPos.y, -halfMap.y, halfMap.y);

            bool insideExpandedView =
                spawnPos.x > left - margin && spawnPos.x < right + margin &&
                spawnPos.y > bottom - margin && spawnPos.y < top + margin;

            if (insideExpandedView)
            {
                Vector2 dir = (spawnPos - (Vector2)camPos).normalized;
                if (dir == Vector2.zero)
                    dir = UnityEngine.Random.insideUnitCircle.normalized;

                float targetX = dir.x >= 0 ? right + margin : left - margin;
                float targetY = dir.y >= 0 ? top + margin : bottom - margin;

                if (Mathf.Abs(dir.x) > Mathf.Abs(dir.y))
                    spawnPos.x = Mathf.Clamp(targetX, -halfMap.x, halfMap.x);
                else
                    spawnPos.y = Mathf.Clamp(targetY, -halfMap.y, halfMap.y);
            }

            bool stillVisible =
                spawnPos.x > left && spawnPos.x < right &&
                spawnPos.y > bottom && spawnPos.y < top;

            if (stillVisible)
                spawnPos = rawSpawnPos;
        }

        return spawnPos;
    }

    public static void BindEvent(this GameObject _go, Action _action = null, Action<BaseEventData> _dragAction = null, Define.UIEvent _type = Define.UIEvent.Click)
    {
        UI_Base.BindEvent(_go, _action, _dragAction, _type);
    }

    public static Vector2 CreateObjectAroundPlayer(Vector3 _pos, float _minDist = 6f, float _maxDist = 10f)
    {
        float angle = UnityEngine.Random.Range(0f, 360f);
        float radius = angle * Mathf.Deg2Rad;

        float dist = UnityEngine.Random.Range(_minDist, _maxDist);


        Vector2 spawnPos = new Vector2(Mathf.Cos(radius), Mathf.Sin(radius)) * dist;
        Vector3 pos = _pos + new Vector3(spawnPos.x, spawnPos.y, 0f);

        return pos;
    }

    //NOTE : SKillType ?듭씪 ?쒗궎?ㅺ퀬
    public static Define.SkillType GetSkillTypeFromInt(int _value)
    {
        foreach (Define.SkillType type in Enum.GetValues(typeof(Define.SkillType)))
        {
            int minValue = (int)type;
            int maxValue = minValue + 5;

            if (_value >= minValue && _value <= maxValue)
            {
                return type;
            }
        }

        return Define.SkillType.None;
    }

    public static Define.EvoloutionType GetEvolutionSkillTypeFromInt(int _value)
    {
        foreach (Define.EvoloutionType type in Enum.GetValues(typeof(Define.EvoloutionType)))
        {
            return type;
        }

        return Define.EvoloutionType.None;
    }

    public static MonsterController FindClosestMonster(Vector3 _origin, HashSet<MonsterController> _prevTargets = null)
    {
        float closestDist = Mathf.Infinity;
        MonsterController closestMC = null;

        foreach (MonsterController monster in Manager.ObjectM.mcSet)
        {
            if (!monster.IsValid()) continue;
            if (!monster.IsInsideCameraView()) continue;
            if (_prevTargets != null && _prevTargets.Contains(monster)) continue;


            float dist = Vector3.Distance(_origin, monster.transform.position);
            if (dist < closestDist)
            {
                closestDist = dist;
                closestMC = monster;
            }
        }

        return closestMC;
    }

    public static void DestroyChilds(this GameObject _go)
    {
        Transform[] children = new Transform[_go.transform.childCount];

        for (int i = 0; i < _go.transform.childCount; i++)
        {
            children[i] = _go.transform.GetChild(i);
        }

        foreach (Transform child in children)
        {
            Manager.ResourceM.Destory(child.gameObject);
        }
    }



    //List瑜??욎뼱以??
    public static void Shuffle<T>(this List<T> _list)
    {
        int count = _list.Count;

        while (count > 1)
        {
            count--;
            int randNum = UnityEngine.Random.Range(0, count + 1);
            T value = _list[randNum];
            _list[randNum] = _list[count];
            _list[count] = value;
        }
    }


    public static Color HexToColor(string _color)
    {
        Color parsedColor;
        ColorUtility.TryParseHtmlString("#" + _color, out parsedColor);

        return parsedColor;
    }

    public static int GetUpgradeNumber(Enum _grade)
    {
        var match = Regex.Match(_grade.ToString(), @"\d+");
        return match.Success ? int.Parse(match.Value) : 0;
    }

}

