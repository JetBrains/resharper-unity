using System;
using JetBrains.ProjectModel;
using JetBrains.ReSharper.Psi;
using JetBrains.Util;
using JetBrains.Util.Dotnet.TargetFrameworkIds;

namespace JetBrains.ReSharper.Host.Features.OnlineHelp
{
  [SolutionComponent]
  public class UnityOnlineHelpProvider : IOnlineHelpProvider
  {
    public Uri GetUrl(ICompiledElement element,
      TargetFrameworkId targetFrameworkId,
      FileSystemPath assemblyLocation)
    {
      if (assemblyLocation == null || !assemblyLocation.ExistsFile)
        return null;

      if (!(assemblyLocation.Name.StartsWith("UnityEngine") || assemblyLocation.Name.StartsWith("UnityEditor")))
        return null;
      return GetUnityUri(element);
    }

    public int GetPriority()
    {
      return 20;
    }

    public bool IsAsync()
    {
      return false;
    }
    
    public static Uri GetUnityUri(ICompiledElement element)
    {
      var searchableText = element.GetSearchableText();
      return searchableText == null
        ? null
        : new Uri($"http://www.unity.com/search?q={Uri.EscapeDataString(searchableText)}");
    }
  }
}