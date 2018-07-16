param (
  [string]$Source, # Rider SDK Packages folder, optional
  [string]$BuildCounter = 9999, # Sets Rider plugin version to version from Packaging.Props with the last zero replaced by $BuildCounter
  [string]$Configuration = "Release", # Release / Debug
  [switch]$RunTests
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
  .\gradlew --info --stacktrace "buildPlugin" $gradleArgs
}
else{
  .\gradlew.bat --info --stacktrace "buildPlugin" $gradleArgs
}

if ($LastExitCode -ne 0) { throw "Exec: Unable to build plugin: exit code $LastExitCode" }
Pop-Location