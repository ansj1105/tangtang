using System;
using System.Reflection;
using UnityEditor;
using UnityEngine;

[InitializeOnLoad]
public static class GameViewResolutionSetter
{
    private const int TargetWidth = 3840;
    private const int TargetHeight = 2160;
    private const string TargetLabel = "4K UHD (3840x2160)";

    static GameViewResolutionSetter()
    {
        EditorApplication.delayCall += Apply;
    }

    [MenuItem("NYAON_HUNTER/Apply 4K Game View")]
    public static void Apply()
    {
        try
        {
            int index = EnsureGameViewSize(TargetWidth, TargetHeight, TargetLabel);
            SelectGameViewSize(index);
        }
        catch (Exception ex)
        {
            Debug.LogWarning($"Failed to apply 4K Game View size: {ex.Message}");
        }
    }

    private static int EnsureGameViewSize(int width, int height, string label)
    {
        Assembly editorAssembly = typeof(Editor).Assembly;
        Type gameViewSizesType = editorAssembly.GetType("UnityEditor.GameViewSizes");
        Type scriptableSingletonType = typeof(ScriptableSingleton<>).MakeGenericType(gameViewSizesType);
        PropertyInfo instanceProperty = scriptableSingletonType.GetProperty("instance", BindingFlags.Public | BindingFlags.Static);
        object gameViewSizes = instanceProperty.GetValue(null, null);

        MethodInfo getGroup = gameViewSizesType.GetMethod("GetGroup");
        object group = getGroup.Invoke(gameViewSizes, new object[] { (int)BuildTargetGroup.Standalone });
        Type groupType = group.GetType();

        MethodInfo getTotalCount = groupType.GetMethod("GetTotalCount");
        MethodInfo getGameViewSize = groupType.GetMethod("GetGameViewSize");
        int count = (int)getTotalCount.Invoke(group, null);

        for (int i = 0; i < count; i++)
        {
            object size = getGameViewSize.Invoke(group, new object[] { i });
            int sizeWidth = (int)size.GetType().GetProperty("width").GetValue(size, null);
            int sizeHeight = (int)size.GetType().GetProperty("height").GetValue(size, null);
            if (sizeWidth == width && sizeHeight == height)
                return i;
        }

        Type gameViewSizeType = editorAssembly.GetType("UnityEditor.GameViewSize");
        Type gameViewSizeTypeEnum = editorAssembly.GetType("UnityEditor.GameViewSizeType");
        object fixedResolution = Enum.Parse(gameViewSizeTypeEnum, "FixedResolution");
        ConstructorInfo constructor = gameViewSizeType.GetConstructor(new[] { gameViewSizeTypeEnum, typeof(int), typeof(int), typeof(string) });
        object newSize = constructor.Invoke(new object[] { fixedResolution, width, height, label });

        MethodInfo addCustomSize = groupType.GetMethod("AddCustomSize");
        addCustomSize.Invoke(group, new[] { newSize });
        return (int)getTotalCount.Invoke(group, null) - 1;
    }

    private static void SelectGameViewSize(int index)
    {
        Type gameViewType = typeof(Editor).Assembly.GetType("UnityEditor.GameView");
        EditorWindow gameView = EditorWindow.GetWindow(gameViewType);
        PropertyInfo selectedSizeIndex = gameViewType.GetProperty("selectedSizeIndex", BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic);
        selectedSizeIndex.SetValue(gameView, index, null);
        gameView.Repaint();
    }
}
