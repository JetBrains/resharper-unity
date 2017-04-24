param (
  [string]$Source, # Rider SDK Packages folder, optional
  [string]$BuildCounter, # Set Rider plugin version to version from Packaging.Props + $BuildCounter, optional
  [string]$SinceBuild, # Set since-build in Rider plugin descriptor
  [string]$UntilBuild, # Set until-build in Rider plugin descriptor
  [switch]$NoBuild # Skip building and packing, just set package versions and restore packages
)

Set-StrictMode -Version Latest; $ErrorActionPreference = [System.Management.Automation.ActionPreference]::Stop
[System.IO.Directory]::SetCurrentDirectory($PSScriptRoot)

function SetPropertyValue($file, $name, $value)
{
  Write-Host "- ${file}: $name -> $value"

  $xml = New-Object xml
  $xml.PreserveWhitespace = $true
  $xml.Load($file)
  
  $node = $xml.SelectSingleNode("//$name")
  if($node -eq $null) { Write-Error "$name was not found in $file" }

  if ($node.InnerText -ne $value) {
    $node.InnerText = $value
    $xml.Save($file)
  }
}

function GetBasePluginVersion($packagingPropsFile)
{
  $xml = New-Object xml
  $xml.Load($packagingPropsFile)
  
  $node = $xml.SelectSingleNode("//Version")
  if($node -eq $null) { Write-Error "//Version was not found in $packagingPropsFile" }

  $version = $node.InnerText

  Write-Host "- ${packagingPropsFile}: version = $version"

  return $version
}

function SetIdeaVersion($file, $since, $until)
{
  if ($since -or $until) {
    Write-Host "- ${file}: since-build -> $since, until-build -> $until"

    $xml = New-Object xml
    $xml.PreserveWhitespace = $true
    $xml.Load($file)
  
    $node = $xml.SelectSingleNode("//idea-version")
    if($node -eq $null) { Write-Error "idea-build was not found in $file" }

    if ($since) {
      $node.SetAttribute("since-build", $since)
    }

    if ($until) {
      $node.SetAttribute("until-build", $until)
    }

    $xml.Save($file)
  }
}

function SetPluginVersion($file, $version)
{
  Write-Host "- ${file}: version -> $version"

  $xml = New-Object xml
  $xml.PreserveWhitespace = $true
  $xml.Load($file)
  
  $node = $xml.SelectSingleNode("//version")
  if($node -eq $null) { Write-Error "//version was not found in $file" }

  $node.InnerText = $version
  $xml.Save($file)
}

function SetRiderSDKVersions($sdkPackageVersion, $sdkTestsPackageVersion, $psiFeaturesVisualStudioVersion)
{
  Write-Host "Setting versions:"
  Write-Host "  JetBrains.ReSharper.SDK -> $sdkPackageVersion"
  Write-Host "  JetBrains.ReSharper.SDK.Tests -> $sdkTestsPackageVersion"
  Write-Host "  JetBrains.Psi.Features.VisualStudio -> $psiFeaturesVisualStudioVersion"  

  SetPropertyValue  "Directory.Build.props" "RiderJetBrainsPsiFeaturesVisualStudioVersion" "[$psiFeaturesVisualStudioVersion]"
  SetPropertyValue  "Directory.Build.props" "RiderJetBrainsReSharperSDKVersion" "[$sdkPackageVersion]"
  SetPropertyValue  "Directory.Build.props" "RiderJetBrainsReSharperSDKTestsVersion" "[$sdkTestsPackageVersion]"
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

if ($Source) {
  $sdkPackageVersion = GetPackageVersionFromFolder $Source "JetBrains.ReSharper.SDK"
  $sdkTestsPackageVersion = GetPackageVersionFromFolder $Source "JetBrains.ReSharper.SDK.Tests"
  $psiFeaturesVisualStudioVersion = GetPackageVersionFromFolder $Source "JetBrains.Psi.Features.VisualStudio"
  SetRiderSDKVersions -sdkPackageVersion $sdkPackageVersion -sdkTestsPackageVersion $sdkTestsPackageVersion -psiFeaturesVisualStudioVersion $psiFeaturesVisualStudioVersion
}

Write-Host "##teamcity[progressMessage 'Restoring packages']"
if ($Source) {
  & dotnet restore --source $Source --source https://api.nuget.org/v3/index.json src/resharper-unity.sln
} else {
  & dotnet restore src/resharper-unity.sln
}
if ($LastExitCode -ne 0) { throw "Exec: Unable to dotnet restore: exit code $LastExitCode" }

if ($NoBuild) { Exit 0 }

$vspath = .\tools\vswhere -latest -products * -requires Microsoft.Component.MSBuild -property installationPath
if (!$vspath) {
  Write-Error "Could not find Visual Studio 2017+ for MSBuild 15"
}

$msbuild = join-path $vspath 'MSBuild\15.0\Bin\MSBuild.exe'
if (!(test-path $msbuild)) {
  Write-Error "MSBuild 15 is expected at $msbuild"
}  

Write-Host "##teamcity[progressMessage 'Building']"
& $msbuild src\resharper-unity.sln /p:Configuration=Release
if ($LastExitCode -ne 0) { throw "Exec: Unable to build solution: exit code $LastExitCode" }

Write-Host "##teamcity[progressMessage 'Building and Packaging: Wave08']"
& dotnet pack src/resharper-unity/resharper-unity.wave08.csproj /p:Configuration=Release /p:NuspecFile=resharper-unity.wave08.nuspec --no-build
if ($LastExitCode -ne 0) { throw "Exec: Unable to dotnet pack: exit code $LastExitCode" }
Write-Host "##teamcity[publishArtifacts 'build/resharper-unity.wave08/bin/Release/*.nupkg']"

Write-Host "##teamcity[progressMessage 'Building and Packaging: Rider']"
& dotnet pack src/resharper-unity/resharper-unity.rider.csproj /p:Configuration=Release /p:NuspecFile=resharper-unity.rider.nuspec --no-build
if ($LastExitCode -ne 0) { throw "Exec: Unable to dotnet pack: exit code $LastExitCode" }
Write-Host "##teamcity[publishArtifacts 'build/resharper-unity.rider/bin/Release/*.nupkg']"

### Pack Rider plugin directory
SetIdeaVersion -file "rider/src/main/resources/META-INF/plugin.xml" -since $SinceBuild -until $UntilBuild

$baseVersion = GetBasePluginVersion "Packaging.props"
if ($BuildCounter) {
  $version = "$baseVersion.$BuildCounter"
} else {
  $version = $baseVersion
}

Write-Host "##teamcity[buildNumber '$version']"
SetPluginVersion -file "rider/src/main/resources/META-INF/plugin.xml" -version $version

Push-Location -Path rider
.\gradle.bat buildPlugin
if ($LastExitCode -ne 0) { throw "Exec: Unable to build Rider front end plugin: exit code $LastExitCode" }
Pop-Location

# TODO: This might need to be $version
Write-Host "##teamcity[publishArtifacts 'rider\build\distributions\rider-unity-1.6.2.zip']"
