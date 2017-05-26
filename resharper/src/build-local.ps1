param (
  [string]$Configuration = "Debug", # Release / Debug
  [string]$GradleTask = "runIde" # runIde / buildPlugin
)

Push-Location ((Split-Path $MyInvocation.InvocationName) + "\..\..\")
Invoke-Expression ".\build.ps1 -Configuration $Configuration -GradleTask $GradleTask"