using System.Linq;
using JetBrains.Diagnostics;
using JetBrains.Rd.Base;
using UnityEditor;
#if UNITY_2017_3
using UnityEditor.PackageManager;
using UnityEditor.PackageManager.Requests;
#endif

namespace JetBrains.Rider.Unity.Editor.AfterUnity56.Packages
{
    public static class Initialization
    {
        private static string packageId = "com.unity.ide.rider";
        private static readonly ILog ourLogger = Log.GetLog("Packages.Initialization");
#if UNITY_2017_3
        private static SearchRequest ourRequest;
#endif
        public static void OnModelInitializationHandler(UnityModelAndLifetime modelAndLifetime)
        {
#if UNITY_2017_3
            ourRequest = Client.Search(packageId);
            modelAndLifetime.Lifetime.OnTermination(() => { ourRequest = null; });
            EditorApplication.update += () => Progress(modelAndLifetime);
#endif
        }

        static void Progress(UnityModelAndLifetime modelAndLifetime)
        {
#if UNITY_2017_3
            if (ourRequest == null)
            {
                EditorApplication.update -= () => Progress(modelAndLifetime);
                return;
            }

            if (!ourRequest.IsCompleted) return;
            if (ourRequest.Status == StatusCode.Success)
            {
                var res = ourRequest.Result.FirstOrDefault();
                if (res != null)
                {
                    ourLogger.Info("Found: " + res.version);
                    //  ?.versions.latestCompatible
                    modelAndLifetime.Model.RiderPackagePotentialUpdateVersion.Set(res.version);
                }
            }
            else if (ourRequest.Status >= StatusCode.Failure)
            {
                ourLogger.Error(ourRequest.Error.message);
            }

            EditorApplication.update -= () => Progress(modelAndLifetime);
#endif
        }
    }
}