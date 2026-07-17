param(
    [string]$UnityPath = "C:\work\Unity\Hub\Editor\2021.3.19f1\Editor\Unity.exe",
    [string]$ProjectPath = "C:\work\likeTangTang-build-src\LikeTangTang",
    [string]$OutputPath = "C:\work\likeTangTang-build-src\LikeTangTang\Builds\Android\LikeTangTang.apk",
    [string]$AndroidSdk = "C:\work\android-sdk",
    [string]$AndroidNdk = "C:\work\android-ndk-r21d",
    [string]$JavaHome = "C:\work\jdk8",
    [string]$Architectures = "ARM64",
    [switch]$StopGradleDaemon
)

if ($StopGradleDaemon) {
    Get-CimInstance Win32_Process |
        Where-Object {
            $_.Name -eq "java.exe" -and
            $_.CommandLine -like "*GradleDaemon*" -and
            $_.CommandLine -like "*gradle-launcher-6.1.1.jar*"
        } |
        ForEach-Object { Stop-Process -Id $_.ProcessId -Force }
}

$env:ANDROID_HOME = $AndroidSdk
$env:ANDROID_SDK_ROOT = $AndroidSdk
$env:ANDROID_NDK_ROOT = $AndroidNdk
$env:JAVA_HOME = $JavaHome
$env:UNITY_ANDROID_ARCHITECTURES = $Architectures
$env:Path = "$JavaHome\bin;$AndroidSdk\tools\bin;$AndroidSdk\platform-tools;$env:Path"

& $UnityPath `
    -batchmode `
    -nographics `
    -quit `
    -projectPath $ProjectPath `
    -executeMethod CommandLineBuild.BuildAndroidApk `
    -buildOutput $OutputPath `
    -logFile "C:\work\likeTangTang-unity-build.log"

exit $LASTEXITCODE
