param (
  [string] [ValidateSet("Wave08", "Wave07", "Folder", "Dev")] $target = "Dev", # 'Folder' takes packages version from specified Folder
  [string]$Source, # SDK Packages folder
  [switch]$NoBuild # Skip building and packing, just set package versions and restore packages
)

Set-StrictMode -Version Latest; $ErrorActionPreference = [System.Management.Automation.ActionPreference]::Stop
[System.IO.Directory]::SetCurrentDirectory($PSScriptRoot)

if ($Source -and $target -ne "Folder") {
  Write-Error "If you specified packages folder you need to set -Target to 'Folder'"
}
if (-not $Source -and $target -eq "Folder") {
  Write-Error "You must specify -Source if target is 'Folder'"
}

function SetPackageReferenceVersion($csproj, $name, $version)
{
  Write-Host "- ${csproj}: package $name -> $version"

  $xml = New-Object xml
  $xml.PreserveWhitespace = $true
  $xml.Load($csproj)
  
  $node = $xml.SelectSingleNode("//PackageReference[@Include='$name']")
  if($node -eq $null) { Write-Error "PackageReference of $name was not found in $csproj" }

  if ($node.Version -ne $version) {
    $node.Version = $version
    $xml.Save($csproj)
  }
}

function ReplaceInFile($file, $what, $value) {
  Write-Host "- ${file}: $what -> $value"

  $content = [System.IO.File]::ReadAllText($file)
  if (-not [regex]::Match($content, $what).Success) {
    Write-Error "Regex $what was not found in file $file"
  }
  $replaced = [regex]::Replace($content, $what, $value)
  [System.IO.File]::WriteAllText($file, $replaced, [System.Text.Encoding]::UTF8)
}

function SetSDKVersions($sdkPackageVersion, $platformVisualStudioVersion, $psiFeaturesVisualStudioVersion)
{
  Write-Host "Setting versions:"
  Write-Host "  JetBrains.ReSharper.SDK -> $sdkPackageVersion"
  Write-Host "  JetBrains.Platform.VisualStudio -> $platformVisualStudioVersion"
  Write-Host "  JetBrains.Psi.Features.VisualStudio -> $psiFeaturesVisualStudioVersion"  

  $wave = [convert]::ToInt32($platformVisualStudioVersion.Split('.')[0], 10) - 100
  Write-Host "  Wave -> $wave"

  SetPackageReferenceVersion "src\ApiParser\ApiParser.csproj" "JetBrains.ReSharper.SDK" $sdkPackageVersion

  SetPackageReferenceVersion "src\resharper-unity\resharper-unity.csproj" "JetBrains.ReSharper.SDK" $sdkPackageVersion
  SetPackageReferenceVersion "src\resharper-unity\resharper-unity.csproj" "JetBrains.Platform.VisualStudio" $platformVisualStudioVersion
  SetPackageReferenceVersion "src\resharper-unity\resharper-unity.csproj" "JetBrains.Psi.Features.VisualStudio" $psiFeaturesVisualStudioVersion

  SetPackageReferenceVersion "test\src\resharper-unity.tests.csproj" "JetBrains.ReSharper.SDK.Tests" $sdkPackageVersion
  SetPackageReferenceVersion "test\src\resharper-unity.tests.csproj" "JetBrains.Platform.VisualStudio" $platformVisualStudioVersion

  ReplaceInFile "src\resharper-unity\resharper-unity.csproj" "WAVE\d+" ("WAVE" + $wave.ToString("00"))
  ReplaceInFile "src\resharper-unity\resharper-unity.nuspec" ([Regex]::Escape('<dependency id="Wave" version="[') + '\d+\.0\]" />') ('<dependency id="Wave" version="[' + $wave + '.0]" />')
}

function GetPackageVersionFromFolder($folder, $name) {
  foreach ($file in Get-ChildItem $folder) {
    $match = [regex]::Match($file.Name, "^" + [Regex]::Escape($name) + "\.((\d+\.)+\d+(\-eap\d+)?)\.nupkg$")
    if ($match.Success) {
      return $match.Groups[1].Value
    }
  }
  Write-Error "Package $name was not found in folder $folder"
}

switch ($target) {
  "Wave08" { 
    SetSDKVersions -sdkPackageVersion "2017.1.20170309.135707-eap04" -platformVisualStudioVersion "108.0.20170309.131657-eap04" -psiFeaturesVisualStudioVersion "108.0.20170309.132110-eap04"
  }
  "Wave07" {
    SetSDKVersions -sdkPackageVersion "2016.3.20170126.124206" -platformVisualStudioVersion "107.0.20170126.120431" -psiFeaturesVisualStudioVersion "107.0.20170126.120917"
  }
  "Folder" {
    $sdkPackageVersion = GetPackageVersionFromFolder $Source "JetBrains.ReSharper.SDK"
    $platformVisualStudioVersion = GetPackageVersionFromFolder $Source "JetBrains.Platform.VisualStudio"
    $psiFeaturesVisualStudioVersion = GetPackageVersionFromFolder $Source "JetBrains.Psi.Features.VisualStudio"
    SetSDKVersions -sdkPackageVersion $sdkPackageVersion -platformVisualStudioVersion $platformVisualStudioVersion -psiFeaturesVisualStudioVersion $psiFeaturesVisualStudioVersion
  }
  "Dev" {
    # Dev target: do not substitute package versions, use defaults
  }
}

Write-Host "##teamcity[progressMessage 'Restoring packages']"
if ($Source) {
  & dotnet restore --source $Source --source https://api.nuget.org/v3/index.json src/resharper-unity.sln
} else {
  & dotnet restore src/resharper-unity.sln
}
if ($LastExitCode -ne 0) { throw "Exec: Unable to dotnet restore: exit code $LastExitCode" }

if ($NoBuild) { Exit 0 }

Write-Host "##teamcity[progressMessage 'Building']"
& dotnet build src/resharper-unity.sln
if ($LastExitCode -ne 0) { throw "Exec: Unable to dotnet build: exit code $LastExitCode" }

Write-Host "##teamcity[progressMessage 'Creating nupkg']"
& dotnet pack src/resharper-unity/resharper-unity.csproj /p:NuspecFile=resharper-unity.nuspec 
if ($LastExitCode -ne 0) { throw "Exec: Unable to dotnet pack: exit code $LastExitCode" }
Write-Host "##teamcity[publishArtifacts 'src/resharper-unity/bin/JetBrains.Unity.*.nupkg']"

### Pack Rider plugin directory
$dir = "src\resharper-unity\bin\zip"
if (Test-Path $dir) { Remove-Item $dir -Force -Recurse }
New-Item $dir -type directory | Out-Null
New-Item $dir\resharper-unity -type directory | Out-Null
Copy-Item src\resharper-unity\bin\JetBrains.Unity.*.nupkg $dir\resharper-unity -recurse
Copy-Item rider\* $dir\resharper-unity -recurse

### Pack and publish Rider plugin zip
$zip = "src/resharper-unity/bin/JetBrains.Unity.zip"
Compress-Archive -Path $dir\* -Force -DestinationPath $zip
Write-Host "##teamcity[publishArtifacts '$zip']"
