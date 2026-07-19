using System.IO;
using UnityEditor;
using UnityEngine;

[InitializeOnLoad]
public static class NHBasicAlienControllerBinder
{
    private const string PrefabPath = "Assets/Art/Enemies/1-1/Prefabs/NH_BasicAlien.prefab";
    private const string MarkerPath = "ProjectSettings/NH_BasicAlien.controller_bound";

    static NHBasicAlienControllerBinder()
    {
        EditorApplication.delayCall += BindIfNeeded;
    }

    [MenuItem("NYAON_HUNTER/Bind NH Basic Alien Controller")]
    public static void Bind()
    {
        if (!File.Exists(PrefabPath))
            return;

        GameObject root = PrefabUtility.LoadPrefabContents(PrefabPath);

        NHBasicAlienMonsterController alienController = root.GetComponent<NHBasicAlienMonsterController>();
        if (alienController == null)
            alienController = root.AddComponent<NHBasicAlienMonsterController>();

        MonsterController baseController = root.GetComponent<MonsterController>();
        if (baseController != null && baseController.GetType() == typeof(MonsterController))
            Object.DestroyImmediate(baseController, true);

        PrefabUtility.SaveAsPrefabAsset(root, PrefabPath);
        PrefabUtility.UnloadPrefabContents(root);
        File.WriteAllText(MarkerPath, "NH_BasicAlien prefab uses NHBasicAlienMonsterController.");
        AssetDatabase.SaveAssets();
        AssetDatabase.Refresh();
        Debug.Log("Bound NH_BasicAlien prefab to NHBasicAlienMonsterController.");
    }

    private static void BindIfNeeded()
    {
        if (File.Exists(MarkerPath))
            return;

        Bind();
    }
}
