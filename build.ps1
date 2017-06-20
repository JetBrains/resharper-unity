param (
  [string]$Source, # Rider SDK Packages folder, optional
  [string]$BuildCounter, # Set Rider plugin version to version from Packaging.Props + $BuildCounter, optional
  [string]$SinceBuild, # Set since-build in Rider plugin descriptor
  [string]$UntilBuild, # Set until-build in Rider plugin descriptor
  [string]$Configuration = "Release", # Release / Debug
  [string]$GradleTask = "buildPlugin", # buildPlugin / runIde
  [switch]$NoBuild # Skip building and packing, just set package versions and restore packages
)

$isUnix = [System.Environment]::OSVersion.Platform -eq "Unix"
if ($isUnix){
    $platform = "Unix"
}
else{
    $platform = "Windows"
}

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

function GetBasePluginVersion($packagingPropsFile, $nodeName)
{
  $xml = New-Object xml
  $xml.Load($packagingPropsFile)
  
  $node = $xml.SelectSingleNode("//$nodeName")
  if($node -eq $null) { Write-Error "//$nodeName was not found in $packagingPropsFile" }

  $version = $node.InnerText

  Write-Host "- ${packagingPropsFile}: $nodeName = $version"

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

  SetPropertyValue  "resharper/Directory.Build.props" "RiderJetBrainsPsiFeaturesVisualStudioVersion" "[$psiFeaturesVisualStudioVersion]"
  SetPropertyValue  "resharper/Directory.Build.props" "RiderJetBrainsReSharperSDKVersion" "[$sdkPackageVersion]"
  SetPropertyValue  "resharper/Directory.Build.props" "RiderJetBrainsReSharperSDKTestsVersion" "[$sdkTestsPackageVersion]"
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

$generatedNuspecs = {}.Invoke()

function CreateConfigurationNuspec($nuspec){
    $xml = [xml](Get-Content $nuspec)
    foreach ($f in $xml.package.files.ChildNodes){
        $f.src = $f.src.Replace("\Release\", "\$Configuration\")
    }

    $output = $nuspec.Replace(".nuspec", ".$Configuration.nuspec")
    $xml.Save($output)
    $generatedNuspecs.Add($output)
    return $output
}

function CreatePlatformNuspec($nuspec){
    $xml = [xml](Get-Content $nuspec)
    if ($isUnix){
        foreach ($f in $xml.package.files.ChildNodes){
            $f.src = $f.src.Replace("\", "/")
        }        
    }

    $output = $nuspec.Replace(".nuspec", ".$platform.nuspec")
    $xml.Save($output)
    $generatedNuspecs.Add($output)
    return $output
}

function DeleteGeneratedNuspecs(){
    foreach ($f in $generatedNuspecs){
        Remove-Item $f
    }
}

function PackNuget($id){
    $nuspecPath = "resharper/src/resharper-unity/resharper-unity.$id.nuspec"
    $nuspecPath = CreateConfigurationNuspec $nuspecPath
    $nuspecPath = CreatePlatformNuspec $nuspecPath
    Write-Host $nuspecPath

    Write-Host "##teamcity[progressMessage 'Building and Packaging: $id']"
    if ($isUnix){  
      & nuget pack $nuspecPath -OutputDirectory resharper/build/resharper-unity.$id/bin/$Configuration
    }
    else{
      $nuspecFilename = Split-Path $nuspecPath -leaf
      & dotnet pack resharper/src/resharper-unity/resharper-unity.$id.csproj /p:Configuration=$Configuration /p:NuspecFile=$nuspecFilename --no-build  
    }
    if ($LastExitCode -ne 0) { throw "Exec: Unable to dotnet pack: exit code $LastExitCode" }
    Write-Host "##teamcity[publishArtifacts 'resharper/build/resharper-unity.$id/bin/$Configuration/*.nupkg']"    
}

if ($Source) {
  $sdkPackageVersion = GetPackageVersionFromFolder $Source "JetBrains.ReSharper.SDK"
  $sdkTestsPackageVersion = GetPackageVersionFromFolder $Source "JetBrains.ReSharper.SDK.Tests"
  $psiFeaturesVisualStudioVersion = GetPackageVersionFromFolder $Source "JetBrains.Psi.Features.VisualStudio"
  SetRiderSDKVersions -sdkPackageVersion $sdkPackageVersion -sdkTestsPackageVersion $sdkTestsPackageVersion -psiFeaturesVisualStudioVersion $psiFeaturesVisualStudioVersion
}

Write-Host "##teamcity[progressMessage 'Restoring packages']"
if ($Source) {
  & dotnet restore --source $Source --source https://api.nuget.org/v3/index.json resharper/src/resharper-unity.sln
} else {
  & dotnet restore resharper/src/resharper-unity.sln
}
if ($LastExitCode -ne 0) { throw "Exec: Unable to dotnet restore: exit code $LastExitCode" }

if ($NoBuild) { Exit 0 }

$assemblyVersion = GetBasePluginVersion "Packaging.props" "AssemblyVersion"
Invoke-Expression ".\merge-unity-3d-rider.ps1 -inputDir resharper\src\resharper-unity\Unity3dRider\Assets\Plugins\Editor\JetBrains -version $assemblyVersion"

if ($isUnix){
  $msbuild = which msbuild
}
else{
  $vspath = .\tools\vswhere -latest -products * -requires Microsoft.Component.MSBuild -property installationPath
  if (!$vspath) {
    Write-Error "Could not find Visual Studio 2017+ for MSBuild 15"
  }
  $msbuild = join-path $vspath 'MSBuild\15.0\Bin\MSBuild.exe'
}

if (!(test-path $msbuild)) {
  Write-Error "MSBuild 15 is expected at $msbuild"
}  

Write-Host "##teamcity[progressMessage 'Building']"
& $msbuild resharper\src\resharper-unity.sln /p:Configuration=$Configuration
if ($LastExitCode -ne 0) { throw "Exec: Unable to build solution: exit code $LastExitCode" }

try{
    PackNuget "wave08"
    PackNuget "rider"
}
finally{
    DeleteGeneratedNuspecs
}

### Pack Rider plugin directory
$baseVersion = GetBasePluginVersion "Packaging.props" "Version"
if ($BuildCounter) {
  $version = "$baseVersion.$BuildCounter"
} else {
  $version = $baseVersion
}

SetIdeaVersion -file "rider/src/main/resources/META-INF/plugin.xml" -since $SinceBuild -until $UntilBuild

Write-Host "##teamcity[buildNumber '$version']"
SetPluginVersion -file "rider/src/main/resources/META-INF/plugin.xml" -version $version

Push-Location -Path rider
if ($isUnix){
  .\gradlew $GradleTask "-PmyArgs=$Configuration,$baseVersion"
}
else{
  .\gradlew.bat $GradleTask "-PmyArgs=$Configuration,$baseVersion"
}
if ($LastExitCode -ne 0) { throw "Exec: Unable to build Rider front end plugin: exit code $LastExitCode" }
Pop-Location
