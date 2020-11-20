using System;
using System.Diagnostics;
using System.Threading.Tasks;
using JetBrains.Annotations;
using JetBrains.Application.Threading;
using JetBrains.Application.UI.Components;
using JetBrains.Collections.Viewable;
using JetBrains.Core;
using JetBrains.Diagnostics;
using JetBrains.Lifetimes;
using JetBrains.ProjectModel;
using JetBrains.Rd.Base;
using JetBrains.Rd.Tasks;
using JetBrains.Rider.Model.Unity;
using JetBrains.Rider.Model.Unity.BackendUnity;
using JetBrains.Util;

namespace JetBrains.ReSharper.Plugins.Unity.Rider.Protocol
{
    [SolutionComponent]
    public class BackendUnityHost
    {
        private readonly JetBrains.Application.ActivityTrackingNew.UsageStatistics myUsageStatistics;

        private UnityEditorState myEditorState;

        // The property value will be null when the backend/Unity protocol is not available
        [NotNull]
        public readonly ViewableProperty<BackendUnityModel> BackendUnityModel = new ViewableProperty<BackendUnityModel>(null);

        public BackendUnityHost(Lifetime lifetime, ILogger logger,
                                FrontendBackendHost frontendBackendHost,
                                IThreading threading, IIsApplicationActiveState isApplicationActiveState,
                                JetBrains.Application.ActivityTrackingNew.UsageStatistics usageStatistics)
        {
            myUsageStatistics = usageStatistics;

            myEditorState = UnityEditorState.Disconnected;

            BackendUnityModel.ViewNotNull(lifetime, (modelLifetime, backendUnityModel) =>
            {
                InitialiseModel(backendUnityModel);
                AdviseModel(backendUnityModel, modelLifetime);
                StartPollingUnityEditorState(backendUnityModel, modelLifetime, frontendBackendHost, threading,
                    isApplicationActiveState, logger);
            });
            BackendUnityModel.ViewNull(lifetime, _ =>
            {
                myEditorState = UnityEditorState.Disconnected;
                if (frontendBackendHost.IsAvailable)
                    UpdateFrontendEditorState(frontendBackendHost, logger);
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

        public bool IsConnectionEstablished()
        {
            return myEditorState != UnityEditorState.Refresh && myEditorState != UnityEditorState.Disconnected;
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

        private void TrackActivity(BackendUnityModel backendUnityModel, Lifetime modelLifetime)
        {
            backendUnityModel.UnityApplicationData.AdviseOnce(modelLifetime, data =>
            {
                // ApplicationVersion may look like `2017.2.1f1-CustomPostfix`
                var unityVersion = UnityVersion.VersionToString(UnityVersion.Parse(data.ApplicationVersion));
                myUsageStatistics.TrackActivity("UnityVersion", unityVersion);
                if (data.ApplicationVersion.StartsWith(unityVersion) && unityVersion != data.ApplicationVersion)
                    myUsageStatistics.TrackActivity("UnityIsCustomBuild", unityVersion);
            });
            backendUnityModel.UnityProjectSettings.ScriptingRuntime.AdviseOnce(modelLifetime, runtime =>
            {
                myUsageStatistics.TrackActivity("ScriptingRuntime", runtime.ToString());
            });
        }

        private void StartPollingUnityEditorState(BackendUnityModel backendUnityModel, Lifetime modelLifetime,
                                                  FrontendBackendHost frontendBackendHost,
                                                  IThreading threading,
                                                  IIsApplicationActiveState isApplicationActiveState,
                                                  ILogger logger)
        {
            modelLifetime.StartAsync(threading.Tasks.GuardedMainThreadScheduler, async () =>
            {
                // TODO: This would be much easier with a property
                // Would have to reset the property when the connection drops
                while (modelLifetime.IsAlive)
                {
                    if (isApplicationActiveState.IsApplicationActive.Value
                        || frontendBackendHost.Model?.RiderFrontendTests.HasTrueValue() == true)
                    {
                        PollEditorState(backendUnityModel, frontendBackendHost, modelLifetime, threading, logger);
                    }

                    await Task.Delay(1000, modelLifetime);
                }
            });
        }

        private void PollEditorState(BackendUnityModel backendUnityModel, FrontendBackendHost frontendBackendHost,
                                     Lifetime modelLifetime, IThreading threading, ILogger logger)
        {
            if (!backendUnityModel.IsBound)
            {
                myEditorState = UnityEditorState.Disconnected;
                UpdateFrontendEditorState(frontendBackendHost, logger);
                return;
            }

            var task = backendUnityModel.GetUnityEditorState.Start(Unit.Instance);
            task?.Result.AdviseOnce(modelLifetime, result =>
            {
                logger.Trace($"Got poll result from Unity editor: {result.Result}");
                myEditorState = result.Result;
                UpdateFrontendEditorState(frontendBackendHost, logger);
            });

            Task.Delay(TimeSpan.FromSeconds(2), modelLifetime).ContinueWith(_ =>
            {
                if (task != null && !task.AsTask().IsCompleted)
                {
                    logger.Trace(
                        "There were no response from Unity in two seconds. Setting state to Disconnected.");
                    myEditorState = UnityEditorState.Disconnected;
                    UpdateFrontendEditorState(frontendBackendHost, logger);
                }
            }, threading.Tasks.GuardedMainThreadScheduler);
        }

        private void UpdateFrontendEditorState(FrontendBackendHost frontendBackendHost, ILogger logger)
        {
            logger.Trace($"Sending connection state to frontend: {myEditorState}");
            frontendBackendHost.Do(m => m.UnityEditorState.Value = myEditorState);
        }
    }
}