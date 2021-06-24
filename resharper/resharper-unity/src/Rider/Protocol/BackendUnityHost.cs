using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using JetBrains.Annotations;
using JetBrains.Application.Components;
using JetBrains.Application.Threading;
using JetBrains.Application.UI.Components;
using JetBrains.Collections.Viewable;
using JetBrains.Core;
using JetBrains.Diagnostics;
using JetBrains.Lifetimes;
using JetBrains.ProjectModel;
using JetBrains.ProjectModel.ProjectsHost;
using JetBrains.ProjectModel.ProjectsHost.Impl;
using JetBrains.ProjectModel.ProjectsHost.LiveTracking;
using JetBrains.ProjectModel.ProjectsHost.SolutionHost;
using JetBrains.ProjectModel.ProjectsHost.SolutionHost.Impl;
using JetBrains.Rd.Base;
using JetBrains.Rd.Tasks;
using JetBrains.ReSharper.Plugins.Unity.Packages;
using JetBrains.ReSharper.Resources.Shell;
using JetBrains.Rider.Model.Unity;
using JetBrains.Rider.Model.Unity.BackendUnity;
using JetBrains.Util;

namespace JetBrains.ReSharper.Plugins.Unity.Rider.Protocol
{
    // This component manages subscriptions to the backend/Unity protocol
    // * BackendUnityHost should subscribe to the protocol and push into a component, or subscribe to a component and
    //   push into the protocol
    // * Use PassthroughHost to set up subscriptions between frontend and Unity
    // * Avoid using BackendUnityModel for subscriptions. It should be used to get values and start tasks
    // These guidelines help avoid introducing circular dependencies. Subscriptions should be handled by the host
    [SolutionComponent]
    public class BackendUnityHost
    {
        private readonly ISolution mySolution;
        private readonly JetBrains.Application.ActivityTrackingNew.UsageStatistics myUsageStatistics;

        private UnityEditorState myEditorState;

        private FileContentTracker myFileContentTracker;
        private SolutionHost mySolutionHost;

        // Do not use for subscriptions! Should only be used to read values and start tasks.
        // The property's value will be null when the backend/Unity protocol is not available
        [NotNull]
        public readonly ViewableProperty<BackendUnityModel> BackendUnityModel = new ViewableProperty<BackendUnityModel>(null);

        // TODO: Remove FrontendBackendHost. It's too easy to get circular dependencies
        public BackendUnityHost(Lifetime lifetime, ILogger logger,
                                FrontendBackendHost frontendBackendHost,
                                IThreading threading,
                                IIsApplicationActiveState isApplicationActiveState,
                                PackageManager packageManager,
                                ISolution solution,
                                JetBrains.Application.ActivityTrackingNew.UsageStatistics usageStatistics)
        {
            mySolution = solution;
            myUsageStatistics = usageStatistics;

            myEditorState = UnityEditorState.Disconnected;
            myFileContentTracker = solution.ProjectsHostContainer().GetComponent<FileContentTracker>();
            mySolutionHost = solution.ProjectsHostContainer().GetComponent<SolutionHost>();

            BackendUnityModel.ViewNotNull(lifetime, (modelLifetime, backendUnityModel) =>
            {
                InitialiseModel(backendUnityModel);
                AdviseModel(backendUnityModel, modelLifetime, packageManager);
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
            if (PlatformUtil.RuntimePlatform == PlatformUtil.Platform.Windows)
            {
                // RiderProcessId is only used on Windows (for AllowSetForegroundWindow)
                var frontendProcess = Process.GetCurrentProcess().GetParent();
                if (frontendProcess != null)
                    backendUnityModel.RiderProcessId.SetValue(frontendProcess.Id);
            }
        }

        // Subscribe to changes from the protocol
        private void AdviseModel(BackendUnityModel backendUnityModel, Lifetime modelLifetime,
                                 PackageManager packageManager)
        {
            backendUnityModel.FileChanges.Advise(modelLifetime, list =>
            {
                IEnumerable<IProjectMark> projectMarks;
                using (ReadLockCookie.Create())
                {
                    foreach (var (filePath, content) in list)
                    {
                        var location = FileSystemPath.Parse(filePath);
                        var projectFile = mySolution.FindProjectItemsByLocation(location).OfType<IProjectFile>().SelectBestProjectFile();
                        if (projectFile == null)
                            location.WriteAllText(content, Encoding.Unicode);
                        else
                            myFileContentTracker.Write(location, content);
                    }
                    projectMarks = list
                        .Select(a =>
                            mySolution.FindProjectByProjectFilePath(FileSystemPath.Parse(a.FilePath)).GetProjectMark())
                        .WhereNotNull().ToArray();
                }

                mySolutionHost.ReloadProjectsAsync(projectMarks);
                mySolutionHost.ReloadSolution();
            });
            AdvisePackages(backendUnityModel, modelLifetime, packageManager);
            TrackActivity(backendUnityModel, modelLifetime);
        }

        private void AdvisePackages(BackendUnityModel backendUnityModel, Lifetime modelLifetime,
                                    PackageManager packageManager)
        {
            backendUnityModel.UnityApplicationData.Advise(modelLifetime, _ =>
            {
                // When the backend gets new application data, refresh packages, so we can be up to date with
                // builtin packages. Note that we don't refresh when we lose the model. This means we're
                // potentially viewing stale builtin packages, but that's ok. It's better than clearing all packages
                packageManager.RefreshPackages();
            });
        }

        private void TrackActivity(BackendUnityModel backendUnityModel, Lifetime modelLifetime)
        {
            backendUnityModel.UnityApplicationData.AdviseOnce(modelLifetime, data =>
            {
                // ApplicationVersion may look like `2017.2.1f1-CustomPostfix`
                var unityVersion = UnityVersion.VersionToString(UnityVersion.Parse(data.ApplicationVersion));
                if (data.ApplicationVersion.StartsWith(unityVersion) && unityVersion != data.ApplicationVersion)
                    myUsageStatistics.TrackActivity("UnityVersion", unityVersion + "-custom"); // impersonate, but still track that it is custom build
                else
                    myUsageStatistics.TrackActivity("UnityVersion", unityVersion);
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