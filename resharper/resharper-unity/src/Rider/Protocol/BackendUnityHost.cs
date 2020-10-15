using System.Diagnostics;
using JetBrains.Annotations;
using JetBrains.Collections.Viewable;
using JetBrains.Diagnostics;
using JetBrains.Lifetimes;
using JetBrains.ProjectModel;
using JetBrains.Rd.Base;
using JetBrains.Rd.Tasks;
using JetBrains.Rider.Model.Unity.BackendUnity;
using JetBrains.Util;

namespace JetBrains.ReSharper.Plugins.Unity.Rider.Protocol
{
    [SolutionComponent]
    public class BackendUnityHost
    {
        private readonly JetBrains.Application.ActivityTrackingNew.UsageStatistics myUsageStatistics;

        // The property value will be null when the backend/Unity protocol is not available
        [NotNull]
        public readonly ViewableProperty<BackendUnityModel> BackendUnityModel = new ViewableProperty<BackendUnityModel>(null);

        public BackendUnityHost(Lifetime lifetime,
                                FrontendBackendHost frontendBackendHost,
                                JetBrains.Application.ActivityTrackingNew.UsageStatistics usageStatistics)
        {
            myUsageStatistics = usageStatistics;

            BackendUnityModel.ViewNotNull(lifetime, (modelLifetime, backendUnityModel) =>
            {
                InitialiseModel(backendUnityModel);
                AdviseModel(backendUnityModel, modelLifetime);
            });

            // Are we testing?
            if (frontendBackendHost.IsAvailable)
            {
                // Tell the frontend if the backend/Unity connection is available
                // (not actually passthrough)
                var frontendBackendModel = frontendBackendHost.Model.NotNull("frontendBackendHost.Model != null");
                BackendUnityModel.FlowIntoRdSafe(lifetime,
                    backendUnityModel => backendUnityModel != null,
                    frontendBackendModel.UnityEditorConnected);
            }
        }

        private static void InitialiseModel(BackendUnityModel backendUnityModel)
        {
            SetConnectionPollHandler(backendUnityModel);
            SetRiderProcessId(backendUnityModel);
        }

        private static void SetConnectionPollHandler(BackendUnityModel backendUnityModel)
        {
            // Set up result for polling. Called before the Unity editor tries to use the protocol to open a
            // file. It ensures that the protocol is connected and active.
            // TODO: Is there a simpler check that the model is still connected?
            backendUnityModel.IsBackendConnected.Set(_ => true);
        }

        private static void SetRiderProcessId(BackendUnityModel backendUnityModel)
        {
            if (PlatformUtil.RuntimePlatform == PlatformUtil.Platform.Windows)
            {
                // RiderProcessId is only used on Windows (for AllowSetForegroundWindow)
                var frontendProcess = Process.GetCurrentProcess().GetParent();
                if (frontendProcess != null)
                    backendUnityModel.RiderProcessId.SetValue(frontendProcess.Id);
            }
        }

        private void AdviseModel(BackendUnityModel backendUnityModel, in Lifetime modelLifetime)
        {
            TrackActivity(backendUnityModel, modelLifetime);
        }

        private void TrackActivity(BackendUnityModel backendUnityModel, Lifetime lf)
        {
            backendUnityModel.UnityApplicationData.AdviseOnce(lf, data => myUsageStatistics.TrackActivity("UnityVersion", data.ApplicationVersion));
            backendUnityModel.ScriptingRuntime.AdviseOnce(lf, runtime => myUsageStatistics.TrackActivity("ScriptingRuntime", runtime.ToString()));
        }
    }
}