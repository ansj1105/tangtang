param(
    [string]$SourcePath = "C:\Users\msi\Downloads\Telegram Desktop\NYAON_HUNTERS-00\NYAON_HUNTERS\NYAON_HUNTERS",
    [string]$WslRepoRoot = "\\wsl.localhost\Ubuntu\home\ubuntu\work\likeTangTang",
    [string]$WindowsBuildRoot = "C:\work\likeTangTang-build-src",
    [switch]$SkipWindowsBuildCopy,
    [switch]$DryRun,
    [switch]$Detailed
)

$ErrorActionPreference = "Stop"

$excludedDirs = @(
    ".git",
    "Library",
    "Temp",
    "Logs",
    "Builds",
    "UserSettings",
    "ServerData",
    "obj"
)

$excludedFiles = @(
    "*.keystore"
)

function Invoke-RobocopyMirror {
    param(
        [Parameter(Mandatory = $true)][string]$Source,
        [Parameter(Mandatory = $true)][string]$Destination,
        [Parameter(Mandatory = $true)][string]$Label
    )

    if (-not (Test-Path -LiteralPath $Source)) {
        throw "$Label source not found: $Source"
    }

    if (-not (Test-Path -LiteralPath $Destination)) {
        New-Item -ItemType Directory -Path $Destination | Out-Null
    }

    $args = @(
        $Source,
        $Destination,
        "/MIR",
        "/FFT",
        "/R:1",
        "/W:1",
        "/NP",
        "/XD"
    ) + $excludedDirs + @("/XF") + $excludedFiles

    if ($DryRun) {
        $args += "/L"
    }

    if (-not $Detailed) {
        $args += @("/NFL", "/NDL", "/NJH", "/NJS")
    }

    Write-Host "== $Label =="
    Write-Host "From: $Source"
    Write-Host "To:   $Destination"

    if ($Detailed) {
        & robocopy @args
    } else {
        $robocopyOutput = & robocopy @args 2>&1
    }

    $exitCode = $LASTEXITCODE

    if ($exitCode -gt 7) {
        if (-not $Detailed -and $robocopyOutput) {
            $robocopyOutput | Select-Object -Last 40
        }

        throw "Robocopy failed for $Label with exit code $exitCode"
    }

    Write-Host "$Label completed with robocopy exit code $exitCode"
}

$wslProjectPath = Join-Path $WslRepoRoot "LikeTangTang"

Invoke-RobocopyMirror `
    -Source $SourcePath `
    -Destination $wslProjectPath `
    -Label "Download source -> WSL repo project"

if (-not $SkipWindowsBuildCopy) {
    Invoke-RobocopyMirror `
        -Source $WslRepoRoot `
        -Destination $WindowsBuildRoot `
        -Label "WSL repo -> Windows Unity build copy"
}

Write-Host "NyaonHunter source resync finished."
