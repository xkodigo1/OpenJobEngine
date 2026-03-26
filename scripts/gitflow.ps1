param(
    [Parameter(Mandatory = $true, Position = 0)]
    [string]$Command,

    [Parameter(Position = 1)]
    [string]$Name
)

$ErrorActionPreference = "Stop"

function Invoke-Git {
    param(
        [Parameter(ValueFromRemainingArguments = $true)]
        [string[]]$Arguments
    )

    & git @Arguments

    if ($LASTEXITCODE -ne 0) {
        throw "git $($Arguments -join ' ') failed."
    }
}

function Invoke-DotNet {
    param(
        [Parameter(ValueFromRemainingArguments = $true)]
        [string[]]$Arguments
    )

    & dotnet @Arguments

    if ($LASTEXITCODE -ne 0) {
        throw "dotnet $($Arguments -join ' ') failed."
    }
}

function Get-RepoRoot {
    return (Split-Path -Parent $PSScriptRoot)
}

function Require-GitFlow {
    & git flow version | Out-Null

    if ($LASTEXITCODE -ne 0) {
        throw "git-flow is required. Install git-flow-next before using this script."
    }
}

function Get-CurrentBranch {
    return (git rev-parse --abbrev-ref HEAD).Trim()
}

function Ensure-CleanWorkingTree {
    $status = git status --porcelain

    if (-not [string]::IsNullOrWhiteSpace($status)) {
        throw "Working tree is not clean. Commit or stash your changes before running '$Command'."
    }
}

function Ensure-DevelopExists {
    & git show-ref --verify --quiet refs/heads/develop

    if ($LASTEXITCODE -ne 0) {
        Invoke-Git branch develop main
    }

    $remoteDevelop = git ls-remote --heads origin develop

    if ([string]::IsNullOrWhiteSpace($remoteDevelop)) {
        Invoke-Git push -u origin develop
    }
}

function Initialize-GitFlow {
    Require-GitFlow
    Ensure-DevelopExists

    $initialized = git config --local --get gitflow.initialized

    if ($null -ne $initialized -and $initialized.Trim().ToLowerInvariant() -eq "true") {
        Write-Host "git-flow is already initialized in this repository."
        return
    }

    Invoke-Git flow init --preset=classic --main main --develop develop --tag v
}

function Start-Feature {
    param([string]$FeatureName)

    if ([string]::IsNullOrWhiteSpace($FeatureName)) {
        throw "Feature name is required."
    }

    Ensure-CleanWorkingTree
    Require-GitFlow
    Invoke-Git checkout develop
    Invoke-Git fetch origin
    Invoke-Git pull --ff-only origin develop
    Invoke-Git flow feature start $FeatureName
}

function Sync-Feature {
    Require-GitFlow

    $branch = Get-CurrentBranch

    if (-not $branch.StartsWith("feature/")) {
        throw "feature-sync must run from a feature branch."
    }

    Ensure-CleanWorkingTree
    Invoke-Git fetch origin
    Invoke-Git rebase origin/develop
}

function Run-Verification {
    $repoRoot = Get-RepoRoot
    Push-Location $repoRoot

    try {
        Invoke-DotNet restore "OpenJobEngine.sln"
        Invoke-DotNet build "OpenJobEngine.sln" -c Release
    }
    finally {
        Pop-Location
    }
}

function Get-BranchSuffix {
    param([string]$Prefix)

    $branch = Get-CurrentBranch

    if (-not $branch.StartsWith($Prefix)) {
        throw "Current branch must start with '$Prefix'."
    }

    return $branch.Substring($Prefix.Length)
}

function Finish-Feature {
    param([string]$FeatureName)

    Require-GitFlow
    Ensure-CleanWorkingTree
    Run-Verification

    if ([string]::IsNullOrWhiteSpace($FeatureName)) {
        $FeatureName = Get-BranchSuffix "feature/"
    }

    Invoke-Git flow feature finish $FeatureName
    Write-Host "Feature finished. Push develop with: git push origin develop"
}

function Start-Release {
    param([string]$VersionName)

    if ([string]::IsNullOrWhiteSpace($VersionName)) {
        throw "Release version is required."
    }

    Ensure-CleanWorkingTree
    Require-GitFlow
    Invoke-Git checkout develop
    Invoke-Git fetch origin
    Invoke-Git pull --ff-only origin develop
    Invoke-Git flow release start $VersionName
}

function Finish-Release {
    param([string]$VersionName)

    Require-GitFlow
    Ensure-CleanWorkingTree
    Run-Verification

    if ([string]::IsNullOrWhiteSpace($VersionName)) {
        $VersionName = Get-BranchSuffix "release/"
    }

    Invoke-Git flow release finish $VersionName
    Write-Host "Release finished. Push with:"
    Write-Host "  git push origin develop"
    Write-Host "  git push origin main --tags"
}

function Start-Hotfix {
    param([string]$VersionName)

    if ([string]::IsNullOrWhiteSpace($VersionName)) {
        throw "Hotfix version is required."
    }

    Ensure-CleanWorkingTree
    Require-GitFlow
    Invoke-Git checkout main
    Invoke-Git fetch origin
    Invoke-Git pull --ff-only origin main
    Invoke-Git flow hotfix start $VersionName
}

function Finish-Hotfix {
    param([string]$VersionName)

    Require-GitFlow
    Ensure-CleanWorkingTree
    Run-Verification

    if ([string]::IsNullOrWhiteSpace($VersionName)) {
        $VersionName = Get-BranchSuffix "hotfix/"
    }

    Invoke-Git flow hotfix finish $VersionName
    Write-Host "Hotfix finished. Push with:"
    Write-Host "  git push origin develop"
    Write-Host "  git push origin main --tags"
}

switch ($Command) {
    "init" { Initialize-GitFlow }
    "feature-start" { Start-Feature $Name }
    "feature-sync" { Sync-Feature }
    "feature-finish" { Finish-Feature $Name }
    "release-start" { Start-Release $Name }
    "release-finish" { Finish-Release $Name }
    "hotfix-start" { Start-Hotfix $Name }
    "hotfix-finish" { Finish-Hotfix $Name }
    default {
        throw "Unknown command '$Command'. Supported commands: init, feature-start, feature-sync, feature-finish, release-start, release-finish, hotfix-start, hotfix-finish."
    }
}
