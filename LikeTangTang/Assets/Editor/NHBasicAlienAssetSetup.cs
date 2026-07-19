using System.IO;
using System.Linq;
using UnityEditor;
using UnityEditor.AddressableAssets;
using UnityEditor.AddressableAssets.Settings;
using UnityEditor.Animations;
using UnityEngine;

public static class NHBasicAlienAssetSetup
{
    const string AlienRoot = "Assets/@Resources/Creatures/Monster/NH_BasicAlien";
    const string MonsterAnimPath = "Assets/@Resources/Creatures/Monster/Goblin/Monster.anim";
    const string MonsterControllerPath = "Assets/@Resources/Creatures/Monster/Goblin/Monster.controller";
    const string MonsterPrefabPath = "Assets/@Resources/Prefab/Creatures/Monster.prefab";
    const string SpritesGroupName = "Sprites";

    public static void Apply()
    {
        AssetDatabase.StartAssetEditing();
        try
        {
            ConfigureSpriteImports();
        }
        finally
        {
            AssetDatabase.StopAssetEditing();
        }

        AssetDatabase.Refresh();

        Sprite[] moveSprites = LoadSprites("Move");
        CreateMoveAnimation(moveSprites);
        EnsureMonsterController();
        UpdateMonsterPrefab(moveSprites.First());
        AddSpriteAddressables();

        AssetDatabase.SaveAssets();
        AssetDatabase.Refresh();

        Debug.Log("NHBasicAlienAssetSetup complete.");
    }

    static void ConfigureSpriteImports()
    {
        foreach (string path in Directory.GetFiles(AlienRoot, "*.png", SearchOption.AllDirectories))
        {
            string assetPath = path.Replace('\\', '/');
            TextureImporter importer = AssetImporter.GetAtPath(assetPath) as TextureImporter;
            if (importer == null)
                continue;

            importer.textureType = TextureImporterType.Sprite;
            importer.spriteImportMode = SpriteImportMode.Single;
            importer.alphaIsTransparency = true;
            importer.filterMode = FilterMode.Bilinear;
            importer.textureCompression = TextureImporterCompression.Uncompressed;
            importer.mipmapEnabled = false;
            importer.spritePixelsPerUnit = 200f;
            importer.SaveAndReimport();
        }
    }

    static Sprite[] LoadSprites(string folder)
    {
        string folderPath = $"{AlienRoot}/{folder}";
        return Directory.GetFiles(folderPath, "*.png")
            .OrderBy(path => path)
            .Select(path => AssetDatabase.LoadAssetAtPath<Sprite>(path.Replace('\\', '/')))
            .Where(sprite => sprite != null)
            .ToArray();
    }

    static void CreateMoveAnimation(Sprite[] sprites)
    {
        if (sprites.Length == 0)
            throw new FileNotFoundException("NH_BasicAlien Move sprites were not imported.");

        AnimationClip clip = AssetDatabase.LoadAssetAtPath<AnimationClip>(MonsterAnimPath);
        if (clip == null)
        {
            clip = new AnimationClip();
            AssetDatabase.CreateAsset(clip, MonsterAnimPath);
        }

        clip.ClearCurves();
        clip.frameRate = 10f;

        EditorCurveBinding binding = new EditorCurveBinding
        {
            path = "",
            type = typeof(SpriteRenderer),
            propertyName = "m_Sprite"
        };

        ObjectReferenceKeyframe[] frames = sprites
            .Select((sprite, index) => new ObjectReferenceKeyframe
            {
                time = index / clip.frameRate,
                value = sprite
            })
            .ToArray();

        AnimationUtility.SetObjectReferenceCurve(clip, binding, frames);
        AnimationUtility.SetAnimationClipSettings(clip, new AnimationClipSettings
        {
            loopTime = true
        });
        EditorUtility.SetDirty(clip);
    }

    static void EnsureMonsterController()
    {
        AnimatorController controller = AssetDatabase.LoadAssetAtPath<AnimatorController>(MonsterControllerPath);
        AnimationClip clip = AssetDatabase.LoadAssetAtPath<AnimationClip>(MonsterAnimPath);
        if (controller == null || clip == null)
            return;

        AnimatorStateMachine stateMachine = controller.layers[0].stateMachine;
        AnimatorState state = stateMachine.states.Select(child => child.state).FirstOrDefault();
        if (state == null)
            state = stateMachine.AddState("Monster");

        state.motion = clip;
        stateMachine.defaultState = state;
        EditorUtility.SetDirty(controller);
    }

    static void UpdateMonsterPrefab(Sprite previewSprite)
    {
        GameObject root = PrefabUtility.LoadPrefabContents(MonsterPrefabPath);
        try
        {
            SpriteRenderer renderer = root.GetComponentsInChildren<SpriteRenderer>(true).FirstOrDefault();
            if (renderer != null)
            {
                renderer.sprite = previewSprite;
                renderer.size = new Vector2(1.7f, 2.1f);
            }

            Animator animator = root.GetComponentsInChildren<Animator>(true).FirstOrDefault();
            AnimatorController controller = AssetDatabase.LoadAssetAtPath<AnimatorController>(MonsterControllerPath);
            if (animator != null && controller != null)
                animator.runtimeAnimatorController = controller;

            CircleCollider2D collider = root.GetComponent<CircleCollider2D>();
            if (collider != null)
                collider.radius = 0.7f;

            PrefabUtility.SaveAsPrefabAsset(root, MonsterPrefabPath);
        }
        finally
        {
            PrefabUtility.UnloadPrefabContents(root);
        }
    }

    static void AddSpriteAddressables()
    {
        AddressableAssetSettings settings = AddressableAssetSettingsDefaultObject.Settings;
        if (settings == null)
            return;

        AddressableAssetGroup group = settings.FindGroup(SpritesGroupName) ?? settings.DefaultGroup;
        foreach (string path in Directory.GetFiles(AlienRoot, "*.png", SearchOption.AllDirectories))
        {
            string assetPath = path.Replace('\\', '/');
            string guid = AssetDatabase.AssetPathToGUID(assetPath);
            if (string.IsNullOrEmpty(guid))
                continue;

            AddressableAssetEntry entry = settings.CreateOrMoveEntry(guid, group);
            entry.address = $"{Path.GetFileNameWithoutExtension(assetPath)}.sprite";
        }

        settings.SetDirty(AddressableAssetSettings.ModificationEvent.EntryMoved, group, true);
    }
}
