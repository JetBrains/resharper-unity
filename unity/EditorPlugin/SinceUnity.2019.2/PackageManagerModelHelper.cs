#if UNITY_2019_2_OR_NEWER

using System.Linq;
using JetBrains.Diagnostics;
using JetBrains.Lifetimes;
using JetBrains.Rd.Base;
using JetBrains.Rider.Model.Unity.BackendUnity;
using UnityEditor;
using UnityEditor.PackageManager;
using UnityEditor.PackageManager.Requests;

namespace JetBrains.Rider.Unity.Editor
{
  public static class PackageManagerModelHelper
  {
    private const string PackageId = "com.unity.ide.rider";
    private static readonly ILog ourLogger = Log.GetLog("Packages.Initialization");

    public static void Advise(Lifetime modelLifetime, BackendUnityModel model)
    {
      var request = Client.Search(PackageId);
      ourLogger.Verbose($"Client.Search({PackageId})");
      var definition = modelLifetime.CreateNested();

      void Action()
      {
        WaitForResult(definition, model, request);
      }

      definition.Lifetime.Bracket(() => EditorApplication.update += Action,
        () => EditorApplication.update -= Action);
    }

    private static void WaitForResult(LifetimeDefinition definition, BackendUnityModel model, SearchRequest request)
    {
      if (definition.Lifetime.IsNotAlive)
        return;

      ourLogger.Trace($"request: {request.Status}");

      if (request.Status == StatusCode.Success)
      {
        var latestCompatible = request.Result.FirstOrDefault()?.versions.latestCompatible;
        if (latestCompatible != null)
        {
          ourLogger.Info("Found: " + latestCompatible);
          model.RiderPackagePotentialUpdateVersion.Set(latestCompatible);
          definition.Terminate();
        }
      }
      else if (request.Status >= StatusCode.Failure)
      {
        ourLogger.Error(request.Error.message);
        definition.Terminate();
      }
    }
  }
}

#endif
