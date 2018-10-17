param (
  [string]$Source, # Rider SDK Packages folder, optional
  [string]$BuildCounter = 9999, # Sets Rider plugin version to version from Packaging.Props with the last zero replaced by $BuildCounter
  [string]$Configuration = "Debug", # Release / Debug
  [switch]$RunTests,
  [switch]$Verbose
)

$ErrorActionPreference = [System.Management.Automation.ActionPreference]::Stop

Set-StrictMode -Version Latest
[System.IO.Directory]::SetCurrentDirectory($PSScriptRoot)

$isUnix = [System.Environment]::OSVersion.Platform -eq "Unix"

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

    # rdgen currently complains about Kotlin stdlib problems. AppVeyor will capture that and stop the build
    # We'll temporarily tell the powershell host to continue, invoke rdgen and then reset. Calling buildPlugin
    # will invoke the generateModel task again, but it will be up to date and AppVeyor's powershell implementation
    # won't see the warning and treat it as an error. This is a temporary workaround until rdgen can build without
    # any warnings
    $ErrorActionPreference = [System.Management.Automation.ActionPreference]::Continue
    .\gradlew "generateModel" $gradleArgs
    $ErrorActionPreference = [System.Management.Automation.ActionPreference]::Stop

    .\gradlew "buildPlugin" $gradleArgs

    $code = $LastExitCode
    Write-Host "Gradle finished: $code"

    if ($code -ne 0) { throw "Exec: Unable to build plugin: exit code $LastExitCode" }
}
Finally {
    Pop-Location
}
