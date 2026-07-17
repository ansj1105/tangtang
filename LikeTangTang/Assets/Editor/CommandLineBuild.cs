using System;
using System.IO;
using System.Linq;
using UnityEditor;
using UnityEditor.Build.Reporting;
using UnityEditor.Android;
using UnityEngine;

public static class CommandLineBuild
{
    public static void BuildAndroidApk()
    {
        var outputPath = GetArg("-buildOutput");
        if (string.IsNullOrWhiteSpace(outputPath))
        {
            outputPath = "Builds/Android/LikeTangTang.apk";
        }

        ConfigureAndroidTools();
        Directory.CreateDirectory(Path.GetDirectoryName(outputPath));

        EditorUserBuildSettings.SwitchActiveBuildTarget(BuildTargetGroup.Android, BuildTarget.Android);
        EditorUserBuildSettings.buildAppBundle = false;
        EditorUserBuildSettings.exportAsGoogleAndroidProject = false;
        PlayerSettings.Android.useCustomKeystore = false;
        PlayerSettings.Android.targetArchitectures = GetAndroidArchitectures();

        var scenes = EditorBuildSettings.scenes
            .Where(scene => scene.enabled)
            .Select(scene => scene.path)
            .ToArray();

        var report = BuildPipeline.BuildPlayer(new BuildPlayerOptions
        {
            scenes = scenes,
            locationPathName = outputPath,
            target = BuildTarget.Android,
            options = BuildOptions.None
        });

        if (report.summary.result != BuildResult.Succeeded)
        {
            throw new Exception($"Android build failed: {report.summary.result}");
        }

        Debug.Log($"Android APK built: {Path.GetFullPath(outputPath)}");
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
}
