Push-Location ((Split-Path $MyInvocation.InvocationName) + "\..\..\rider")

$isUnix = [System.Environment]::OSVersion.Platform -eq "Unix"
$gradleArgs = @("-PBuildConfiguration=Debug", "-PRiderOnly=true", "-PSkipNuGetRestore=true")

if ($isUnix) {
  .\gradlew "buildBackend" $gradleArgs
  if ($LastExitCode -ne 0) { throw "Exec: Unable to build Rider backend plugin: exit code $LastExitCode" }
  .\gradlew "runIde" $gradleArgs
}
else{
  .\gradlew.bat "buildBackend" $gradleArgs
  if ($LastExitCode -ne 0) { throw "Exec: Unable to build Rider backend plugin: exit code $LastExitCode" }
  .\gradlew.bat "runIde" $gradleArgs
}
