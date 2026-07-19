using System;
using System.IO;
using System.Linq;
using UnityEditor;
using UnityEditor.AddressableAssets;
using UnityEditor.AddressableAssets.Settings;
using UnityEditor.Build.Reporting;
using UnityEditor.Android;
using UnityEngine;

public static class CommandLineBuild
{
    private static readonly RequiredAddressable[] RequiredAddressables =
    {
        new RequiredAddressable("Assets/@Resources/Sprites/Env/Potion/Normal_Potion/Normal_Potion.png", "Sprites", "Normal_Potion.sprite", "AlwaysKeep"),
        new RequiredAddressable("Assets/@Resources/Sprites/Env/Potion/Good_Potion/Good_Potion.png", "Sprites", "Good_Potion.sprite", "AlwaysKeep"),
        new RequiredAddressable("Assets/@Resources/Sprites/Env/Potion/Best_Potion/Best_Potion.png", "Sprites", "Best_Potion.sprite", "AlwaysKeep"),
        new RequiredAddressable("Assets/@Resources/Sprites/Jam/EXPJam_01.png", "Sprites", "Exp.sprite", "AlwaysKeep"),
        new RequiredAddressable("Assets/@Resources/Sprites/UI/Item/EqptBox_Icon.png", "Sprites", "EqptBox_Icon.sprite", "AlwaysKeep"),
    };

    public static void BuildAndroidApk()
    {
        var outputPath = GetArg("-buildOutput");
        if (string.IsNullOrWhiteSpace(outputPath))
        {
            outputPath = "Builds/Android/NYAON_HUNTERS.apk";
        }

        ConfigureAndroidTools();
        Directory.CreateDirectory(Path.GetDirectoryName(outputPath));

        EditorUserBuildSettings.SwitchActiveBuildTarget(BuildTargetGroup.Android, BuildTarget.Android);
        EditorUserBuildSettings.buildAppBundle = false;
        EditorUserBuildSettings.exportAsGoogleAndroidProject = false;
        PlayerSettings.Android.useCustomKeystore = false;
        PlayerSettings.Android.targetArchitectures = GetAndroidArchitectures();

        BuildAddressables();

        var scenes = EditorBuildSettings.scenes
            .Where(scene => scene.enabled)
            .Select(scene => scene.path)
            .ToArray();

        var report = BuildPipeline.BuildPlayer(new BuildPlayerOptions
        {
            scenes = scenes,
            locationPathName = outputPath,
            target = BuildTarget.Android,
            options = GetBuildOptions()
        });

        if (report.summary.result != BuildResult.Succeeded)
        {
            throw new Exception($"Android build failed: {report.summary.result}");
        }

        Debug.Log($"Android APK built: {Path.GetFullPath(outputPath)}");
    }

    private static BuildOptions GetBuildOptions()
    {
        bool development = HasArg("-developmentBuild")
            || string.Equals(Environment.GetEnvironmentVariable("UNITY_DEVELOPMENT_BUILD"), "1", StringComparison.OrdinalIgnoreCase);

        if (!development)
            return BuildOptions.None;

        EditorUserBuildSettings.development = true;
        EditorUserBuildSettings.allowDebugging = true;
        EditorUserBuildSettings.waitForManagedDebugger = false;
        return BuildOptions.Development | BuildOptions.AllowDebugging;
    }

    private static void BuildAddressables()
    {
        AddressableAssetSettings settings = AddressableAssetSettingsDefaultObject.Settings;
        if (settings == null)
        {
            throw new Exception("AddressableAssetSettings not found.");
        }

        EnsureRequiredAddressables(settings);

        var serverDataPath = Path.Combine(Directory.GetCurrentDirectory(), "ServerData", "Android");
        if (Directory.Exists(serverDataPath))
        {
            Directory.Delete(serverDataPath, true);
        }

        AddressableAssetSettings.CleanPlayerContent(settings.ActivePlayerDataBuilder);
        AddressableAssetSettings.BuildPlayerContent();
        Debug.Log("Addressables Android content rebuilt for player build.");
    }

    private static void EnsureRequiredAddressables(AddressableAssetSettings settings)
    {
        foreach (var required in RequiredAddressables)
        {
            RegisterAddressable(settings, required);
        }

        AssetDatabase.SaveAssets();
    }

    private static void RegisterAddressable(AddressableAssetSettings settings, RequiredAddressable required)
    {
        var guid = AssetDatabase.AssetPathToGUID(required.AssetPath);
        if (string.IsNullOrWhiteSpace(guid))
        {
            throw new Exception($"Required addressable asset not found: {required.AssetPath}");
        }

        var group = settings.FindGroup(required.GroupName);
        if (group == null)
        {
            throw new Exception($"Required addressable group not found: {required.GroupName}");
        }

        var entry = settings.CreateOrMoveEntry(guid, group, false, false);
        entry.address = required.Address;
        if (!string.IsNullOrWhiteSpace(required.Label))
        {
            settings.AddLabel(required.Label);
            entry.SetLabel(required.Label, true, true);
        }

        settings.SetDirty(AddressableAssetSettings.ModificationEvent.EntryMoved, entry, true);
    }

    private static AndroidArchitecture GetAndroidArchitectures()
    {
        var value = GetArg("-androidArchitectures")
            ?? Environment.GetEnvironmentVariable("UNITY_ANDROID_ARCHITECTURES")
            ?? "ARM64";

        AndroidArchitecture architectures = 0;
        foreach (var token in value.Split(new[] { ',', ';', '|' }, StringSplitOptions.RemoveEmptyEntries))
        {
            switch (token.Trim().ToUpperInvariant())
            {
                case "ARMV7":
                case "ARM7":
                    architectures |= AndroidArchitecture.ARMv7;
                    break;
                case "ARM64":
                case "ARM64V8A":
                case "ARM64-V8A":
                    architectures |= AndroidArchitecture.ARM64;
                    break;
                case "ALL":
                    architectures |= AndroidArchitecture.ARMv7 | AndroidArchitecture.ARM64;
                    break;
                default:
                    throw new Exception($"Unsupported Android architecture: {token}");
            }
        }

        return architectures == 0 ? AndroidArchitecture.ARM64 : architectures;
    }

    private static void ConfigureAndroidTools()
    {
#if UNITY_ANDROID
        var sdk = Environment.GetEnvironmentVariable("ANDROID_SDK_ROOT")
            ?? Environment.GetEnvironmentVariable("ANDROID_HOME");
        var ndk = Environment.GetEnvironmentVariable("ANDROID_NDK_ROOT");
        var jdk = Environment.GetEnvironmentVariable("JAVA_HOME");

        if (!string.IsNullOrWhiteSpace(sdk))
        {
            UnityEditor.Android.AndroidExternalToolsSettings.sdkRootPath = sdk;
        }
        if (!string.IsNullOrWhiteSpace(ndk))
        {
            UnityEditor.Android.AndroidExternalToolsSettings.ndkRootPath = ndk;
        }
        if (!string.IsNullOrWhiteSpace(jdk))
        {
            UnityEditor.Android.AndroidExternalToolsSettings.jdkRootPath = jdk;
        }
#endif
    }

    private static string GetArg(string name)
    {
        var args = Environment.GetCommandLineArgs();
        for (var i = 0; i < args.Length - 1; i++)
        {
            if (args[i] == name)
            {
                return args[i + 1];
            }
        }
        return null;
    }

    private static bool HasArg(string name)
    {
        return Environment.GetCommandLineArgs().Any(arg => arg == name);
    }

    private readonly struct RequiredAddressable
    {
        public RequiredAddressable(string assetPath, string groupName, string address, string label)
        {
            AssetPath = assetPath;
            GroupName = groupName;
            Address = address;
            Label = label;
        }

        public string AssetPath { get; }
        public string GroupName { get; }
        public string Address { get; }
        public string Label { get; }
    }
}
