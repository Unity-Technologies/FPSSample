# Execute the HDEditorCLI from a powsershell scripts
# UNITY_PATH environment variable must be set to the folder containing Unity.exe

Param
(
    [parameter(Mandatory=$true)]
    [ValidateSet("ResetMaterialKeywords")]
    [String]
    $Operation
)

function FindUnityProjectRootPath
{
    Param
    (
        [parameter(Mandatory=$true)]
        [string]
        $Path
    )

    $folderName = [System.IO.Path]::GetFileNameWithoutExtension("$Path");
    $currentPath = $Path;

    while ($folderName -ne "Assets" -and -not [string]::IsNullOrEmpty("$currentPath"))
    {
        $currentPath = Split-Path -Path $currentPath -Parent;
        $folderName = [System.IO.Path]::GetFileNameWithoutExtension("$currentPath");
    }

    return Split-Path -Path $currentPath -Parent;
}

function FindUnityBin
{
    return [System.IO.Path]::Combine("$env:UNITY_PATH", "Unity.exe");
}

function GetLogFilePath
{
    Param
    (
        [parameter(Mandatory=$true)]
        [string]
        $ProjectPath
    )

    $libPath = [System.IO.Path]::Combine("$ProjectPath", "Library");

    if (-not [System.IO.Directory]::Exists("$libPath"))
    {
        [System.IO.Directory]::CreateDirectory("$libPath");
    }

    return [System.IO.Path]::Combine("$libPath", "hdcli.log");
}

$projectPath = FindUnityProjectRootPath $PSScriptRoot;
if ([string]::IsNullOrEmpty("$projectPath"))
{
    throw "Unity Project Path was not found"
}

$unityPath = FindUnityBin
if (-not [System.IO.File]::Exists("$unityPath"))
{
    throw "Unity binary was not found at $unityPath";
}

$logFilePath = GetLogFilePath $projectPath

$cmd = $unityPath;
$params = '-projectPath', "$projectPath", '-quit', '-batchmode', '-logFile', "$logFilePath", '-executeMethod', 'UnityEditor.Experimental.Rendering.HDPipeline.HDEditorCLI.Run', '-operation',  "$Operation"

if (Test-Path $logFilePath)
{
    Remove-Item $logFilePath
}

Start-Process $cmd $params

while ((Test-Path $logFilePath) -eq $false) 
{
    Start-Sleep -Milliseconds 100
}

$readJob = Start-Job -Name Reader -Arg $logFilePath -ScriptBlock {
    param($file)

    Get-Content $file -Tail 1 -Wait
}

Write-Host "Starting Task"

while ($true) 
{
    $lines = Receive-Job -Name Reader
    $theEnd = $false;
    foreach ($line in $lines)
    {
        if ($line.Contains("[HDEditorCLI]"))
        {
            Write-Host $line;
        }
        if ($line.Contains("Exiting batchmode"))
        {
            $theEnd = $true;
            break;
        }
    }

    if ($theEnd)
        { break; }

    Start-Sleep -Milliseconds 500
}

Stop-Job $readJob

Write-Host "Task Complete"