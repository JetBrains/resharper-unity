using System.Collections.Generic;
using JetBrains.Application.BuildScript.Compile;
using JetBrains.Application.BuildScript.Solution;
using JetBrains.Build;

namespace JetBrains.ReSharper.Plugins.Unity.BuildScript
{
  public sealed class SubplatformForAutomationTools
  {
    [BuildStep]
    public static IEnumerable<SubplatformComponentForPackaging> Include(AllAssembliesOnEverything aae) =>
      AutomationToolsMarker.IncludeIn<SubplatformForAutomationTools>(aae);
  }
}