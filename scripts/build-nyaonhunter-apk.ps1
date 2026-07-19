param(
    [switch]$DevelopmentBuild,
    [switch]$KeepGradleDaemon
)

$ErrorActionPreference = "Stop"

$scriptDir = Split-Path -Parent $MyInvocation.MyCommand.Path
$buildScript = Join-Path $scriptDir "build-android-apk.ps1"
$apkPath = "C:\work\likeTangTang-build-src\LikeTangTang\Builds\Android\NyaonHunter.apk"
$copyPath = "C:\work\NyaonHunter.apk"

if (-not (Test-Path -LiteralPath $buildScript)) {
    throw "Build script not found: $buildScript"
}

Remove-Item $apkPath -Force -ErrorAction SilentlyContinue
Remove-Item $copyPath -Force -ErrorAction SilentlyContinue

$buildParams = @{
    OutputPath = $apkPath
}

if ($DevelopmentBuild) {
    $buildParams.DevelopmentBuild = $true
}

if (-not $KeepGradleDaemon) {
    $buildParams.StopGradleDaemon = $true
}

& $buildScript @buildParams

if ($LASTEXITCODE -ne 0) {
    exit $LASTEXITCODE
}

if (-not (Test-Path -LiteralPath $apkPath)) {
    throw "APK was not created: $apkPath"
}

Copy-Item $apkPath $copyPath -Force

$apk = Get-Item -LiteralPath $apkPath
$copy = Get-Item -LiteralPath $copyPath

Write-Host "NyaonHunter APK built."
Write-Host "APK:  $($apk.FullName) ($($apk.Length) bytes)"
Write-Host "Copy: $($copy.FullName) ($($copy.Length) bytes)"
