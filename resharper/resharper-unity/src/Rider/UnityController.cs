using System;
using System.Diagnostics;
using System.Threading.Tasks;
using JetBrains.Annotations;
using JetBrains.Application.Components;
using JetBrains.Application.Threading.Tasks;
using JetBrains.Collections.Viewable;
using JetBrains.Core;
using JetBrains.Lifetimes;
using JetBrains.ProjectModel;
using JetBrains.Rd.Tasks;
using JetBrains.ReSharper.Host.Features;
using JetBrains.ReSharper.Host.Features.Unity;
using JetBrains.ReSharper.Plugins.Unity.ProjectModel;
using JetBrains.ReSharper.Plugins.Unity.Rider.UnitTesting;
using JetBrains.ReSharper.UnitTestFramework.Strategy;
using JetBrains.Rider.Model;
using JetBrains.Util;

namespace JetBrains.ReSharper.Plugins.Unity.Rider
{
    [SolutionComponent]
    public class UnityController : IUnityController, IHideImplementation<DefaultUnityController>
    {
        private static readonly TimeSpan ourUnityConnectionTimeout = TimeSpan.FromMinutes(10);
        private static readonly string ourUnityTimeoutMessage = $"Unity hasn't connected. Timeout {ourUnityConnectionTimeout.TotalMilliseconds} ms is over.";
        private readonly UnityEditorProtocol myUnityEditorProtocol;
        private readonly UnityVersion myUnityVersion;
        private readonly ISolution mySolution;
        private readonly Lifetime myLifetime;
        private readonly RdUnityModel myRdUnityModel;

        private FileSystemPath EditorInstanceJsonPath => mySolution.SolutionDirectory.Combine("Library/EditorInstance.json");

        public UnityController(Lifetime lifetime, 
                               ISolution solution,
                               UnityEditorProtocol unityEditorProtocol,
                               UnityVersion unityVersion)

        {
            if (solution.GetData(ProjectModelExtensions.ProtocolSolutionKey) == null)
                return;

            myUnityEditorProtocol = unityEditorProtocol;
            myUnityVersion = unityVersion;
            mySolution = solution;
            myLifetime = lifetime;
            myRdUnityModel = solution.GetProtocolSolution().GetRdUnityModel();
        }

        public Task<ExitUnityResult> ExitUnityAsync(bool force)
        {
            var lifetimeDef = myLifetime.CreateNested();
            if (myUnityEditorProtocol.UnityModel.Value == null) // no connection
            {
                if (force)
                {
                    return Task.FromResult(KillProcess());
                }

                return Task.FromResult(new ExitUnityResult(false, "No connection to Unity Editor.", null));
            }

            var protocolTaskSource = new TaskCompletionSource<bool>();
            mySolution.Locks.Tasks.StartNew(lifetimeDef.Lifetime, Scheduling.MainGuard, () => myUnityEditorProtocol.UnityModel.Value.ExitUnity.Start(lifetimeDef.Lifetime, Unit.Instance)
                .AsTask());
            var protocolTask = protocolTaskSource.Task;

            var waitTask = Task.WhenAny(protocolTask, Task.Delay(TimeSpan.FromSeconds(0.5), lifetimeDef.Lifetime)); // continue on timeout
            return waitTask.ContinueWith(t =>
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
            if (!myUnityEditorProtocol.UnityModel.HasValue())
                return Task.FromResult(successExitResult);

            var taskSource = new TaskCompletionSource<ExitUnityResult>();
            var waitLifetimeDef = myLifetime.CreateNested();
            waitLifetimeDef.SynchronizeWith(taskSource);

            // Wait RdModel Update
            myUnityEditorProtocol.UnityModel.ViewNull(waitLifetimeDef.Lifetime, _ => taskSource.SetResult(successExitResult));
            return taskSource.Task;
        }

        public int? TryGetUnityProcessId()
        {
            var model = myUnityEditorProtocol.UnityModel.Value;
            if (model != null)
            {
                if (model.UnityProcessId.HasValue())
                {
                    return model.UnityProcessId.Value;
                }
            }
            // no protocol connection - try to fallback to EditorInstance.json
            var processIdString = EditorInstanceJson.TryGetValue(EditorInstanceJsonPath, "process_id");
            return processIdString == null ? (int?) null : Convert.ToInt32(processIdString);
        }

        public Task<int> WaitConnectedUnityProcessId()
        {
            var source = new TaskCompletionSource<int>();
            var lifetimeDef = myLifetime.CreateNested();
            lifetimeDef.SynchronizeWith(source);

            myUnityEditorProtocol.UnityModel.ViewNotNull(
                lifetimeDef.Lifetime,
                (lt, model) => model.UnityProcessId.Advise(lt, id => source.TrySetResult(id)));

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
            var unityPathData = myRdUnityModel.UnityApplicationData;
            if (!unityPathData.HasValue())
                return null;
            var unityPath = unityPathData.Value?.ApplicationPath;
            if (unityPath != null && PlatformUtil.RuntimePlatform == PlatformUtil.Platform.MacOsX)
                unityPath = FileSystemPath.Parse(unityPath).Combine("Contents/MacOS/Unity").FullPath;

            return unityPath == null
                ? null 
                : new[] { CommandLineUtil.QuoteIfNeeded(unityPath), "-projectPath", CommandLineUtil.QuoteIfNeeded(mySolution.SolutionDirectory.FullPath) };
        }

        public bool IsUnityGeneratedProject(IProject project)
        {
            return project.IsUnityGeneratedProject();
        }

        public bool IsUnityEditorUnitTestRunStrategy(IUnitTestRunStrategy strategy) => strategy is RunViaUnityEditorStrategy;

        public Version GetUnityVersion()
        {
            return myUnityVersion.ActualVersionForSolution.Value;
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