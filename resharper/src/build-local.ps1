param (
  [string]$Configuration = "Debug", # Release / Debug
  [switch]$RunIde = $True # If true, builds and runs the Rider plugin, else packages whole solution
)

Push-Location ((Split-Path $MyInvocation.InvocationName) + "\..\..\")
$runIdeArg = if ($RunIde) {"-RunIde"} else {""}
Invoke-Expression ".\build.ps1 -Configuration $Configuration $runIdeArg"
