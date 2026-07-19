using System.IO;
using UnityEditor;
using UnityEditor.AddressableAssets;
using UnityEditor.AddressableAssets.Settings;
using UnityEngine;

[InitializeOnLoad]
public static class NHBasicAlienStage1Setup
{
    private const string PrefabPath = "Assets/Art/Enemies/1-1/Prefabs/NH_BasicAlien.prefab";
    private const string MoveSpritePath = "Assets/Art/Enemies/1-1/Move/Move_01.png";
    private const string ControllerPath = "Assets/Art/Enemies/1-1/Animator/NH_BasicAlien.controller";
    private const string MarkerPath = "ProjectSettings/NH_BasicAlien.stage1";

    static NHBasicAlienStage1Setup()
    {
        EditorApplication.delayCall += SetupIfNeeded;
    }

    [MenuItem("NYAON_HUNTERS/Setup NH Basic Alien Stage 1")]
    public static void Setup()
    {
        AssetDatabase.Refresh();
        ConfigurePrefab();
        RegisterAddressable(PrefabPath, "Prefabs", "NH_BasicAlien");
        RegisterAddressable(MoveSpritePath, "Sprites", "NH_BasicAlien_Move_01.sprite");
        RegisterAddressable(ControllerPath, "Anim", "NH_BasicAlien");
        File.WriteAllText(MarkerPath, "NH_BasicAlien is wired to stage 1 monster ID 10000.");
        AssetDatabase.SaveAssets();
        AssetDatabase.Refresh();
        Debug.Log("Configured NH_BasicAlien as the stage 1-1 monster.");
    }

    private static void SetupIfNeeded()
    {
        if (File.Exists(MarkerPath) || !File.Exists(PrefabPath))
            return;

        Setup();
    }

    private static void ConfigurePrefab()
    {
        GameObject root = PrefabUtility.LoadPrefabContents(PrefabPath);
        int monsterLayer = LayerMask.NameToLayer("Monster");
        if (monsterLayer >= 0)
            SetLayerRecursively(root, monsterLayer);

        root.transform.localScale = Vector3.one;

        Rigidbody2D rigidbody = root.GetComponent<Rigidbody2D>();
        if (rigidbody == null)
            rigidbody = root.AddComponent<Rigidbody2D>();

        rigidbody.gravityScale = 0f;
        rigidbody.constraints = RigidbodyConstraints2D.FreezeRotation;

        CircleCollider2D collider = root.GetComponent<CircleCollider2D>();
        if (collider == null)
            collider = root.AddComponent<CircleCollider2D>();

        collider.isTrigger = false;
        collider.offset = Vector2.zero;
        collider.radius = 0.67f;

        Animator animator = root.GetComponent<Animator>();
        if (animator == null)
            animator = root.AddComponent<Animator>();

        RuntimeAnimatorController controller = AssetDatabase.LoadAssetAtPath<RuntimeAnimatorController>(ControllerPath);
        animator.runtimeAnimatorController = controller;

        SpriteRenderer renderer = root.GetComponent<SpriteRenderer>();
        if (renderer != null)
        {
            renderer.flipX = false;
            renderer.sortingOrder = 0;
        }

        PrefabUtility.SaveAsPrefabAsset(root, PrefabPath);
        PrefabUtility.UnloadPrefabContents(root);
    }

    private static void RegisterAddressable(string assetPath, string groupName, string address)
    {
        AddressableAssetSettings settings = AddressableAssetSettingsDefaultObject.Settings;
        if (settings == null)
            return;

        AddressableAssetGroup group = settings.FindGroup(groupName);
        if (group == null)
            group = settings.DefaultGroup;

        string guid = AssetDatabase.AssetPathToGUID(assetPath);
        if (string.IsNullOrEmpty(guid))
            return;

        AddressableAssetEntry entry = settings.CreateOrMoveEntry(guid, group, false, false);
        entry.address = address;
        settings.SetDirty(AddressableAssetSettings.ModificationEvent.EntryMoved, entry, true);
    }

    private static void SetLayerRecursively(GameObject target, int layer)
    {
        target.layer = layer;
        foreach (Transform child in target.transform)
            SetLayerRecursively(child.gameObject, layer);
    }
}
