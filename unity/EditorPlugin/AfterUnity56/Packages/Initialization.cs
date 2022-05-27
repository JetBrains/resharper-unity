#if UNITY_2019_2_OR_NEWER
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
#if UNITY_2019_2_OR_NEWER
        private static string packageId = "com.unity.ide.rider";
        private static readonly ILog ourLogger = Log.GetLog("Packages.Initialization");
#endif
        public static void OnModelInitializationHandler(UnityModelAndLifetime modelAndLifetime)
        {
#if UNITY_2019_2_OR_NEWER
            var request = Client.Search(packageId);
            var model = modelAndLifetime.Model;
            ourLogger.Verbose($"Client.Search({packageId})");
            var definition = modelAndLifetime.Lifetime.CreateNested();

            void Action()
            {
                WaitForResult(definition, model, request);
            }

            definition.Lifetime.Bracket(() =>
            {
                ourLogger.Verbose($"EditorApplication.update += WaitForResult");
                EditorApplication.update += Action;
            }, () =>
            {
                ourLogger.Verbose($"EditorApplication.update -= WaitForResult");
                EditorApplication.update -= Action;
            });
#endif
        }

#if UNITY_2019_2_OR_NEWER
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
#endif
    }
}