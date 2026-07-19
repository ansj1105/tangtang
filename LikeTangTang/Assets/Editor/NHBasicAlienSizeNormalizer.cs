using System.IO;
using System.Linq;
using UnityEditor;
using UnityEngine;

[InitializeOnLoad]
public static class NHBasicAlienSizeNormalizer
{
    private const string EnemyBasePath = "Assets/Art/Enemies/1-1";
    private const string PlayerReferencePath = "Assets/@Resources/Sprites/Player/1. Alpha/Idle/Alpha.png";
    private const string PrefabPath = EnemyBasePath + "/Prefabs/NH_BasicAlien.prefab";
    private const string MarkerPath = "ProjectSettings/NH_BasicAlien.size_normalized";

    static NHBasicAlienSizeNormalizer()
    {
        EditorApplication.delayCall += NormalizeIfNeeded;
    }

    [MenuItem("NYAON_HUNTERS/Normalize NH Basic Alien Size")]
    public static void Normalize()
    {
        AssetDatabase.Refresh();

        TextureImporter playerImporter = AssetImporter.GetAtPath(PlayerReferencePath) as TextureImporter;
        float playerPpu = playerImporter != null ? playerImporter.spritePixelsPerUnit : 100f;
        int playerVisibleHeight = GetVisiblePixelHeight(PlayerReferencePath);
        float targetWorldHeight = playerVisibleHeight / playerPpu;
        float referencePpu = GetReferencePixelsPerUnit(targetWorldHeight);

        foreach (string path in Directory.GetFiles(EnemyBasePath, "*.png", SearchOption.AllDirectories))
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

        NormalizePrefab();
        File.WriteAllText(MarkerPath, "NH_BasicAlien frame sizes normalized to the player visible height.");
        AssetDatabase.SaveAssets();
        AssetDatabase.Refresh();
        Debug.Log("Normalized NH_BasicAlien visual size to match the player.");
    }

    private static float GetReferencePixelsPerUnit(float targetWorldHeight)
    {
        string movePath = EnemyBasePath + "/Move";
        if (!Directory.Exists(movePath) || targetWorldHeight <= 0f)
            return 0f;

        int maxVisibleHeight = Directory.GetFiles(movePath, "*.png", SearchOption.TopDirectoryOnly)
            .Select(GetVisiblePixelHeight)
            .DefaultIfEmpty(0)
            .Max();

        return maxVisibleHeight > 0 ? maxVisibleHeight / targetWorldHeight : 0f;
    }

    private static void NormalizeIfNeeded()
    {
        if (File.Exists(MarkerPath) || !Directory.Exists(EnemyBasePath))
            return;

        Normalize();
    }

    private static int GetVisiblePixelHeight(string assetPath)
    {
        byte[] bytes = File.ReadAllBytes(assetPath);
        Texture2D texture = new Texture2D(2, 2, TextureFormat.RGBA32, false);
        if (!texture.LoadImage(bytes))
        {
            Object.DestroyImmediate(texture);
            return 0;
        }

        Color32[] pixels = texture.GetPixels32();
        int width = texture.width;
        int height = texture.height;
        int minY = height;
        int maxY = -1;

        for (int y = 0; y < height; y++)
        {
            for (int x = 0; x < width; x++)
            {
                if (pixels[y * width + x].a <= 10)
                    continue;

                if (y < minY) minY = y;
                if (y > maxY) maxY = y;
            }
        }

        Object.DestroyImmediate(texture);
        return maxY < 0 ? 0 : maxY - minY + 1;
    }

    private static void NormalizePrefab()
    {
        if (!File.Exists(PrefabPath))
            return;

        GameObject root = PrefabUtility.LoadPrefabContents(PrefabPath);
        root.transform.localScale = Vector3.one;

        Transform shadow = root.transform.Find("Shadow");
        if (shadow != null)
            shadow.localScale = new Vector3(0.35f, 0.35f, 1f);

        PrefabUtility.SaveAsPrefabAsset(root, PrefabPath);
        PrefabUtility.UnloadPrefabContents(root);
    }
}
