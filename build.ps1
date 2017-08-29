param (
  [string]$Source, # Rider SDK Packages folder, optional
  [string]$BuildCounter = 9999, # Sets Rider plugin version to version from Packaging.Props with the last zero replaced by $BuildCounter
  [string]$SinceBuild, # Set since-build in Rider plugin descriptor
  [string]$UntilBuild, # Set until-build in Rider plugin descriptor
  [string]$Configuration = "Release", # Release / Debug
  [switch]$NoBuild, # Skip building and packing, just set package versions and restore packages
  [switch]$RunIde # Build Rider project only, then call gradle runIde
)

$isUnix = [System.Environment]::OSVersion.Platform -eq "Unix"

if ($isUnix){
    $platform = "Unix"
}
else{
    $platform = "Windows"
}

if ($RunIde){
    $gradleTask = "runIde"
}
else{
    $gradleTask = "buildPlugin"
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

function SetPackagingPropsVersion($buildCounter){
  $packagingPropsPath = "./Packaging.props"
  $xml = [xml] (Get-Content $packagingPropsPath)
  # 2.0.0.0 -> 2.0.0.$buildCounter
  $xml.Project.PropertyGroup.Version = $xml.Project.PropertyGroup.Version -replace "((\d+\.)+)\d+", "`${1}$buildCounter"
  $xml.Project.PropertyGroup.AssemblyVersion = $xml.Project.PropertyGroup.AssemblyVersion -replace "((\d+\.)+)\d+", "`${1}$buildCounter"
  $xml.Save($packagingPropsPath)  
  Write-Host "- ${packagingPropsPath}: buildCounter set to $buildCounter"
}

# this is for the ease of local debugging
function SetDefaultExtsInBuildGradle($version, $configuration){
    $buildGradlePath = "./rider/build.gradle"
    $content = [IO.File]::ReadAllText($buildGradlePath)

    # ext.myArgs = "Release,1.0.0" -> ext.myArgs = "$configuration,$version"
    $content = $content -replace "(ext\.myArgs = )`"[^,]+,(\d+\.)+\d+`"", "`${1}`"$configuration,$version`""

    [IO.File]::WriteAllText($buildGradlePath, $content)    
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

function SetRiderSDKVersions($sdkPackageVersion)
{
  Write-Host "Setting versions:"
  Write-Host "  JetBrains.Rider.SDK -> $sdkPackageVersion"
  Write-Host "  JetBrains.Rider.SDK.Tests -> $sdkPackageVersion"  

  SetPropertyValue  "resharper/Directory.Build.props" "RiderSDKVersion" "[$sdkPackageVersion]"  
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

# XML parser will unescape the characters and they will be saved unescaped. bad for us
function SetNuspecVersion($nuspec, $versionToSet){
    $content = [IO.File]::ReadAllText($nuspec)

    # ${2} match is -rider suffix or nothing
    # <version>1.0.0-rider</version> -> <version>2.0.0.500</version>
    # <version>1.0.0</version> -> <version>2.0.0.500</version>
    $content = $content -replace "\<version\>(\d+\.)+\d+(.*)\<\/version\>", "<version>$versionToSet`${2}</version>"

    [IO.File]::WriteAllText($nuspec, $content)
    return $nuspec
}

function CreateConfigurationNuspec($nuspec){
    $content = [IO.File]::ReadAllText($nuspec)

    # <file src="..\..\build\resharper-unity.rider\bin\Release\net452\JetBrains.ReSharper.Plugins.Unity.dll" target="DotFiles" />
    # to
    # <file src="..\..\build\resharper-unity.rider\bin\$Configuration\net452\JetBrains.ReSharper.Plugins.Unity.dll" target="DotFiles" />
    $content = $content -replace "(\<file src=`"[^`"]*)Release([^`"]*)", "`${1}$Configuration`${2}"

    $output = $nuspec.Replace(".nuspec", ".$Configuration.nuspec")
    [IO.File]::WriteAllText($output, $content)
    $generatedNuspecs.Add($output)
    return $output
}

function CreatePlatformNuspec($nuspec){  
    $content = [IO.File]::ReadAllText($nuspec)
    if ($isUnix){
      # Nuget on mono doesn't like the '../..', so fix up the path, relative to current dir
      $content = $content -replace "\.\.\\\.\.", (Join-Path (Get-Location).Path "resharper")
      # fixup DOS-style slashes
      $content = $content -replace "\\", "/"      
    }

    $output = $nuspec.Replace(".nuspec", ".$platform.nuspec")
    [IO.File]::WriteAllText($output, $content)
    $generatedNuspecs.Add($output)
    return $output
}

function DeleteGeneratedNuspecs(){
    foreach ($f in $generatedNuspecs){
        Remove-Item $f
    }
}
function PackNuget($id, $versionToSet){    
    $nuspecPath = "resharper/src/resharper-unity/resharper-unity.$id.nuspec"    
    $nuspecPath = SetNuspecVersion $nuspecPath $versionToSet        
    $nuspecPath = CreateConfigurationNuspec $nuspecPath
    $nuspecPath = CreatePlatformNuspec $nuspecPath
    Write-Host $nuspecPath        

    ServiceMessage "progressMessage" "Building and Packaging: $id"
    if ($isUnix){  
      & nuget pack $nuspecPath -OutputDirectory resharper/build/resharper-unity.$id/bin/$Configuration
    }
    else{
      $nuspecFilename = Split-Path $nuspecPath -leaf
      & dotnet pack resharper/src/resharper-unity/resharper-unity.$id.csproj /p:Configuration=$Configuration /p:NuspecFile=$nuspecFilename --no-build  
    }
    if ($LastExitCode -ne 0) { throw "Exec: Unable to dotnet pack: exit code $LastExitCode" }
    ServiceMessage "publishArtifacts" "resharper/build/resharper-unity.$id/bin/$Configuration/*.nupkg"
}

function ServiceMessage($type, $message){
    if (!($RunIde)){
        Write-Host "##teamcity[$type '$message']"
    }
}

if ($Source) {
  $sdkPackageVersion = GetPackageVersionFromFolder $Source "JetBrains.Rider.SDK"
  SetRiderSDKVersions -sdkPackageVersion $sdkPackageVersion
}

if (!$RunIde){
    ServiceMessage "progressMessage" "Restoring packages"
    if ($Source) {
      & dotnet restore --source https://api.nuget.org/v3/index.json --source $Source resharper/src/resharper-unity.sln
    } else {
      & dotnet restore resharper/src/resharper-unity.sln
    }
    if ($LastExitCode -ne 0) { throw "Exec: Unable to dotnet restore: exit code $LastExitCode" }
}

if ($NoBuild) { Exit 0 }

SetPackagingPropsVersion($BuildCounter)
$version = GetBasePluginVersion "Packaging.props" "Version"

Invoke-Expression ".\merge-unity-3d-rider.ps1 -inputDir resharper\src\resharper-unity\Unity3dRider\Assets\Plugins\Editor\JetBrains -version $version"

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

ServiceMessage "progressMessage" "Building"
if ($RunIde){
    & $msbuild resharper\src\resharper-unity\resharper-unity.rider.csproj /p:Configuration=$Configuration
}
else{
    & $msbuild resharper\src\resharper-unity.sln /p:Configuration=$Configuration
}
if ($LastExitCode -ne 0) { throw "Exec: Unable to build solution: exit code $LastExitCode" }

try{
    if (!($RunIde)){
        PackNuget "wave08" $version
    }
    PackNuget "rider" $version
}
finally{
    DeleteGeneratedNuspecs
}

SetDefaultExtsInBuildGradle -version $version -configuration $Configuration

### Pack Rider plugin directory

SetIdeaVersion -file "rider/src/main/resources/META-INF/plugin.xml" -since $SinceBuild -until $UntilBuild

ServiceMessage "buildNumber" "$version"
SetPluginVersion -file "rider/src/main/resources/META-INF/plugin.xml" -version $version

Push-Location -Path rider
if ($isUnix){
  .\gradlew $gradleTask "-PmyArgs=$Configuration,$version"
}
else{
  .\gradlew.bat $gradleTask "-PmyArgs=$Configuration,$version"
}
if ($LastExitCode -ne 0) { throw "Exec: Unable to build Rider front end plugin: exit code $LastExitCode" }
Pop-Location
