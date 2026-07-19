using System.IO;
using UnityEditor;

[InitializeOnLoad]
public static class NYAONEnemyRegenerateOnReload
{
    private const string RequestPath = "ProjectSettings/NYAON_Enemy_Regenerate.request";

    static NYAONEnemyRegenerateOnReload()
    {
        EditorApplication.delayCall += RunIfRequested;
    }

    private static void RunIfRequested()
    {
        if (!File.Exists(RequestPath))
            return;

        File.Delete(RequestPath);
        NYAONEnemyBulkGenerator.Generate();
    }

    public static void RunPendingRequest()
    {
        EditorApplication.delayCall += RunIfRequested;
    }
}

public sealed class NYAONEnemyRegeneratePostprocessor : AssetPostprocessor
{
    private static void OnPostprocessAllAssets(
        string[] importedAssets,
        string[] deletedAssets,
        string[] movedAssets,
        string[] movedFromAssetPaths)
    {
        NYAONEnemyRegenerateOnReload.RunPendingRequest();
    }
}
