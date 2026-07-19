using System.Collections;
using System.Collections.Generic;
using System.Diagnostics;
using UnityEngine;


public class Manager : MonoBehaviour
{

    //드래그 드랍방식이아닌 코드에서 생성
    static Manager instance;
    static bool init = false;


    //매니저를 분할해서 사용
    #region Contents
    GameManager gameM = new GameManager();
    ObjectManager objectM = new ObjectManager();
    PoolManager poolM = new PoolManager();
    AchievementManager achievementM = new AchievementManager();
    AdManager adM = new AdManager();

    public static GameManager GameM { get { return Instance?.gameM; } }
    public static ObjectManager ObjectM { get { return Instance?.objectM; } }
    public static PoolManager PoolM { get { return Instance?.poolM; } }
    public static AchievementManager AchievementM { get { return Instance?.achievementM; } }

    public static AdManager AdM { get { return Instance?.adM; }}

    #endregion

    //엔진쪽에서 사용하는 부분(모든 게임에서 똑같은 부분)
    #region System
    DataManager dataM = new DataManager();
    ResourceManager resourceM = new ResourceManager();
    CustomSceneManager sceneM = new CustomSceneManager();
    SoundManager soundM = new SoundManager();
    UIManager uiM = new UIManager();
    TimeManager timeM = new TimeManager();
    SpawnManager spawnM = new SpawnManager();
    UpdateManager updateM;


    public static DataManager DataM { get { return Instance?.dataM; } }
    public static ResourceManager ResourceM {get { return Instance?.resourceM; } }
    public static CustomSceneManager SceneM { get { return Instance?.sceneM; } }
    public static SoundManager SoundM { get { return Instance?.soundM; } }
    public static UIManager UiM { get { return Instance?.uiM; } }

    public static SpawnManager SpawnM { get { return Instance?.spawnM; } }
    public static UpdateManager UpdateM {get {return Instance?.updateM;}}
    public static TimeManager TimeM {get {return Instance?.timeM;}}

    #endregion



    public static Manager Instance
    {
        get
        {
            if(init == false)
            {
                init = true;

                GameObject obj = GameObject.Find("@Managers");

                if(obj == null)
                {
                    obj = new GameObject() {name = "@Managers"};
                    obj.AddComponent<Manager>();
                }
                DontDestroyOnLoad(obj);
                instance = obj.GetComponent<Manager>();
                ApplyHighQualityRendering();
                ApplyHighResolution();
                instance.updateM = obj.AddComponent<UpdateManager>();
                instance.soundM.Init();
                instance.timeM = obj.AddComponent<TimeManager>();

            }

            return instance;
        }
    }

    private string GetDebuggerDisplay()
    {
        return ToString();
    }

    static void ApplyHighQualityRendering()
    {
        int highestQuality = QualitySettings.names.Length - 1;
        if (highestQuality >= 0)
            QualitySettings.SetQualityLevel(highestQuality, true);

        QualitySettings.masterTextureLimit = 0;
        QualitySettings.antiAliasing = 4;
        QualitySettings.anisotropicFiltering = AnisotropicFiltering.Enable;
        QualitySettings.lodBias = 2f;
    }

    static void ApplyHighResolution()
    {
#if UNITY_EDITOR || UNITY_STANDALONE
        Screen.SetResolution(3840, 2160, false);
#endif
    }

    public static void Clear()
    {
        SoundM.Clear();
        PoolM.Clear();
        ObjectM.Clear();
        SceneM.Clear();
        UiM.Clear();
        
    }
}
