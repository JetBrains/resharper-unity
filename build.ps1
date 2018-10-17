param (
  [string]$Source, # Rider SDK Packages folder, optional
  [string]$BuildCounter = 9999, # Sets Rider plugin version to version from Packaging.Props with the last zero replaced by $BuildCounter
  [string]$Configuration = "Debug", # Release / Debug
  [switch]$RunTests,
  [switch]$Verbose
)

Set-StrictMode -Version Latest
[System.IO.Directory]::SetCurrentDirectory($PSScriptRoot)

$gradleArgs = @()

($MyInvocation.MyCommand.Parameters).Keys | ForEach-Object {
  if ($_ -eq "Verbose") {
      return
  }

  $val = (Get-Variable -Name $_ -EA SilentlyContinue).Value
  if($val.ToString().length -gt 0) {    

    $gradleArgs += "-P$($_)=$($val)"
  }
}

if ($Verbose) {
    $gradleArgs += "--info"
    $gradleArgs += "--stacktrace"
}

Write-Host "gradleArgs=$gradleArgs"

Push-Location -Path rider
Try {

    .\gradlew "buildPlugin" $gradleArgs

    $code = $LastExitCode
    Write-Host "Gradle finished: $code"

    if ($code -ne 0) { throw "Exec: Unable to build plugin: exit code $LastExitCode" }
}
Finally {
    Pop-Location
}
