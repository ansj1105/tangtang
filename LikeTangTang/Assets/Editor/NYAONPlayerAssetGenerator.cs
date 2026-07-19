using System;
using System.IO;
using System.Linq;
using UnityEditor;
using UnityEngine;

[InitializeOnLoad]
public static class NYAONPlayerAssetGenerator
{
    private const string RequestPath = "ProjectSettings/NYAON_Player_Regenerate.request";
    private const string AlphaSpritePath = "Assets/@Resources/Sprites/Player/1. Alpha";
    private const string AlphaAnimPath = "Assets/@Resources/Anim/Player/1. Alpha";
    private const float TargetVisibleLocalHeight = 1.72f;

    static NYAONPlayerAssetGenerator()
    {
        EditorApplication.delayCall += RunIfRequested;
    }

    [MenuItem("NYAON_HUNTER/Regenerate Player Alpha Assets")]
    public static void Generate()
    {
        AssetDatabase.Refresh();

        ImportState("Idle", 1, 1);
        ImportState("Move", 1, 14);
        ImportState("Dead", 1, 14);
        ImportSingle($"{AlphaSpritePath}/Idle/Alpha.png");

        CreateSpriteClip(
            $"{AlphaAnimPath}/Player_Alpha_Idle.anim",
            GetSprites("Idle", 1, 1),
            12f,
            true);

        CreateSpriteClip(
            $"{AlphaAnimPath}/Player_Alpha_Move.anim",
            GetSprites("Move", 1, 14),
            12f,
            true);

        AnimationClip deadClip = CreateSpriteClip(
            $"{AlphaAnimPath}/Player_Alpha_Dead.anim",
            GetSprites("Dead", 1, 14),
            12f,
            false);

        CreateController(deadClip);

        AssetDatabase.SaveAssets();
        AssetDatabase.Refresh();
        Debug.Log("Regenerated NYAON player Alpha sprites and animation clips.");
    }

    private static void RunIfRequested()
    {
        if (!File.Exists(RequestPath))
            return;

        File.Delete(RequestPath);
        Generate();
    }

    private static void ImportState(string state, int first, int last)
    {
        for (int i = first; i <= last; i++)
            ImportSingle($"{AlphaSpritePath}/{state}/{i:D2}.png");
    }

    private static void ImportSingle(string assetPath)
    {
        TextureImporter importer = AssetImporter.GetAtPath(assetPath) as TextureImporter;
        if (importer == null)
            return;

        importer.textureType = TextureImporterType.Sprite;
        importer.spriteImportMode = SpriteImportMode.Single;
        importer.alphaIsTransparency = true;
        importer.mipmapEnabled = false;
        importer.textureCompression = TextureImporterCompression.Uncompressed;
        importer.maxTextureSize = 4096;
        importer.filterMode = FilterMode.Bilinear;
        importer.spritePivot = new Vector2(0.5f, 0.5f);

        float referencePpu = GetReferencePixelsPerUnit();
        if (referencePpu > 0f)
            importer.spritePixelsPerUnit = referencePpu;

        importer.SaveAndReimport();
    }

    private static Sprite[] GetSprites(string state, int first, int last)
    {
        return Enumerable.Range(first, last - first + 1)
            .Select(index => AssetDatabase.LoadAssetAtPath<Sprite>($"{AlphaSpritePath}/{state}/{index:D2}.png"))
            .Where(sprite => sprite != null)
            .ToArray();
    }

    private static AnimationClip CreateSpriteClip(string clipPath, Sprite[] sprites, float fps, bool loop)
    {
        AnimationClip clip = AssetDatabase.LoadAssetAtPath<AnimationClip>(clipPath);
        if (clip == null)
        {
            clip = new AnimationClip();
            AssetDatabase.CreateAsset(clip, clipPath);
        }

        clip.frameRate = fps;
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

    private static void CreateController(AnimationClip deadClip)
    {
        string controllerPath = $"{AlphaAnimPath}/Player_Alpha.controller";
        UnityEditor.Animations.AnimatorController controller =
            AssetDatabase.LoadAssetAtPath<UnityEditor.Animations.AnimatorController>(controllerPath);
        if (controller == null || deadClip == null)
            return;

        UnityEditor.Animations.AnimatorStateMachine stateMachine = controller.layers[0].stateMachine;
        UnityEditor.Animations.AnimatorState deadState = stateMachine.states
            .Select(child => child.state)
            .FirstOrDefault(state => state.name == "Dead");

        if (deadState == null)
        {
            deadState = stateMachine.AddState("Dead", new Vector3(320f, 260f, 0f));
        }

        deadState.motion = deadClip;
        EditorUtility.SetDirty(controller);
    }

    private static float GetReferencePixelsPerUnit()
    {
        string movePath = $"{AlphaSpritePath}/Move";
        if (!Directory.Exists(movePath))
            return 0f;

        int maxVisibleHeight = Directory.GetFiles(movePath, "*.png", SearchOption.TopDirectoryOnly)
            .Where(path => Path.GetFileNameWithoutExtension(path).All(char.IsDigit))
            .Select(path => int.TryParse(Path.GetFileNameWithoutExtension(path), out int index) && index <= 14 ? GetVisiblePixelHeight(path) : 0)
            .DefaultIfEmpty(0)
            .Max();

        return maxVisibleHeight > 0 ? maxVisibleHeight / TargetVisibleLocalHeight : 0f;
    }

    private static int GetVisiblePixelHeight(string assetPath)
    {
        string fullPath = Path.GetFullPath(assetPath);
        byte[] bytes = File.ReadAllBytes(fullPath);
        Texture2D texture = new Texture2D(2, 2, TextureFormat.RGBA32, false);
        if (!texture.LoadImage(bytes))
        {
            UnityEngine.Object.DestroyImmediate(texture);
            return 0;
        }

        Color32[] pixels = texture.GetPixels32();
        int width = texture.width;
        int minY = texture.height;
        int maxY = -1;
        for (int i = 0; i < pixels.Length; i++)
        {
            if (pixels[i].a <= 10)
                continue;

            int y = i / width;
            if (y < minY)
                minY = y;
            if (y > maxY)
                maxY = y;
        }

        UnityEngine.Object.DestroyImmediate(texture);
        return maxY < 0 ? 0 : maxY - minY + 1;
    }
}
