param (
  [string]$Configuration = "Debug" # Release / Debug
)

Push-Location ((Split-Path $MyInvocation.InvocationName) + "\..\..\")
Invoke-Expression ".\build.ps1 -Configuration $Configuration"