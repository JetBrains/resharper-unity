Push-Location ((Split-Path $MyInvocation.InvocationName) + "\..\..\rider")

$isUnix = [System.Environment]::OSVersion.Platform -eq "Unix"
$gradleArgs = @("-PConfiguration=Debug", "-PRiderOnly=true", "-PSkipNuGetRestore=true")

if ($isUnix) {
  .\gradlew "buildBackend" $gradleArgs
  .\gradlew "runIde" $gradleArgs
}
else{
  .\gradlew.bat "buildBackend" $gradleArgs
  .\gradlew.bat "runIde" $gradleArgs
}
