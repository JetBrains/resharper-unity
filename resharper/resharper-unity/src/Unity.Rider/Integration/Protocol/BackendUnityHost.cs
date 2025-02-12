using System;
using System.Diagnostics;
using System.Threading.Tasks;
using JetBrains.Application.Parts;
using JetBrains.Application.Threading;
using JetBrains.Application.UI.Components;
using JetBrains.Collections.Viewable;
using JetBrains.Core;
using JetBrains.Diagnostics;
using JetBrains.HabitatDetector;
using JetBrains.Lifetimes;
using JetBrains.ProjectModel;
using JetBrains.Rd.Base;
using JetBrains.Rd.Tasks;
using JetBrains.ReSharper.Plugins.Unity.Rider.Common.Protocol;
using JetBrains.ReSharper.Plugins.Unity.UnityEditorIntegration.Packages;
using JetBrains.Rider.Model.Unity;
using JetBrains.Rider.Model.Unity.BackendUnity;
using JetBrains.Util;

#nullable enable

namespace JetBrains.ReSharper.Plugins.Unity.Rider.Integration.Protocol
{
    // This component manages subscriptions to the backend/Unity protocol
    // * BackendUnityHost should subscribe to the protocol and push into a component, or subscribe to a component and
    //   push into the protocol
    // * Use PassthroughHost to set up subscriptions between frontend and Unity
    // * Avoid using BackendUnityModel for subscriptions. It should be used to get values and start tasks
    // These guidelines help avoid introducing circular dependencies. Subscriptions should be handled by the host
    [SolutionComponent(Instantiation.DemandAnyThreadSafe)]
    public class BackendUnityHost : IBackendUnityHost
    {
        private readonly ILogger myLogger;
        private readonly IThreading myThreading;
        private readonly IIsApplicationActiveState myIsApplicationActiveState;
        private readonly PackageManager myPackageManager;
        private readonly UnityEditorUsageCollector myUnityEditorUsageCollector;
        private readonly UnityProfilerEventsHost myUnityProfilerEventsHost;

        private UnityEditorState myEditorState;
        
        private string? myApplicationPath;

        // Do not use for subscriptions! Should only be used to read values and start tasks.
        // The property's value will be null when the backend/Unity protocol is not available
        public readonly ViewableProperty<BackendUnityModel?> BackendUnityModel = new(null);
        public readonly ViewableProperty<UnityProfilerModel?> BackendUnityProfilerModel  = new(null);
        
        // TODO: Remove FrontendBackendHost. It's too easy to get circular dependencies
        public BackendUnityHost(Lifetime lifetime, 
            ILogger logger,
            FrontendBackendHost frontendBackendHost,
            IThreading threading,
            IIsApplicationActiveState isApplicationActiveState,
            PackageManager packageManager,
            UnityEditorUsageCollector unityEditorUsageCollector,
            UnityProfilerEventsHost unityProfilerEventsHost)
        {
            myLogger = logger;
            myThreading = threading;
            myIsApplicationActiveState = isApplicationActiveState;
            myPackageManager = packageManager;
            myUnityEditorUsageCollector = unityEditorUsageCollector;
            myUnityProfilerEventsHost = unityProfilerEventsHost;

            myEditorState = UnityEditorState.Disconnected;

            threading.ExecuteOrQueueEx(lifetime, GetType().Name, () => MainThreadInit(lifetime, frontendBackendHost));
        }

        private void MainThreadInit(Lifetime lifetime, FrontendBackendHost frontendBackendHost)
        {
            // TODO: ReactiveEx.ViewNotNull isn't NRT ready
            BackendUnityModel!.ViewNotNull<BackendUnityModel>(lifetime, (modelLifetime, backendUnityModel) =>
            {
                Assertion.AssertNotNull(backendUnityModel);
                InitialiseModel(backendUnityModel);
                AdvisePackages(backendUnityModel, modelLifetime, myPackageManager);
                ReportUnityEditorInformationToFus(backendUnityModel, modelLifetime);
                StartPollingUnityEditorState(backendUnityModel, modelLifetime, frontendBackendHost);
            });
            BackendUnityModel!.ViewNull<BackendUnityModel>(lifetime, _ =>
            {
                myEditorState = UnityEditorState.Disconnected;
                if (frontendBackendHost.IsAvailable)
                    UpdateFrontendEditorState(frontendBackendHost);
            });
            
            BackendUnityProfilerModel.ViewNotNull(lifetime, (_, backendProfilerModel) =>
            {
                myUnityProfilerEventsHost.AdviseOpenFileByMethodName(backendProfilerModel, frontendBackendHost, lifetime);
            });

            // Are we testing?
            if (frontendBackendHost.IsAvailable)
            {
                // Tell the frontend if the backend/Unity connection is available
                // (not actually passthrough)
                var frontendBackendModel = frontendBackendHost.Model.NotNull();
                BackendUnityModel.FlowIntoRdSafe(lifetime,
                    backendUnityModel => backendUnityModel != null,
                    frontendBackendModel.UnityEditorConnected);
            }
        }

        public bool IsConnectionEstablished() => myEditorState != UnityEditorState.Refresh &&
                                                 myEditorState != UnityEditorState.Disconnected;

        // Push values into the protocol
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
            if (PlatformUtil.RuntimePlatform == JetPlatform.Windows)
            {
                // RiderProcessId is only used on Windows (for AllowSetForegroundWindow)
                var frontendProcess = Process.GetCurrentProcess().GetParent();
                if (frontendProcess != null)
                    backendUnityModel.RiderProcessId.SetValue(frontendProcess.Id);
            }
        }

        private void AdvisePackages(BackendUnityModel backendUnityModel, Lifetime modelLifetime,
                                    PackageManager packageManager)
        {
            backendUnityModel.UnityApplicationData.AdviseNotNull(modelLifetime, data =>
            {
                // We want to refresh package only if applicationPath is new  
                if (myApplicationPath == data.ApplicationPath) return;
                myApplicationPath = data.ApplicationPath;
                
                // When the backend gets new application data, refresh packages, so we can be up to date with
                // builtin packages. Note that we don't refresh when we lose the model. This means we're
                // potentially viewing stale builtin packages, but that's ok. It's better than clearing all packages
                packageManager.RefreshPackages();
            });
        }

        private void ReportUnityEditorInformationToFus(BackendUnityModel backendUnityModel, Lifetime modelLifetime)
        {
            backendUnityModel.UnityProjectSettings.ScriptingRuntime.AdviseOnce(modelLifetime, runtime =>
            {
                // eventual consistency
                Assertion.Assert(backendUnityModel.UnityApplicationData.HasValue());
                myUnityEditorUsageCollector.SetInformation(backendUnityModel.UnityApplicationData.Value.ApplicationVersion, runtime);
            });
        }

        private void StartPollingUnityEditorState(BackendUnityModel backendUnityModel, Lifetime modelLifetime,
                                                  FrontendBackendHost frontendBackendHost)
        {
            modelLifetime.StartAsync(myThreading.Tasks.GuardedMainThreadScheduler, async () =>
            {
                // TODO: This would be much easier with a property
                // Would have to reset the property when the connection drops
                while (modelLifetime.IsAlive)
                {
                    if (myIsApplicationActiveState.IsApplicationActive.Value
                        || frontendBackendHost.Model?.RiderFrontendTests.HasTrueValue() == true)
                    {
                        PollEditorState(backendUnityModel, frontendBackendHost, modelLifetime);
                    }

                    await Task.Delay(1000, modelLifetime);
                }
            });
        }

        private void PollEditorState(BackendUnityModel backendUnityModel, FrontendBackendHost frontendBackendHost,
                                     Lifetime modelLifetime)
        {
            if (!backendUnityModel.IsBound)
            {
                myEditorState = UnityEditorState.Disconnected;
                UpdateFrontendEditorState(frontendBackendHost);
                return;
            }

            var task = backendUnityModel.GetUnityEditorState.Start(modelLifetime, Unit.Instance);
            task.Result.AdviseOnce(modelLifetime, result =>
            {
                myLogger.Trace($"Got poll result from Unity editor: {result.Result}");
                myEditorState = result.Result;
                UpdateFrontendEditorState(frontendBackendHost);
            });

            Task.Delay(TimeSpan.FromSeconds(2), modelLifetime).ContinueWith(_ =>
            {
                if (!task.AsTask().IsCompleted)
                {
                    myLogger.Trace(
                        "There were no response from Unity in two seconds. Setting state to Disconnected.");
                    myEditorState = UnityEditorState.Disconnected;
                    UpdateFrontendEditorState(frontendBackendHost);
                }
            }, myThreading.Tasks.GuardedMainThreadScheduler);
        }

        private void UpdateFrontendEditorState(FrontendBackendHost frontendBackendHost)
        {
            myLogger.Trace($"Sending connection state to frontend: {myEditorState}");
            frontendBackendHost.Do(m => m.UnityEditorState.Value = myEditorState);
        }
    }
}