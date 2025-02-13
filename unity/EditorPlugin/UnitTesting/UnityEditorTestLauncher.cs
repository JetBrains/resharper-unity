using System;
using System.Linq;
using System.Reflection;
using JetBrains.Collections.Viewable;
using JetBrains.Diagnostics;
using JetBrains.Rd.Tasks;
using JetBrains.Rider.Model.Unity.BackendUnity;

namespace JetBrains.Rider.Unity.Editor.UnitTesting
{
  internal class UnityEditorTestLauncher
  {
    private readonly UnitTestLaunch myLaunch;

    private static readonly ILog ourLogger = Log.GetLog("RiderPlugin");

    public UnityEditorTestLauncher(UnitTestLaunch launch)
    {
      myLaunch = launch;
    }

    public bool TryLaunchUnitTests()
    {
      var success = TryLaunchUnitTestsInternal();
      if (success)
        return true;
      return false;
    }

    private bool TryLaunchUnitTestsInternal()
    {
      try
      {
        var riderPackageAssembly = RiderPackageInterop.GetAssembly();
        var launcherType = riderPackageAssembly?.GetType("Packages.Rider.Editor.UnitTesting.RiderTestRunner");
        if (launcherType == null) return false;
        var assemblyNames = myLaunch.TestFilters.Select(a => a.AssemblyName).ToArray();
        var testNames = myLaunch.TestFilters.SelectMany(a => a.TestNames).ToArray();
        var groupNames = myLaunch.TestFilters.SelectMany(a => a.GroupNames).ToArray();
        var categoryNames = myLaunch.TestFilters.SelectMany(a => a.TestCategories).ToArray();
        var mode = (int) myLaunch.TestMode; // 0 for Both, 1 for Edit, 2 for Play

        MethodInfo runTestsMethod;
        object[] args;

        if (myLaunch.ClientControllerInfo != null)
        {
          ourLogger.Verbose($"ClientController specified (SessionId={myLaunch.SessionId}): {myLaunch.ClientControllerInfo.TypeName}, {myLaunch.ClientControllerInfo.CodeBase}");

          runTestsMethod = launcherType.GetMethod("RunTestsWithSyncCallbacks");
          if (runTestsMethod == null)
          {
            ourLogger.Verbose($"Method 'RunTestsWithSyncCallbacks' not found in type '{launcherType.AssemblyQualifiedName}'");
            return false;
          }

          args = new object[] {myLaunch.SessionId.ToString(), mode, assemblyNames, testNames, categoryNames, groupNames, null,
            myLaunch.ClientControllerInfo.CodeBase, myLaunch.ClientControllerInfo.TypeName,
            myLaunch.ClientControllerInfo.CodeBaseDependencies?.ToArray() };
        }
        else
        {
          runTestsMethod = launcherType.GetMethod("RunTests");
          if (runTestsMethod == null)
          {
            ourLogger.Verbose($"Method 'RunTests' not found in type '{launcherType.AssemblyQualifiedName}'");
            return false;
          }

          args = new object[] {mode, assemblyNames, testNames, categoryNames, groupNames, null};
        }

        runTestsMethod.Invoke(null, args);
        return true;
      }
      catch (Exception e)
      {
        ourLogger.Error(e, "Exception while TryLaunchUnitTestsInternal.");
      }

      return false;
    }

    internal static void SupportAbortNew(UnitTestLaunch launch)
    {
      try
      {
        var riderPackageAssembly = RiderPackageInterop.GetAssembly();
        var apiType = riderPackageAssembly?.GetType("Packages.Rider.Editor.UnitTesting.RiderTestRunner");
        if (apiType == null)
        {
          ourLogger.Verbose($"Could not find RiderTestRunner in the {riderPackageAssembly}.");
          return;
        };
        
        var methodName = "CancelTestRun";
        var stopRunMethod = apiType.GetMethod(methodName); 
        if (stopRunMethod == null)
          stopRunMethod = apiType.GetMethod(methodName, BindingFlags.Static | BindingFlags.NonPublic);
        if (stopRunMethod == null) 
          ourLogger.Verbose($"Could not find {methodName} in the {apiType.Name}.");

        launch.Abort.Set((lifetime, _) =>
        {
          var task = new RdTask<bool>();

          if (stopRunMethod != null)
          {
            ourLogger.Verbose($"Call {methodName} method via reflection.");
            try
            {
              stopRunMethod.Invoke(null, null);
              task.Set(true);
              if (!launch.RunStarted.HasTrueValue()) // if RunStarted never happened
                  launch.RunResult(new RunResult(false));
            }
            catch (Exception)
            {
              ourLogger.Verbose($"Call {methodName} method failed.");
              task.Set(false);
            }
          }
          else
            task.Set(false);

          return task;
        });
      }
      catch (Exception e)
      {
        ourLogger.Error(e, "Unexpected exception in SupportAbortNew");
      }
    }
  }
}