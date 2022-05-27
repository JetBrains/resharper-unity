#if UNITY_2019_2
using System.Linq;
using JetBrains.Diagnostics;
using JetBrains.Rd.Base;
using JetBrains.Rider.Model.Unity.BackendUnity;
using UnityEditor;
using UnityEditor.PackageManager;
using UnityEditor.PackageManager.Requests;
using JetBrains.Lifetimes;
#endif

namespace JetBrains.Rider.Unity.Editor.AfterUnity56.Packages
{
    public static class Initialization
    {
#if UNITY_2019_2
        private static string packageId = "com.unity.ide.rider";
        private static readonly ILog ourLogger = Log.GetLog("Packages.Initialization");
        private static SearchRequest ourRequest;
        private static BackendUnityModel ourModel;
        private static LifetimeDefinition ourDefinition;
#endif
        public static void OnModelInitializationHandler(UnityModelAndLifetime modelAndLifetime)
        {
#if UNITY_2019_2
            ourRequest = Client.Search(packageId);
            ourModel = modelAndLifetime.Model;
            ourLogger.Verbose($"Client.Search({packageId})");
            ourDefinition = modelAndLifetime.Lifetime.CreateNested();
            ourLogger.Verbose($"EditorApplication.update += WaitForResult");
            EditorApplication.update += WaitForResult;
#endif
        }

#if UNITY_2019_2
        private static void WaitForResult()
        {
            ourLogger.Trace($"request: {ourRequest.Status}");
            if (ourDefinition.Lifetime.IsNotAlive)
            {
                ourLogger.Verbose($"EditorApplication.update -= WaitForResult");
                EditorApplication.update -= WaitForResult;
                return;
            }
            
            if (ourRequest.Status == StatusCode.Success)
            {
                var latestCompatible = ourRequest.Result.FirstOrDefault()?.versions.latestCompatible;
                if (latestCompatible != null)
                {
                    ourLogger.Info("Found: " + latestCompatible);
                    ourModel.RiderPackagePotentialUpdateVersion.Set(latestCompatible);
                    ourDefinition.Terminate();
                }
            }
            else if (ourRequest.Status >= StatusCode.Failure)
            {
                ourLogger.Error(ourRequest.Error.message);
                ourDefinition.Terminate();
            }
        }
#endif
    }
}