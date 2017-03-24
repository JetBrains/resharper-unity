param (
  [string]$Source, # Rider SDK Packages folder, optional
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

  if ($node.Text -ne $value) {
    $node.Text = $value
    $xml.Save($file)
  }
}

function SetRiderSDKVersions($sdkPackageVersion, $sdkTestsPackageVersion, $psiFeaturesVisualStudioVersion)
{
  Write-Host "Setting versions:"
  Write-Host "  JetBrains.ReSharper.SDK -> $sdkPackageVersion"
  Write-Host "  JetBrains.ReSharper.SDK.Tests -> $sdkTestsPackageVersion"
  Write-Host "  JetBrains.Psi.Features.VisualStudio -> $psiFeaturesVisualStudioVersion"  

  SetPropertyValue  "Directory.Build.props" "RiderJetBrainsPsiFeaturesVisualStudioVersion" "[$platformVisualStudioVersion]"
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
  SetSDKVersions -sdkPackageVersion $sdkPackageVersion -sdkTestsPackageVersion $sdkTestsPackageVersion -psiFeaturesVisualStudioVersion $psiFeaturesVisualStudioVersion
}

Write-Host "##teamcity[progressMessage 'Restoring packages']"
if ($Source) {
  & dotnet restore --source $Source --source https://api.nuget.org/v3/index.json src/resharper-unity.sln
} else {
  & dotnet restore src/resharper-unity.sln
}
if ($LastExitCode -ne 0) { throw "Exec: Unable to dotnet restore: exit code $LastExitCode" }

if ($NoBuild) { Exit 0 }

Write-Host "##teamcity[progressMessage 'Building and Packaging: Wave08']"
& dotnet pack src/resharper-unity/resharper-unity.wave08.csproj /p:Configuration=Release /p:NuspecFile=resharper-unity.wave08.nuspec 
if ($LastExitCode -ne 0) { throw "Exec: Unable to dotnet pack: exit code $LastExitCode" }
Write-Host "##teamcity[publishArtifacts 'build/resharper-unity.wave08/bin/Release/*.nupkg']"

Write-Host "##teamcity[progressMessage 'Building and Packaging: Rider']"
& dotnet pack src/resharper-unity/resharper-unity.rider.csproj /p:Configuration=Release /p:NuspecFile=resharper-unity.rider.nuspec 
if ($LastExitCode -ne 0) { throw "Exec: Unable to dotnet pack: exit code $LastExitCode" }
Write-Host "##teamcity[publishArtifacts 'build/resharper-unity.rider/bin/Release/*.nupkg']"

### Pack Rider plugin directory
$dir = "build\zip"
if (Test-Path $dir) { Remove-Item $dir -Force -Recurse }
New-Item $dir -type directory | Out-Null
New-Item $dir\resharper-unity -type directory | Out-Null
Copy-Item build\resharper-unity.rider\bin\Release\*.nupkg $dir\resharper-unity -recurse
Copy-Item rider\* $dir\resharper-unity -recurse

### Pack and publish Rider plugin zip
$zip = "build/JetBrains.Unity.zip"
Compress-Archive -Path $dir\* -Force -DestinationPath $zip
Write-Host "##teamcity[publishArtifacts '$zip']"
