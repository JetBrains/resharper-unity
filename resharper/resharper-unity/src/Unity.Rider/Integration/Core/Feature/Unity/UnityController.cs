using System;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Threading.Tasks;
using JetBrains.Application.Components;
using JetBrains.Application.Threading;
using JetBrains.Application.Threading.Tasks;
using JetBrains.Application.UI.Controls;
using JetBrains.Collections.Viewable;
using JetBrains.HabitatDetector;
using JetBrains.Lifetimes;
using JetBrains.ProjectModel;
using JetBrains.Rd.Tasks;
using JetBrains.ReSharper.Feature.Services.Protocol;
using JetBrains.ReSharper.Plugins.Unity.Core.ProjectModel;
using JetBrains.ReSharper.Plugins.Unity.Rider.Integration.Core.Feature.UnitTesting;
using JetBrains.ReSharper.Plugins.Unity.Rider.Integration.Protocol;
using JetBrains.ReSharper.Plugins.Unity.Rider.Resources;
using JetBrains.ReSharper.Plugins.Unity.UnityEditorIntegration;
using JetBrains.ReSharper.UnitTestFramework.Execution;
using JetBrains.Rider.Backend.Features.Unity;
using JetBrains.Rider.Model.Unity;
using JetBrains.Rider.Model.Unity.FrontendBackend;
using JetBrains.Util;

namespace JetBrains.ReSharper.Plugins.Unity.Rider.Integration.Core.Feature.Unity
{
    [SolutionComponent]
    public class UnityController : IUnityController, IHideImplementation<DefaultUnityController>
    {
        private static readonly TimeSpan ourUnityConnectionTimeout = TimeSpan.FromMinutes(10);
        private static readonly RpcTimeouts ourUnityStartProfilingTimeouts = RpcTimeouts.Maximal;
        private static readonly string ourUnityTimeoutMessage = $"Unity hasn't connected. Timeout {ourUnityConnectionTimeout.TotalMilliseconds} ms is over.";
        
        private readonly BackendUnityHost myBackendUnityHost;
        private readonly UnityVersion myUnityVersion;
        private readonly UnitySolutionTracker myUnitySolutionTracker;
        private readonly IThreading myThreading;
        private readonly ILogger myLogger;
        private readonly ISolution mySolution;
        private readonly Lifetime myLifetime;
        private readonly FrontendBackendModel myFrontendBackendModel;
        private readonly IBackgroundProgressIndicatorManager myIndicatorManager;
        
        private readonly object myStartUnitySync = new();
        private Task<int> myStartUnityTask = Task.FromResult(0);
        
        public UnityController(Lifetime lifetime,
                               ISolution solution,
                               BackendUnityHost backendUnityHost,
                               IBackgroundProgressIndicatorManager indicatorManager,
                               UnityVersion unityVersion,
                               UnitySolutionTracker unitySolutionTracker,
                               IThreading threading,
                               ILogger logger)
        {
            if (!solution.HasProtocolSolution())
                return;

            myBackendUnityHost = backendUnityHost;
            myUnityVersion = unityVersion;
            myUnitySolutionTracker = unitySolutionTracker;
            myThreading = threading;
            myLogger = logger;
            mySolution = solution;
            myLifetime = lifetime;
            myIndicatorManager = indicatorManager;
            myFrontendBackendModel = solution.GetProtocolSolution().GetFrontendBackendModel();
        }
        
        public bool IsUnityEditorUnitTestRunStrategy(IUnitTestRunStrategy strategy) 
            => strategy is RunViaUnityEditorStrategy;

        public Version GetUnityVersion() => myUnityVersion.ActualVersionForSolution.Value;

        public string GetPresentableUnityVersion() 
            => myFrontendBackendModel.UnityApplicationData.Value?.ApplicationVersion ?? string.Empty;
        
        public Task<int> StartUnity(Lifetime lifetime, Func<bool> condition = null)
        {
            lock (myStartUnitySync)
                return myStartUnityTask = myStartUnityTask
                    .ContinueWith(_ => StartUnityInternal(lifetime, condition), myLifetime, TaskContinuationOptions.None, myThreading.Tasks.Scheduler)
                    .Unwrap();
        }
        
        public Task StartProfiler(Lifetime lifetime, 
                                  FileSystemPath unityProfilerApiPath = null, 
                                  bool reloadUserAssemblies = true)
        {
            var unityModel = myBackendUnityHost.BackendUnityModel.Value;
            if (unityModel == null)
                return Task.FromException(new InvalidOperationException("Unity Editor is not connected."));

            var isPlayMode = myFrontendBackendModel.UnitTestPreference.Value is UnitTestLaunchPreference.PlayMode;
            var apiPath = GetProfilerApiPath();
            var data = new ProfilingData(isPlayMode, apiPath, reloadUserAssemblies);
            
            return myThreading.Tasks.StartNew(lifetime, Scheduling.MainDispatcher, () => unityModel.StartProfiling.Sync(data, ourUnityStartProfilingTimeouts));
        }
        
        public Task StopProfiler(Lifetime lifetime)
        {
            var unityModel = myBackendUnityHost.BackendUnityModel.Value;
            if (unityModel == null)
                return Task.FromException(new InvalidOperationException("Unity Editor is not connected."));

            var isPlayMode = myFrontendBackendModel.UnitTestPreference.Value is UnitTestLaunchPreference.PlayMode;
            var apiPath = GetProfilerApiPath();
            var data = new ProfilingData(isPlayMode, apiPath, false);
            
            return myThreading.Tasks.StartNew(lifetime, Scheduling.MainDispatcher, () => unityModel.StopProfiling.Sync(data, ourUnityStartProfilingTimeouts));
        }

        public bool IsUnitySolution() => myUnitySolutionTracker.IsUnityGeneratedProject.Maybe.ValueOrDefault;

        private VirtualFileSystemPath EditorInstanceJsonPath => mySolution.SolutionDirectory.Combine("Library/EditorInstance.json");

        private async Task<int> StartUnityInternal(Lifetime lifetime, Func<bool> condition = null)
        {
            if (TryGetUnityProcessId(out var unityPid) && 
                Process.GetProcessById(unityPid) is { HasExited:false } process)
                return process.Id;

            if (!(condition == null || condition()))
                return 0;

            using var startUnityDefinition = lifetime.CreateNested();
      
            var startUnityTask = StartUnityAndWaitConnection(startUnityDefinition.Lifetime);
      
            await myThreading.Tasks.YieldToIfNeeded(startUnityDefinition.Lifetime, Scheduling.MainGuard);

            myIndicatorManager.CreateBackgroundProgress(startUnityDefinition.Lifetime, Strings.UnityController_StartUnityInternal_Start_Unity_Editor, startUnityDefinition.Terminate);

            return await startUnityTask.ConfigureAwait(false);
        }
        
        private bool TryGetUnityProcessId(out int processId)
        {
            processId = 0;
            var applicationData = myBackendUnityHost.BackendUnityModel.Value?.UnityApplicationData.Value;
            if (applicationData is {UnityProcessId: { }})
            {
                processId = applicationData.UnityProcessId.Value;
                return true;
            }
            
            // no protocol connection - try to fallback to EditorInstance.json
            var processIdString = EditorInstanceJson.TryGetValue(EditorInstanceJsonPath, "process_id");
            if (processIdString == null)
                return false;

            // Check exists of process if it was killed by manual and EditorInstance.json wasn't deleted
            processId = Convert.ToInt32(processIdString);
            return ProcessUtil.IsProcessAlive(processId) == true;
        }

        private Task<int> StartUnityAndWaitConnection(Lifetime lifetime)
        {
            if (!TryGetUnityCommandline(out var unityPath, out var unityArgs))
                throw new InvalidOperationException("Cannot get command line to start Unity Editor.");
            
            var process = Process.Start(new ProcessStartInfo(unityPath, string.Join(" ", unityArgs)));
            if (process == null)
                throw new InvalidOperationException("Unity process hasn't started.");
            
            if (process.HasExited)
                throw new InvalidOperationException($"Unity process has been exited. {process.StandardError.ReadToEnd()}");

            return WaitConnectedUnityProcessId(lifetime);
        }
        
        private string GetProfilerApiPath()
        {
            const string etwAssemblyShorName = "JetBrains.Etw";
            var etwAssemblyLocation = AppDomain.CurrentDomain.GetAssemblies().FirstOrDefault(a => a.GetName().Name.Equals(etwAssemblyShorName))?.Location;
            if (etwAssemblyLocation == null)
            {
                myLogger.Error($"{etwAssemblyShorName} was not found.");
                return null;
            }
            var unityProfilerApiPath = FileSystemPath.Parse(etwAssemblyLocation).Parent
                .Combine("JetBrains.Etw.UnityProfilerApi.dll");
            if (!unityProfilerApiPath.ExistsFile)
            {
                myLogger.Error($"{unityProfilerApiPath} doesn't exist.");
                return null;
            }
            
            return unityProfilerApiPath.FullPath;
        }
        
        private Task<int> WaitConnectedUnityProcessId(Lifetime lifetime)
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
        
        private bool TryGetUnityCommandline(out string path, out string[] args)
        {
            path = string.Empty;
            args = EmptyArray<string>.Instance;
            
            var unityPathData = myFrontendBackendModel.UnityApplicationData;
            if (!unityPathData.HasValue())
                return false;
            
            var unityPath = unityPathData.Value?.ApplicationPath;
            if (unityPath != null && PlatformUtil.RuntimePlatform == JetPlatform.MacOsX)
                unityPath = VirtualFileSystemPath.Parse(unityPath, InteractionContext.SolutionContext).Combine("Contents/MacOS/Unity").FullPath;
            
            if (unityPath == null)
                return false;

            path = unityPath;
            args = new[] {"-projectPath", CommandLineUtil.QuoteIfNeeded(mySolution.SolutionDirectory.FullPath)};
            return true;
        }
    }
}