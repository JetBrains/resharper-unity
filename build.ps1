param (
  [string]$Source, # Rider SDK Packages folder, optional
  [string]$BuildCounter = 9999, # Sets Rider plugin version to version from Packaging.Props with the last zero replaced by $BuildCounter
  [string]$SinceBuild, # Set since-build in Rider plugin descriptor
  [string]$UntilBuild, # Set until-build in Rider plugin descriptor
  [string]$Configuration = "Release", # Release / Debug
  [switch]$RiderOnly # Build only Rider csprojects
)

Set-StrictMode -Version Latest; $ErrorActionPreference = [System.Management.Automation.ActionPreference]::Stop
[System.IO.Directory]::SetCurrentDirectory($PSScriptRoot)

$isUnix = [System.Environment]::OSVersion.Platform -eq "Unix"

$gradleArgs = @()

($MyInvocation.MyCommand.Parameters ).Keys | ForEach-Object {
  $val = (Get-Variable -Name $_ -EA SilentlyContinue).Value
  if($val.ToString().length -gt 0) {    

    $gradleArgs += "-P$($_)=$($val)"
  }
}

Write-Host "gradleArgs=$gradleArgs"

Push-Location -Path rider
if ($isUnix){
  .\gradlew --info --stacktrace "buildBackend" $gradleArgs
  if ($LastExitCode -ne 0) { throw "Exec: Unable to build Rider backend plugin: exit code $LastExitCode" }
  .\gradlew --info --stacktrace "buildPlugin" $gradleArgs
}
else{
  .\gradlew.bat --info --stacktrace "buildBackend" $gradleArgs
  if ($LastExitCode -ne 0) { throw "Exec: Unable to build Rider backend plugin: exit code $LastExitCode" }
  .\gradlew.bat --info --stacktrace "buildPlugin" $gradleArgs
}

if ($LastExitCode -ne 0) { throw "Exec: Unable to build Rider front end plugin: exit code $LastExitCode" }
Pop-Location
