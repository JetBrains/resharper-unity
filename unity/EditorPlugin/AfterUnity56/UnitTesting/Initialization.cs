using System;
using System.Linq;
using System.Reflection;
using JetBrains.Collections.Viewable;
using JetBrains.Diagnostics;
using JetBrains.Platform.Unity.EditorPluginModel;
using JetBrains.Rd.Tasks;
using UnityEditor;

namespace JetBrains.Rider.Unity.Editor.AfterUnity56.UnitTesting
{
  public static class Initialization
  {
    private static readonly ILog ourLogger = Log.GetLog("UnitTesting.Initialization");
    
    public static void OnModelInitializationHandler(UnityModelAndLifetime modelAndLifetime)
    {
      ourLogger.Verbose("AdviseUnitTestLaunch");
      var modelValue = modelAndLifetime.Model;
      var connectionLifetime = modelAndLifetime.Lifetime;
      
      modelValue.GetCompilationResult.Set(_ => !EditorUtility.scriptCompilationFailed);

      CompiledAssembliesTracker.Init(modelAndLifetime);
      
      modelValue.UnitTestLaunch.Advise(connectionLifetime, launch =>
      {
        new TestEventsSender(launch);
        if (!SupportAbort(launch)) 
          SupportAbortNew(launch);
      });
      
      modelValue.RunUnitTestLaunch.Set(rdVoid =>
      {
        if (!modelValue.UnitTestLaunch.HasValue()) return false;
        var testLauncher = new UnityEditorTestLauncher(modelValue.UnitTestLaunch.Value, connectionLifetime);
        return testLauncher.TryLaunchUnitTests();
      });
    }
    
    private static Assembly GetEditorTestRunnerAssembly()
    {
      const string assemblyName = "UnityEditor.TestRunner";
      var assembly = AppDomain.CurrentDomain.GetAssemblies().FirstOrDefault(a => a.GetName().Name.Equals(assemblyName));
      if (assembly == null)
        ourLogger.Verbose($"Could not find {assemblyName} in the AppDomain.");
      return assembly;
    }

    private static bool SupportAbort(UnitTestLaunch launch)
    {
      try
      {
        if (!UnityEditorTestLauncher.TryGetAssemblies(out var testEditorAssembly, out var testEngineAssembly)
        ) return false;
        if (!UnityEditorTestLauncher.TryGetLauncherType(testEditorAssembly, out var launcherType)) return false;
        if (!UnityEditorTestLauncher.TryGetFilter(testEngineAssembly, out var filter)) return false;
        if (!UnityEditorTestLauncher.TryGetLauncher(new string[] { }, testEngineAssembly, testEditorAssembly,
          launcherType, filter, out var launcher))
          return false;
        if (!UnityEditorTestLauncher.TryGetRunner(launcherType, launcher, out var runner)) return false;
        return SubscribeToAbort(runner, launch);
      }
      catch (Exception e)
      {
        ourLogger.Error(e, "Unexpected exception in SupportAbort");
        return false;
      }
    }

    private static bool SubscribeToAbort(object runner, UnitTestLaunch launch)
    {
      var unityTestAssemblyRunnerField =
        runner.GetType().GetField("m_Runner", BindingFlags.Instance | BindingFlags.NonPublic);
      if (unityTestAssemblyRunnerField == null)
      {
        ourLogger.Verbose("Could not find m_Runner field.");
        return false;
      }

      var unityTestAssemblyRunner = unityTestAssemblyRunnerField.GetValue(runner);
      var stopRunMethod = unityTestAssemblyRunner.GetType()
        .GetMethod("StopRun", BindingFlags.Instance | BindingFlags.Public);
      if (stopRunMethod == null)
      {
        ourLogger.Verbose("Could not find StopRun method.");
        return false;
      }

      launch.Abort.Set((lifetime, _) =>
      {
        ourLogger.Verbose("Call StopRun method via reflection.");
        var task = new RdTask<bool>();
        try
        {
          stopRunMethod.Invoke(unityTestAssemblyRunner, null);
          task.Set(true);
        }
        catch (Exception)
        {
          ourLogger.Verbose("Call StopRun method failed.");
          task.Set(false);
        }

        return task;
      });

      return true;
    }

    private static void SupportAbortNew(UnitTestLaunch launch)
    {
      try
      {
        const string typeName = "UnityEditor.TestTools.TestRunner.Api.TestRunnerApi";
        const string methodName = "CancelAllTestRuns";

        MethodInfo stopRunMethod = null;
        var assembly = GetEditorTestRunnerAssembly();
        if (assembly != null)
        {
          var apiType = assembly.GetType(typeName);
          if (apiType == null)
            ourLogger.Verbose($"Could not find {typeName}.");
          else
          {
            stopRunMethod = apiType.GetMethod(methodName);
            if (stopRunMethod == null)
              ourLogger.Verbose($"Could not find {methodName} in the {typeName}.");
          }
        }
      
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
            }
            catch (Exception)
            {
              ourLogger.Verbose($"Call {methodName} method failed.");
              task.Set(false);
            }
          }
          else
            task.Set(false);
        
          if (!launch.RunStarted.HasTrueValue()) // if RunStarted never happened 
            launch.RunResult(new RunResult(false));
        
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