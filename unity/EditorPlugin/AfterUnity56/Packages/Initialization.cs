#if UNITY_2019_2
using System.Linq;
using JetBrains.Diagnostics;
using JetBrains.Rd.Base;
using UnityEditor;
using UnityEditor.PackageManager;
using UnityEditor.PackageManager.Requests;
#endif

namespace JetBrains.Rider.Unity.Editor.AfterUnity56.Packages
{
    public static class Initialization
    {
#if UNITY_2019_2
        private static string packageId = "com.unity.ide.rider";
        private static readonly ILog ourLogger = Log.GetLog("Packages.Initialization");
        private static SearchRequest ourRequest;
#endif
        public static void OnModelInitializationHandler(UnityModelAndLifetime modelAndLifetime)
        {
#if UNITY_2019_2
            ourRequest = Client.Search(packageId);
            modelAndLifetime.Lifetime.OnTermination(() => { ourRequest = null; });
            EditorApplication.update += () => Progress(modelAndLifetime);
#endif
        }
#if UNITY_2019_2
        static void Progress(UnityModelAndLifetime modelAndLifetime)
        {
            if (ourRequest == null)
            {
                EditorApplication.update -= () => Progress(modelAndLifetime);
                return;
            }

            if (!ourRequest.IsCompleted) return;
            if (ourRequest.Status == StatusCode.Success)
            {
                var latestCompatible = ourRequest.Result.FirstOrDefault()?.versions.latestCompatible;
                if (latestCompatible != null)
                {
                    ourLogger.Info("Found: " + latestCompatible);
                    modelAndLifetime.Model.RiderPackagePotentialUpdateVersion.Set(latestCompatible);
                }
            }
            else if (ourRequest.Status >= StatusCode.Failure)
            {
                ourLogger.Error(ourRequest.Error.message);
            }

            EditorApplication.update -= () => Progress(modelAndLifetime);
        }
#endif
    }
}