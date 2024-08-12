using System;
using JetBrains.Collections.Viewable;
using JetBrains.Diagnostics;
using JetBrains.Lifetimes;
using JetBrains.Rd.Tasks;
using JetBrains.Rider.Model.Unity.BackendUnity;
using UnityEditor;

namespace JetBrains.Rider.Unity.Editor.UnitTesting
{
  internal static class UnitTestingModelHelper
  {
    private static readonly ILog ourLogger = Log.GetLog("Initialization");

    public static void Advise(Lifetime appDomainLifetime, Lifetime modelLifetime, BackendUnityModel model)
    {
      ourLogger.Verbose("AdviseUnitTestLaunch");

      model.GetCompilationResult.Set(_ => !EditorUtility.scriptCompilationFailed);
      model.UnitTestLaunch.Advise(modelLifetime, launch =>
      {
        new TestEventsSender(appDomainLifetime, launch);
        UnityEditorTestLauncher.SupportAbortNew(launch); // TestFramework 1.2.x
      });

      model.RunUnitTestLaunch.Set(rdVoid =>
      {
        if (!model.UnitTestLaunch.HasValue()) return false;
        if (EditorApplication.isPlaying)
          throw new InvalidOperationException("Running tests during the Play mode is not possible.");
        var testLauncher = new UnityEditorTestLauncher(model.UnitTestLaunch.Value, modelLifetime);
        return testLauncher.TryLaunchUnitTests();
      });
    }
  }
}