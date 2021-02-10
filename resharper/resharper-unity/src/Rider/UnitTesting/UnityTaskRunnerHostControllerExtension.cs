using System;
using System.Diagnostics;
using System.Linq;
using System.Threading.Tasks;
using JetBrains.Application.Threading;
using JetBrains.Application.Threading.Tasks;
using JetBrains.Diagnostics;
using JetBrains.Lifetimes;
using JetBrains.ProjectModel;
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

        private readonly IUnityController myUnityController;
        private readonly IShellLocks myShellLocks;
        private readonly string[] myAvailableProviders;
        private readonly object myStartUnitySync = new object();
        private Task myStartUnityTask;

        public UnityTaskRunnerHostControllerExtension(IShellLocks shellLocks,
                                                      IUnityController unityController,
                                                      RunHostProvider runHostProvider,
                                                      DebugHostProvider debugHostProvider)
        {
            myShellLocks = shellLocks.NotNull();
            myUnityController = unityController.NotNull();
            myAvailableProviders = new[] { runHostProvider.ID, debugHostProvider.ID };
            myStartUnityTask = Task.CompletedTask;
        }

        public bool IsApplicable(IUnitTestRun run)
        {
            var isUnity = myUnityController.IsUnityEditorUnitTestRunStrategy(run.RunStrategy);
            return isUnity && myAvailableProviders.Contains(run.HostController.HostId);
        }

        public ClientControllerInfo GetClientControllerInfo(IUnitTestRun run, ITaskRunnerHostController next) => null;

        public async Task PrepareForRun(IUnitTestRun run, ITaskRunnerHostController next)
        {
            var lifetimeDef = Lifetime.Define();
            run.PutData(ourLifetimeDefinitionKey, lifetimeDef);

            lock (myStartUnitySync)
            {
                myStartUnityTask = myStartUnityTask.ContinueWith(_ =>
                {
                    var unityEditorProcessId = myUnityController.TryGetUnityProcessId();
                    return unityEditorProcessId.HasValue
                        ? Task.CompletedTask
                        : myShellLocks.Tasks.StartNew(lifetimeDef.Lifetime, Scheduling.FreeThreaded, () => StartUnityIfNeed(lifetimeDef.Lifetime));
                }, lifetimeDef.Lifetime, TaskContinuationOptions.None, myShellLocks.Tasks.GuardedMainThreadScheduler).Unwrap();
            }
            
            await myStartUnityTask.ConfigureAwait(false);
        }

        public Task CleanupAfterRun(IUnitTestRun run, ITaskRunnerHostController next) => Task.CompletedTask;

        public void Cancel(IUnitTestRun run) => run.GetData(ourLifetimeDefinitionKey)?.Terminate();

        private Task StartUnityIfNeed(Lifetime lifetime)
        {
            var message = string.Format(StartUnityEditorQuestionMessage, 
                                              "run", 
                                              myUnityController.GetPresentableUnityVersion());
            var needStart = MessageBox.ShowYesNo(message, PluginName);
            if (!needStart)
                throw new Exception(string.Format(NotAvailableUnityEditorMessage, "run"));

            var commandLines = myUnityController.GetUnityCommandline();
            var unityPath = commandLines.First();
            var unityArgs = string.Join(" ", commandLines.Skip(1));
            var process = new Process
            {
                StartInfo = new ProcessStartInfo(unityPath, unityArgs)
            };
            
            process.Start();
            
            return myUnityController.WaitConnectedUnityProcessId(lifetime);
        }
    }
}