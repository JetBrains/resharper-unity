using System;
using JetBrains.Application.BuildScript.Install;
using JetBrains.Application.BuildScript.Solution;
using JetBrains.Build;

namespace JetBrains.ReSharper.Plugins.Unity.Tests.BuildScript;

public class RequestAppConfigInstall
{
  [BuildStep]
  public static InstallAppConfig[] Run(AllAssembliesOnEverything allass)
  {
    return allass.FindSubplatformByClass<RequestAppConfigInstall>() is SubplatformOnSources subplatform ? new[] {new InstallAppConfig(subplatform.Name, "Unity.Rider.Tests/Unity.Rider.Tests.csproj"), new InstallAppConfig(subplatform.Name, "Unity.Tests/Unity.Tests.csproj")} : Array.Empty<InstallAppConfig>();
  }
}