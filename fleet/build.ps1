$scriptPath = Get-Location
$projectPath = Join-Path $scriptPath -ChildPath "\FleetPluginBuild\FleetPluginBuild.csproj"


$DotnetArgs = @()
$DotnetArgs = $DotnetArgs + "run"
$DotnetArgs = $DotnetArgs + "--project"
$DotnetArgs = $DotnetArgs + $projectPath
$DotnetArgs = $DotnetArgs + "--configuration" + "Release"
$DotnetArgs = $DotnetArgs + "--"
$DotnetArgs = $DotnetArgs + "All"
$DotnetArgs = $DotnetArgs + $scriptPath
$DotnetArgs = $DotnetArgs + "local_sdk"
& dotnet $DotnetArgs