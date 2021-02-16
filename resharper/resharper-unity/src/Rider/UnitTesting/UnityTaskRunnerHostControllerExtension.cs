using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Threading.Tasks;
using JetBrains.Application.Threading;
using JetBrains.Application.Threading.Tasks;
using JetBrains.Diagnostics;
using JetBrains.Lifetimes;
using JetBrains.ProjectModel;
using JetBrains.ReSharper.Host.Features.BackgroundTasks;
using JetBrains.ReSharper.Host.Features.Unity;
using JetBrains.ReSharper.UnitTestFramework;
using JetBrains.ReSharper.UnitTestFramework.Launch;
using JetBrains.Util;

namespace JetBrains.ReSharper.Plugins.Unity.Rider.UnitTesting
{
    [SolutionComponent]
    public class UnityTaskRunnerHostControllerExtension : ITaskRunnerHostControllerExtension
    {
        private static readonly Key<LifetimeDefinition> ourLifetimeDefinitionKey = new Key<LifetimeDefinition>("UnityTaskRunnerHostController.CancelPrepareForRun");
        
        private const string PluginName = "Unity plugin";
        private const string NotAvailableUnityEditorMessage = "Unable to {0} tests: Unity Editor is not running";
        private const string StartUnityEditorQuestionMessage = "To {0} unit tests, you should first run Unity Editor. Do you want to Start Unity {1} now?";

        private readonly Lifetime myLifetime;
        private readonly IUnityController myUnityController;
        private readonly RiderBackgroundTaskHost myRiderBackgroundTaskHost;
        private readonly IShellLocks myShellLocks;
        private readonly IDictionary<string, string> myAvailableProviders;
        private readonly object myStartUnitySync = new object();
        private Task myStartUnityTask;

        public UnityTaskRunnerHostControllerExtension(Lifetime lifetime,
                                                      IShellLocks shellLocks,
                                                      IUnityController unityController,
                                                      RiderBackgroundTaskHost riderBackgroundTaskHost)
        {
            myLifetime = lifetime;
            myShellLocks = shellLocks.NotNull();
            myUnityController = unityController.NotNull();
            myRiderBackgroundTaskHost = riderBackgroundTaskHost;
            myStartUnityTask = Task.CompletedTask;
            myAvailableProviders = new Dictionary<string, string>
            { 
                { WellKnownHostProvidersIds.RunProviderId, "Run" },
                { WellKnownHostProvidersIds.DebugProviderId, "Debug" }
            };
        }

        public bool IsApplicable(IUnitTestRun run)
        {
            var isUnity = myUnityController.IsUnityEditorUnitTestRunStrategy(run.RunStrategy);
            return isUnity && myAvailableProviders.ContainsKey(run.HostController.HostId);
        }

        public ClientControllerInfo GetClientControllerInfo(IUnitTestRun run, ITaskRunnerHostController next) => null;

        public Task PrepareForRun(IUnitTestRun run, ITaskRunnerHostController next)
        {
            var lifetimeDef = myLifetime.CreateNested();
            run.PutData(ourLifetimeDefinitionKey, lifetimeDef);

            lock (myStartUnitySync)
            {
                WrapStartUnityTask(() => PrepareForRunInternal(lifetimeDef.Lifetime, run));
                WrapStartUnityTask(() => next.PrepareForRun(run));
                
                return myStartUnityTask;
            }
        }

        public Task CleanupAfterRun(IUnitTestRun run, ITaskRunnerHostController next) => next.CleanupAfterRun(run);

        public void Cancel(IUnitTestRun run) => run.GetData(ourLifetimeDefinitionKey)?.Terminate();

        private async Task PrepareForRunInternal(Lifetime lifetime, IUnitTestRun run)
        {
            var unityEditorProcessId = myUnityController.TryGetUnityProcessId();
            if (unityEditorProcessId.HasValue)
                return;

            var message = string.Format(StartUnityEditorQuestionMessage, 
                                             myAvailableProviders[run.HostController.HostId],
                                             myUnityController.GetPresentableUnityVersion());
            if (!MessageBox.ShowYesNo(message, PluginName))
                throw new Exception(string.Format(NotAvailableUnityEditorMessage, myAvailableProviders[run.HostController.HostId]));

            var startUnityTask = StartUnity(lifetime);
            
            await myShellLocks.Tasks.YieldToIfNeeded(lifetime, Scheduling.MainGuard);
            ShowProgress(lifetime, startUnityTask);
            
            await startUnityTask.ConfigureAwait(false);
        }
        
        private Task StartUnity(Lifetime lifetime)
        {
            var commandLines = myUnityController.GetUnityCommandline();
            var unityPath = commandLines.First();
            var unityArgs = string.Join(" ", commandLines.Skip(1));
            var process = new Process()
            {
                StartInfo = new ProcessStartInfo(unityPath, unityArgs)
            };
            
            process.Start();
            
            return myUnityController.WaitConnectedUnityProcessId(lifetime);
        }

        private void ShowProgress(Lifetime lifetime, Task task)
        {
            var innerLifeDef = lifetime.CreateNested();
            myRiderBackgroundTaskHost.CreateIndicator(innerLifeDef.Lifetime, false, false, "Start Unity Editor");
            task.ContinueWith(x =>   innerLifeDef.Terminate(), myLifetime, TaskContinuationOptions.None, myShellLocks.Tasks.Scheduler);
        }
        
        private void WrapStartUnityTask(Func<Task> run)
        {
            myStartUnityTask = myStartUnityTask.ContinueWith(_ => run(),
                                               myLifetime,
                                                             TaskContinuationOptions.None,
                                                             myShellLocks.Tasks.Scheduler).Unwrap();
        }
    }
}