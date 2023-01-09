$scriptPath = Get-Location
$projectPath = Join-Path $scriptPath -ChildPath "\FleetPluginBuild\FleetPluginBuild.csproj"


$DotnetArgs = @()
$DotnetArgs = $DotnetArgs + "run"
$DotnetArgs = $DotnetArgs + "--project"
$DotnetArgs = $DotnetArgs + $projectPath
$DotnetArgs = $DotnetArgs + "--configuration" + "Release"
& dotnet $DotnetArgs