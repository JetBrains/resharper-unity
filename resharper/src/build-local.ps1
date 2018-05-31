Push-Location ((Split-Path $MyInvocation.InvocationName) + "\..\..\rider")

$isUnix = [System.Environment]::OSVersion.Platform -eq "Unix"
$gradleArgs = @("-PBuildConfiguration=Debug")

if ($isUnix) {
  .\gradlew runIde $gradleArgs
}
else{
  .\gradlew.bat runIde $gradleArgs
}
