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
        private const string NotAvailableUnityEditorMessage = "Unity Editor is not available";

        private readonly IUnityController myUnityController;
        private readonly IShellLocks myShellLocks;
        private readonly ITaskRunnerHostController myInnerHostController;
        private readonly object myStartUnitySync = new object();
        private Task myStartUnityTask;

        public UnityTaskRunnerHostController(ITaskRunnerHostController innerHostController,
                                             IShellLocks shellLocks,
                                             IUnityController unityController)
        {
            myShellLocks = shellLocks;
            myInnerHostController = innerHostController;
            myUnityController = unityController;
            myStartUnityTask = Task.CompletedTask;
        }

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
        
        public virtual Type[] GetSupplementaryContainerAttributeTypes() => null;

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
            var needStart = MessageBox.ShowYesNo("Unity Editor has not started yet. Run it?", "Unity plugin");
            if (!needStart)
                throw new Exception(NotAvailableUnityEditorMessage);

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