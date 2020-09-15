using System;
using System.Diagnostics;
using System.Linq;
using System.Threading.Tasks;
using JetBrains.Application.Components;
using JetBrains.Application.Threading;
using JetBrains.Application.Threading.Tasks;
using JetBrains.Lifetimes;
using JetBrains.ReSharper.Host.Features.Unity;
using JetBrains.ReSharper.TaskRunnerFramework;
using JetBrains.ReSharper.UnitTestFramework;
using JetBrains.ReSharper.UnitTestFramework.Launch;
using JetBrains.Util;

namespace JetBrains.ReSharper.Plugins.Unity.Rider.UnitTesting
{
    public class UnityTaskRunnerHostController : ITaskRunnerHostController
    {
        private const string PluginName = "Unity plugin";
        private const string NotAvailableUnityEditorMessage = "Unable to {0} tests: Unity Editor is not running";
        private const string StartUnityEditorQuestionMessage = "To {0} unit tests, you should first run Unity Editor. Do you want to Start Unity {1} now?";

        private readonly IUnityController myUnityController;
        private readonly IShellLocks myShellLocks;
        private readonly ITaskRunnerHostController myInnerHostController;
        private readonly object myStartUnitySync = new object();
        private Task myStartUnityTask;

        public UnityTaskRunnerHostController(ITaskRunnerHostController innerHostController,
                                             IShellLocks shellLocks,
                                             IUnityController unityController,
                                             string taskRunnerName)
        {
            myShellLocks = shellLocks ?? throw  new ArgumentNullException(nameof(shellLocks));
            myInnerHostController = innerHostController ?? throw  new ArgumentNullException(nameof(innerHostController));
            myUnityController = unityController ?? throw  new ArgumentNullException(nameof(unityController));
            myStartUnityTask = Task.CompletedTask;
            TaskRunnerName = taskRunnerName ?? throw new ArgumentNullException(nameof(taskRunnerName));
        }

        public string TaskRunnerName { get; }
        
        public void Dispose() => myInnerHostController.Dispose();

        public string HostId => myInnerHostController.HostId;

        public void SupplementContainer(ComponentContainer container) 
            => myInnerHostController.SupplementContainer(container);

        public Task AfterLaunchStarted() => myInnerHostController.AfterLaunchStarted();

        public Task BeforeLaunchFinished() => myInnerHostController.BeforeLaunchFinished();

        public ClientControllerInfo GetClientControllerInfo(IUnitTestRun run) 
            => myInnerHostController.GetClientControllerInfo(run);
        
        public Task CleanupAfterRun(IUnitTestRun run) => myInnerHostController.CleanupAfterRun(run);

        public void Cancel(IUnitTestRun run) => myInnerHostController.Cancel(run);

        public void Abort(IUnitTestRun run) => myInnerHostController.Abort(run);

        public IPreparedProcess StartProcess(ProcessStartInfo startInfo, IUnitTestRun run, ILogger logger) 
            => myInnerHostController.StartProcess(startInfo, run, logger);

        public void CustomizeConfiguration(IUnitTestRun run, TaskExecutorConfiguration configuration) 
            => myInnerHostController.CustomizeConfiguration(run, configuration);

        public async Task PrepareForRun(IUnitTestRun run)
        {
            // ToDo Replace this LifetimeDefinition with LifetimeDefinition from PrepareForRun (When it will be updated. It need to cancel PrepareForRun)
            var lifetimeDef = new LifetimeDefinition();
            
            await myInnerHostController.PrepareForRun(run).ConfigureAwait(false);

            if (!myUnityController.IsUnityEditorUnitTestRunStrategy(run.RunStrategy))
                return;

            lock (myStartUnitySync)
            {
                myStartUnityTask = myStartUnityTask.ContinueWith(_ =>
                {
                    var unityEditorProcessId = myUnityController.TryGetUnityProcessId();
                    return unityEditorProcessId.HasValue
                        ? Task.CompletedTask
                        : myShellLocks.Tasks.StartNew(lifetimeDef.Lifetime, Scheduling.FreeThreaded, StartUnityIfNeed);
                }, lifetimeDef.Lifetime, TaskContinuationOptions.None, myShellLocks.Tasks.GuardedMainThreadScheduler).Unwrap();
            }
            
            await myStartUnityTask.ConfigureAwait(false);
        }
        
        private Task StartUnityIfNeed()
        {
            var message = string.Format(StartUnityEditorQuestionMessage, 
                                              TaskRunnerName, 
                                              myUnityController.GetPresentableUnityVersion());
            var needStart = MessageBox.ShowYesNo(message, PluginName);
            if (!needStart)
                throw new Exception(string.Format(NotAvailableUnityEditorMessage, TaskRunnerName));

            var commandLines = myUnityController.GetUnityCommandline();
            var unityPath = commandLines.First();
            var unityArgs = string.Join(" ", commandLines.Skip(1));
            var process = new Process
            {
                StartInfo = new ProcessStartInfo(unityPath, unityArgs)
            };
            
            process.Start();
            
            return myUnityController.WaitConnectedUnityProcessId();
        }
    }
}