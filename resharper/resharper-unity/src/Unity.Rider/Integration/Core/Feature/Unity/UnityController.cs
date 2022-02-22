using System;
using System.Diagnostics;
using System.IO;
using System.Threading.Tasks;
using JetBrains.Annotations;
using JetBrains.Application.Components;
using JetBrains.Application.Threading.Tasks;
using JetBrains.Collections.Viewable;
using JetBrains.Core;
using JetBrains.Lifetimes;
using JetBrains.ProjectModel;
using JetBrains.Rd.Tasks;
using JetBrains.RdBackend.Common.Features;
using JetBrains.ReSharper.Plugins.Unity.Core.ProjectModel;
using JetBrains.ReSharper.Plugins.Unity.Rider.Integration.Core.Feature.UnitTesting;
using JetBrains.ReSharper.Plugins.Unity.Rider.Integration.Protocol;
using JetBrains.ReSharper.Plugins.Unity.UnityEditorIntegration;
using JetBrains.ReSharper.UnitTestFramework.Execution;
using JetBrains.Rider.Backend.Features.Unity;
using JetBrains.Rider.Model.Unity.FrontendBackend;
using JetBrains.Util;

namespace JetBrains.ReSharper.Plugins.Unity.Rider.Integration.Core.Feature.Unity
{
    [SolutionComponent]
    public class UnityController : IUnityController, IHideImplementation<DefaultUnityController>
    {
        private static readonly TimeSpan ourUnityConnectionTimeout = TimeSpan.FromMinutes(10);
        private static readonly string ourUnityTimeoutMessage = $"Unity hasn't connected. Timeout {ourUnityConnectionTimeout.TotalMilliseconds} ms is over.";
        private readonly BackendUnityHost myBackendUnityHost;
        private readonly UnityVersion myUnityVersion;
        private readonly ISolution mySolution;
        private readonly Lifetime myLifetime;
        private readonly FrontendBackendModel myFrontendBackendModel;

        private VirtualFileSystemPath EditorInstanceJsonPath => mySolution.SolutionDirectory.Combine("Library/EditorInstance.json");

        public UnityController(Lifetime lifetime,
                               ISolution solution,
                               BackendUnityHost backendUnityHost,
                               UnityVersion unityVersion)
        {
            if (solution.GetData(ProjectModelExtensions.ProtocolSolutionKey) == null)
                return;

            myBackendUnityHost = backendUnityHost;
            myUnityVersion = unityVersion;
            mySolution = solution;
            myLifetime = lifetime;
            myFrontendBackendModel = solution.GetProtocolSolution().GetFrontendBackendModel();
        }

        public Task<ExitUnityResult> ExitUnityAsync(Lifetime lifetime, bool force)
        {
            var lifetimeDef = Lifetime.DefineIntersection(myLifetime, lifetime);
            if (myBackendUnityHost.BackendUnityModel.Value == null) // no connection
                return Task.FromResult(force ? KillProcess() : new ExitUnityResult(false, "No connection to Unity Editor.", null));

            var protocolTaskSource = new TaskCompletionSource<bool>();
            mySolution.Locks.Tasks.StartNew(lifetimeDef.Lifetime, Scheduling.MainGuard, () => myBackendUnityHost.BackendUnityModel.Value.ExitUnity.Start(lifetimeDef.Lifetime, Unit.Instance)
                .AsTask());
            var protocolTask = protocolTaskSource.Task;

            var waitTask = Task.WhenAny(protocolTask, Task.Delay(TimeSpan.FromSeconds(0.5), lifetimeDef.Lifetime)); // continue on timeout
            return waitTask.ContinueWith(_ =>
            {
                lifetimeDef.Terminate();
                if (protocolTask.Status != TaskStatus.RanToCompletion && force)
                    return WaitModelUpdate();

                return Task.FromResult(new ExitUnityResult(false, "Attempt to close Unity Editor failed.", null));
            }, TaskContinuationOptions.AttachedToParent).Unwrap();
        }

        private Task<ExitUnityResult> WaitModelUpdate()
        {
            var successExitResult = new ExitUnityResult(true, null, null);
            if (!myBackendUnityHost.BackendUnityModel.HasValue())
                return Task.FromResult(successExitResult);

            var taskSource = new TaskCompletionSource<ExitUnityResult>();
            var waitLifetimeDef = myLifetime.CreateNested();
            waitLifetimeDef.SynchronizeWith(taskSource);

            // Wait RdModel Update
            myBackendUnityHost.BackendUnityModel.ViewNull(waitLifetimeDef.Lifetime, _ => taskSource.SetResult(successExitResult));
            return taskSource.Task;
        }

        public int? TryGetUnityProcessId()
        {
            var model = myBackendUnityHost.BackendUnityModel.Value;
            if (model != null)
            {
                if (model.UnityApplicationData.HasValue())
                {
                    return model.UnityApplicationData.Value.UnityProcessId;
                }
            }
            // no protocol connection - try to fallback to EditorInstance.json
            var processIdString = EditorInstanceJson.TryGetValue(EditorInstanceJsonPath, "process_id");
            if (processIdString == null)
                return null;

            // Check exists of process if it was killed by manual and EditorInstance.json wasn't deleted
            var pid = Convert.ToInt32(processIdString);

            // TODO: Be careful removing this redundant cast
            // It caused a build failure on TC, presumably due to different compiler versions or SDKs? However, it
            // builds fine locally, with .NET 5.0.301 ¯\_(ツ)_/¯
            // ReSharper disable once RedundantCast
            return PlatformUtil.ProcessExists(pid) ? pid : (int?) null;
        }

        public Task<int> WaitConnectedUnityProcessId(Lifetime lifetime)
        {
            var source = new TaskCompletionSource<int>();
            var lifetimeDef = Lifetime.DefineIntersection(myLifetime, lifetime);
            lifetimeDef.SynchronizeWith(source);

            myBackendUnityHost.BackendUnityModel.ViewNotNull(
                lifetimeDef.Lifetime,
                (lt, backendUnityModel) =>
                {
                    backendUnityModel.UnityApplicationData.AdviseNotNull(lt, data =>
                    {
                        // We will always get a process ID from the Unity model
                        if (data.UnityProcessId.HasValue)
                            source.TrySetResult(data.UnityProcessId.Value);
                        else
                        {
                            source.TrySetException(new InvalidDataException(
                                "UnityApplicationData from Unity does not contain process ID"));
                        }
                    });
                });

            // ToDo Replace timeout with CancellationToken
            Task.Delay(ourUnityConnectionTimeout, lifetimeDef.Lifetime).ContinueWith(_ =>
            {
                if (source.Task.Status != TaskStatus.RanToCompletion)
                    source.TrySetException(new TimeoutException(ourUnityTimeoutMessage));
            }, lifetimeDef.Lifetime);

            return source.Task;
        }

        [CanBeNull]
        public string[] GetUnityCommandline()
        {
            var unityPathData = myFrontendBackendModel.UnityApplicationData;
            if (!unityPathData.HasValue())
                return null;
            var unityPath = unityPathData.Value?.ApplicationPath;
            if (unityPath != null && PlatformUtil.RuntimePlatform == PlatformUtil.Platform.MacOsX)
                unityPath = VirtualFileSystemPath.Parse(unityPath, InteractionContext.SolutionContext).Combine("Contents/MacOS/Unity").FullPath;

            return unityPath == null
                ? null
                : new[] { unityPath, "-projectPath", CommandLineUtil.QuoteIfNeeded(mySolution.SolutionDirectory.FullPath) };
        }

        public bool IsUnityGeneratedProject(IProject project)
        {
            return project.IsUnityGeneratedProject();
        }

        public bool IsUnityEditorUnitTestRunStrategy(IUnitTestRunStrategy strategy) => strategy is RunViaUnityEditorStrategy;

        public Version GetUnityVersion() => myUnityVersion.ActualVersionForSolution.Value;

        public string GetPresentableUnityVersion()
        {
            var unityPathData = myFrontendBackendModel.UnityApplicationData;
            return unityPathData.HasValue() ? unityPathData.Value.ApplicationVersion : null;
        }

        private ExitUnityResult KillProcess()
        {
            ExitUnityResult result = null;
            try
            {
                var possibleProcessId = TryGetUnityProcessId();
                if (possibleProcessId > 0)
                {
                    Process process = null;
                    try
                    {
                        process = Process.GetProcessById((int) possibleProcessId);
                    }
                    catch (Exception)
                    {
                        // process may not be running
                    }

                    if (process != null)
                    {
                        process.Kill();
                        result = new ExitUnityResult(true, null, null);
                    }
                }
            }
            catch (Exception e)
            {
                result = new ExitUnityResult(false, "Exception on attempt to kill Unity Editor process", e);
            }

            return result;
        }
    }
}