using System;
using System.IO;
using System.Linq;
using UnityEditor;
using UnityEditor.AddressableAssets;
using UnityEditor.AddressableAssets.Settings;
using UnityEditor.Animations;
using UnityEngine;

[InitializeOnLoad]
public static class NYAONEnemyBulkGenerator
{
    private const string BasePath = "Assets/Art/Enemies";
    private const string PlayerReferencePath = "Assets/@Resources/Sprites/Player/1. Alpha/Idle/Alpha.png";
    private const string MarkerPath = "ProjectSettings/NYAON_Enemy_Bulk.generated";

    private static readonly (string Slot, string EnemyName)[] Enemies =
    {
        ("1-1", "NH_BasicAlien"),
        ("1-2", "NH_KongKongi"),
        ("1-3", "NH_KkuBeogi"),
        ("2-1", "NH_MeongAli"),
        ("2-2", "NH_SaengseonPpyeoByeong"),
        ("2-3", "NH_PokeByeong")
    };

    static NYAONEnemyBulkGenerator()
    {
        EditorApplication.delayCall += GenerateIfNeeded;
    }

    [MenuItem("NYAON_HUNTER/Generate Bulk Enemy Assets")]
    public static void Generate()
    {
        AssetDatabase.Refresh();

        float targetWorldHeight = GetTargetWorldHeight();
        foreach ((string slot, string enemyName) in Enemies)
        {
            string enemyPath = $"{BasePath}/{slot}";
            if (!Directory.Exists(enemyPath))
                continue;

            Directory.CreateDirectory($"{enemyPath}/Animations");
            Directory.CreateDirectory($"{enemyPath}/Animator");
            Directory.CreateDirectory($"{enemyPath}/Prefabs");

            ImportAndNormalizeSprites(enemyPath, targetWorldHeight);

            AnimationClip move = CreateSpriteClip(enemyPath, enemyName, "Move", 10f, true);
            AnimationClip death = CreateSpriteClip(enemyPath, enemyName, "Death", 10f, false);

            AnimatorController controller = CreateAnimatorController(enemyPath, enemyName, move, death);
            CreatePrefab(enemyPath, enemyName, controller);

            RegisterAddressable($"{enemyPath}/Prefabs/{enemyName}.prefab", "Prefabs", enemyName);
            RegisterAddressable($"{enemyPath}/Move/{GetFirstPngFileName(enemyPath, "Move")}", "Sprites", $"{enemyName}_Move_01.sprite");
            RegisterAddressable($"{enemyPath}/Animator/{enemyName}.controller", "Anim", enemyName);
        }

        File.WriteAllText(MarkerPath, "Generated NYAON enemy bulk assets.");
        AssetDatabase.SaveAssets();
        AssetDatabase.Refresh();
        Debug.Log("Generated NYAON bulk enemy Move/Death clips, controllers, prefabs, and Addressables.");
    }

    private static void GenerateIfNeeded()
    {
        if (File.Exists(MarkerPath))
            return;

        if (Enemies.Any(enemy => Directory.Exists($"{BasePath}/{enemy.Slot}")))
            Generate();
    }

    private static void ImportAndNormalizeSprites(string enemyPath, float targetWorldHeight)
    {
        float referencePpu = GetReferencePixelsPerUnit(enemyPath, targetWorldHeight);

        foreach (string path in Directory.GetFiles(enemyPath, "*.png", SearchOption.AllDirectories))
        {
            string assetPath = path.Replace('\\', '/');
            bool isShadow = assetPath.Contains("/Shadow/");

            TextureImporter importer = AssetImporter.GetAtPath(assetPath) as TextureImporter;
            if (importer == null)
                continue;

            importer.textureType = TextureImporterType.Sprite;
            importer.spriteImportMode = SpriteImportMode.Single;
            importer.alphaIsTransparency = true;
            importer.mipmapEnabled = false;
            importer.textureCompression = TextureImporterCompression.Uncompressed;
            importer.maxTextureSize = 4096;
            importer.filterMode = FilterMode.Bilinear;
            importer.spritePivot = new Vector2(0.5f, 0.5f);

            if (!isShadow && referencePpu > 0f)
                importer.spritePixelsPerUnit = referencePpu;

            importer.SaveAndReimport();
        }
    }

    private static AnimationClip CreateSpriteClip(string enemyPath, string enemyName, string folderName, float fps, bool loop)
    {
        string clipPath = $"{enemyPath}/Animations/{enemyName}_{folderName}.anim";
        AnimationClip clip = AssetDatabase.LoadAssetAtPath<AnimationClip>(clipPath);
        if (clip == null)
        {
            clip = new AnimationClip();
            AssetDatabase.CreateAsset(clip, clipPath);
        }

        clip.frameRate = fps;

        Sprite[] sprites = Directory.GetFiles($"{enemyPath}/{folderName}", "*.png", SearchOption.TopDirectoryOnly)
            .OrderBy(path => path, StringComparer.OrdinalIgnoreCase)
            .Select(path => AssetDatabase.LoadAssetAtPath<Sprite>(path.Replace('\\', '/')))
            .Where(sprite => sprite != null)
            .ToArray();

        ObjectReferenceKeyframe[] keyframes = new ObjectReferenceKeyframe[sprites.Length];
        for (int i = 0; i < sprites.Length; i++)
        {
            keyframes[i] = new ObjectReferenceKeyframe
            {
                time = i / fps,
                value = sprites[i]
            };
        }

        AnimationUtility.SetObjectReferenceCurve(clip, new EditorCurveBinding
        {
            type = typeof(SpriteRenderer),
            path = string.Empty,
            propertyName = "m_Sprite"
        }, keyframes);

        AnimationClipSettings settings = AnimationUtility.GetAnimationClipSettings(clip);
        settings.loopTime = loop;
        AnimationUtility.SetAnimationClipSettings(clip, settings);
        EditorUtility.SetDirty(clip);
        return clip;
    }

    private static AnimatorController CreateAnimatorController(
        string enemyPath,
        string enemyName,
        AnimationClip move,
        AnimationClip death)
    {
        string controllerPath = $"{enemyPath}/Animator/{enemyName}.controller";
        AnimatorController existing = AssetDatabase.LoadAssetAtPath<AnimatorController>(controllerPath);
        if (existing != null)
            AssetDatabase.DeleteAsset(controllerPath);

        AnimatorController controller = AnimatorController.CreateAnimatorControllerAtPath(controllerPath);
        controller.AddParameter("Dead", AnimatorControllerParameterType.Bool);

        AnimatorStateMachine sm = controller.layers[0].stateMachine;
        sm.states = new ChildAnimatorState[0];
        sm.anyStateTransitions = new AnimatorStateTransition[0];
        sm.entryTransitions = new AnimatorTransition[0];

        AnimatorState moveState = sm.AddState("Move");
        AnimatorState deathState = sm.AddState("Death");

        moveState.motion = move;
        deathState.motion = death;
        sm.defaultState = moveState;

        AnimatorStateTransition deathTransition = sm.AddAnyStateTransition(deathState);
        deathTransition.hasExitTime = false;
        deathTransition.duration = 0f;
        deathTransition.canTransitionToSelf = false;
        deathTransition.AddCondition(AnimatorConditionMode.If, 0f, "Dead");

        EditorUtility.SetDirty(controller);
        return controller;
    }

    private static void CreatePrefab(string enemyPath, string enemyName, RuntimeAnimatorController controller)
    {
        GameObject root = new GameObject(enemyName);
        int monsterLayer = LayerMask.NameToLayer("Monster");
        if (monsterLayer >= 0)
            root.layer = monsterLayer;

        SpriteRenderer renderer = root.AddComponent<SpriteRenderer>();
        renderer.sprite = LoadFirstSprite(enemyPath, "Move");
        renderer.sortingOrder = 0;

        Animator animator = root.AddComponent<Animator>();
        animator.runtimeAnimatorController = controller;

        Rigidbody2D rigidbody = root.AddComponent<Rigidbody2D>();
        rigidbody.gravityScale = 0f;
        rigidbody.constraints = RigidbodyConstraints2D.FreezeRotation;

        CircleCollider2D collider = root.AddComponent<CircleCollider2D>();
        collider.isTrigger = false;
        collider.radius = 0.67f;

        root.AddComponent<NHBasicAlienMonsterController>();

        Sprite shadowSprite = LoadFirstSprite(enemyPath, "Shadow");
        if (shadowSprite != null)
        {
            GameObject shadow = new GameObject("Shadow");
            if (monsterLayer >= 0)
                shadow.layer = monsterLayer;

            shadow.transform.SetParent(root.transform, false);
            shadow.transform.localScale = new Vector3(0.35f, 0.35f, 1f);
            SpriteRenderer shadowRenderer = shadow.AddComponent<SpriteRenderer>();
            shadowRenderer.sprite = shadowSprite;
            shadowRenderer.sortingOrder = -1;
        }

        PrefabUtility.SaveAsPrefabAsset(root, $"{enemyPath}/Prefabs/{enemyName}.prefab");
        UnityEngine.Object.DestroyImmediate(root);
    }

    private static Sprite LoadFirstSprite(string enemyPath, string folderName)
    {
        string path = Directory.GetFiles($"{enemyPath}/{folderName}", "*.png", SearchOption.TopDirectoryOnly)
            .OrderBy(file => file, StringComparer.OrdinalIgnoreCase)
            .FirstOrDefault();

        return string.IsNullOrEmpty(path) ? null : AssetDatabase.LoadAssetAtPath<Sprite>(path.Replace('\\', '/'));
    }

    private static string GetFirstPngFileName(string enemyPath, string folderName)
    {
        string path = Directory.GetFiles($"{enemyPath}/{folderName}", "*.png", SearchOption.TopDirectoryOnly)
            .OrderBy(file => file, StringComparer.OrdinalIgnoreCase)
            .FirstOrDefault();
        return string.IsNullOrEmpty(path) ? string.Empty : Path.GetFileName(path);
    }

    private static float GetTargetWorldHeight()
    {
        TextureImporter playerImporter = AssetImporter.GetAtPath(PlayerReferencePath) as TextureImporter;
        float ppu = playerImporter != null ? playerImporter.spritePixelsPerUnit : 100f;
        int visibleHeight = GetVisiblePixelHeight(PlayerReferencePath);
        return visibleHeight > 0 ? visibleHeight / ppu : 1.73f;
    }

    private static float GetReferencePixelsPerUnit(string enemyPath, float targetWorldHeight)
    {
        string movePath = $"{enemyPath}/Move";
        if (!Directory.Exists(movePath) || targetWorldHeight <= 0f)
            return 0f;

        int maxVisibleHeight = Directory.GetFiles(movePath, "*.png", SearchOption.TopDirectoryOnly)
            .Select(GetVisiblePixelHeight)
            .DefaultIfEmpty(0)
            .Max();

        return maxVisibleHeight > 0 ? maxVisibleHeight / targetWorldHeight : 0f;
    }

    private static int GetVisiblePixelHeight(string assetPath)
    {
        if (!File.Exists(assetPath))
            return 0;

        byte[] bytes = File.ReadAllBytes(assetPath);
        Texture2D texture = new Texture2D(2, 2, TextureFormat.RGBA32, false);
        if (!texture.LoadImage(bytes))
        {
            UnityEngine.Object.DestroyImmediate(texture);
            return 0;
        }

        Color32[] pixels = texture.GetPixels32();
        int minY = texture.height;
        int maxY = -1;

        for (int y = 0; y < texture.height; y++)
        {
            for (int x = 0; x < texture.width; x++)
            {
                if (pixels[y * texture.width + x].a <= 10)
                    continue;

                if (y < minY) minY = y;
                if (y > maxY) maxY = y;
            }
        }

        UnityEngine.Object.DestroyImmediate(texture);
        return maxY < 0 ? 0 : maxY - minY + 1;
    }

    private static void RegisterAddressable(string assetPath, string groupName, string address)
    {
        if (string.IsNullOrEmpty(assetPath) || !File.Exists(assetPath))
            return;

        AddressableAssetSettings settings = AddressableAssetSettingsDefaultObject.Settings;
        if (settings == null)
            return;

        AddressableAssetGroup group = settings.FindGroup(groupName) ?? settings.DefaultGroup;
        string guid = AssetDatabase.AssetPathToGUID(assetPath);
        if (string.IsNullOrEmpty(guid))
            return;

        AddressableAssetEntry entry = settings.CreateOrMoveEntry(guid, group, false, false);
        entry.address = address;
        settings.SetDirty(AddressableAssetSettings.ModificationEvent.EntryMoved, entry, true);
    }
}
