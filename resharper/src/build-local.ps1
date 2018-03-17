Push-Location ((Split-Path $MyInvocation.InvocationName) + "\..\..\rider")

$isUnix = [System.Environment]::OSVersion.Platform -eq "Unix"
$gradleArgs = @("-PBuildConfiguration=Debug", "-PRiderOnly=true", "-PSkipNuGetRestore=true")

if ($isUnix) {
  .\gradlew "runIde" $gradleArgs
}
else{
  .\gradlew.bat "runIde" $gradleArgs
}
