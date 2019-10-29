using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using JetBrains.Annotations;
using JetBrains.Application.Threading;
using JetBrains.Application.Threading.Tasks;
using JetBrains.Collections.Viewable;
using JetBrains.Core;
using JetBrains.Diagnostics;
using JetBrains.Lifetimes;
using JetBrains.Metadata.Access;
using JetBrains.Platform.Unity.EditorPluginModel;
using JetBrains.ProjectModel;
using JetBrains.ProjectModel.Features.SolutionBuilders.Prototype.Services.Execution;
using JetBrains.Rd.Base;
using JetBrains.ReSharper.Host.Features;
using JetBrains.ReSharper.TaskRunnerFramework;
using JetBrains.ReSharper.UnitTestFramework;
using JetBrains.ReSharper.UnitTestFramework.Launch;
using JetBrains.ReSharper.UnitTestFramework.Strategy;
using JetBrains.ReSharper.UnitTestProvider.nUnit.v30;
using JetBrains.ReSharper.UnitTestProvider.nUnit.v30.Elements;
using JetBrains.Rider.Model;
using JetBrains.Rider.Model.Notifications;
using JetBrains.Util;
using JetBrains.Util.Dotnet.TargetFrameworkIds;
using Status = JetBrains.Platform.Unity.EditorPluginModel.Status;
using UnitTestLaunch = JetBrains.Platform.Unity.EditorPluginModel.UnitTestLaunch;

namespace JetBrains.ReSharper.Plugins.Unity.Rider.UnitTesting
{
    [SolutionComponent]
    public class RunViaUnityEditorStrategy : IUnitTestRunStrategy
    {
        private static readonly Key<TaskCompletionSource<bool>> ourCompletionSourceKey =
            new Key<TaskCompletionSource<bool>>("RunViaUnityEditorStrategy.TaskCompletionSource");
        
        private readonly ISolution mySolution;
        private readonly IUnitTestResultManager myUnitTestResultManager;
        private readonly UnityEditorProtocol myEditorProtocol;
        private readonly NUnitTestProvider myUnitTestProvider;
        private readonly IUnitTestElementIdFactory myIDFactory;
        private readonly ISolutionSaver myRiderSolutionSaver;
        private readonly UnityRefresher myUnityRefresher;
        private readonly NotificationsModel myNotificationsModel;
        private readonly UnityHost myUnityHost;
        private readonly ILogger myLogger;

        private static readonly Key<string> ourLaunchedInUnityKey = new Key<string>("LaunchedInUnityKey");
        private readonly WeakToWeakDictionary<UnitTestElementId, IUnitTestElement> myElements;

        public RunViaUnityEditorStrategy(ISolution solution,
            IUnitTestResultManager unitTestResultManager, 
            UnityEditorProtocol editorProtocol,
            NUnitTestProvider unitTestProvider, 
            IUnitTestElementIdFactory idFactory,
            ISolutionSaver riderSolutionSaver,
            UnityRefresher unityRefresher,
            NotificationsModel notificationsModel,
            UnityHost unityHost,
            ILogger logger
            )
        {
            mySolution = solution;
            myUnitTestResultManager = unitTestResultManager;
            myEditorProtocol = editorProtocol;
            myUnitTestProvider = unitTestProvider;
            myIDFactory = idFactory;
            myRiderSolutionSaver = riderSolutionSaver;
            myUnityRefresher = unityRefresher;
            myNotificationsModel = notificationsModel;
            myUnityHost = unityHost;
            myLogger = logger;
            myElements = new WeakToWeakDictionary<UnitTestElementId, IUnitTestElement>();
        }

        public bool RequiresProjectBuild(IProject project)
        {
            return false;
        }

        public bool RequiresProjectExplorationAfterBuild(IProject project)
        {
            return false;
        }

        public bool RequiresProjectPropertiesRefreshBeforeLaunch()
        {
            return false;
        }

        public IRuntimeEnvironment GetRuntimeEnvironment(IUnitTestLaunch launch, IProject project, TargetFrameworkId targetFrameworkId,
            IUnitTestElement element)
        {
            var targetPlatform = TargetPlatformCalculator.GetTargetPlatform(launch, project, targetFrameworkId);
            return new UnityRuntimeEnvironment(targetPlatform);
        }

        Task IUnitTestRunStrategy.Run(IUnitTestRun run)
        {
            var key = run.Launch.GetData(ourLaunchedInUnityKey);
            if (key != null)
            {
                return Task.FromResult(false);
            }

            var tcs = new TaskCompletionSource<bool>();
            run.Launch.PutData(ourLaunchedInUnityKey, "smth");
            run.PutData(ourCompletionSourceKey, tcs);

            var hostId = run.HostController.HostId;
            switch (hostId)
            {
                case WellKnownHostProvidersIds.DebugProviderId:
                    mySolution.Locks.ExecuteOrQueueEx(run.Lifetime, "AttachDebuggerToUnityEditor", () =>
                    {
                        var task = myUnityHost.GetValue(model =>
                            model.AttachDebuggerToUnityEditor.Start(Unit.Instance));
                        task.Result.AdviseNotNull(run.Lifetime, result =>
                        {
                            if (!result.Result)
                                tcs.SetException(new Exception("Unable to attach debugger."));
                            else
                                RefreshAndRunTask(run, tcs);
                        });
                    });
                    break;
                case WellKnownHostProvidersIds.RunProviderId:
                    RefreshAndRunTask(run, tcs);
                    break;
                default:
                    run.Launch.Output.Error(
                        $"Starting Unity tests from '{hostId}' is currently unsupported. Please use `Run`.");
                    return Task.FromResult(false);
            }

            return tcs.Task;
        }

        private void RefreshAndRunTask(IUnitTestRun run, TaskCompletionSource<bool> tcs)
        {
            myLogger.Verbose("Before calling Refresh.");
            Refresh(mySolution.Locks, run.Lifetime).GetAwaiter().OnCompleted(() =>
            {
                mySolution.Locks.ExecuteOrQueueEx(run.Lifetime, "Check compilation", () =>
                {
                    if (myEditorProtocol.UnityModel.Value == null)
                    {
                        myLogger.Verbose("Unity Editor connection unavailable.");
                        tcs.SetException(new Exception("Unity Editor connection unavailable."));
                        return;
                    }

                    var task = myEditorProtocol.UnityModel.Value.GetCompilationResult.Start(Unit.Instance);
                    task.Result.AdviseNotNull(run.Lifetime, result =>
                    {
                        if (!result.Result)
                        {
                            tcs.SetException(new Exception("There are errors during compilation in Unity."));

                            mySolution.Locks.ExecuteOrQueueEx(run.Lifetime, "RunViaUnityEditorStrategy compilation failed",
                                () =>
                                {
                                    var notification = new NotificationModel("Compilation failed",
                                        "Script compilation in Unity failed, so tests were not started.", true,
                                        RdNotificationEntryType.INFO);
                                    myNotificationsModel.Notification(notification);
                                });
                            myUnityHost.PerformModelAction(model => model.ActivateUnityLogView());
                        }
                        else
                        {
                            var launch = SetupLaunch(run);
                            mySolution.Locks.ExecuteOrQueueEx(run.Lifetime, "ExecuteRunUT", () =>
                            {
                                if (myEditorProtocol.UnityModel.Value == null)
                                {
                                    tcs.SetException(new Exception("Unity Editor connection unavailable."));
                                    return;
                                }

                                myEditorProtocol.UnityModel.ViewNotNull(run.Lifetime, (lt, model) =>
                                {
                                    // recreate UnitTestLaunch in case of AppDomain.Reload, which is the case with PlayMode tests
                                    model.UnitTestLaunch.SetValue(launch);
                                    SubscribeResults(run, lt, tcs, launch);
                                });

                                myEditorProtocol.UnityModel.Value.RunUnitTestLaunch();
                            });
                        }
                    });
                });
            });
        }

        private async Task Refresh(IShellLocks locks, Lifetime lifetime)
        {
            var refreshTask = locks.Tasks.StartNew(lifetime, Scheduling.MainDispatcher, async () =>
            {
                var lifetimeDef = lifetime.CreateNested();
                myRiderSolutionSaver.Save(lifetime, mySolution, () =>
                {
                    myUnityRefresher.Refresh(RefreshType.Force);
                    lifetimeDef.Terminate();
                });
                while (lifetimeDef.Lifetime.IsAlive)
                {
                    await TaskEx.Delay(TimeSpan.FromMilliseconds(10), lifetimeDef.Lifetime);
                }
            });
            
            var lifetimeDefinition = lifetime.CreateNested();
            await refreshTask.ContinueWith(task =>
            {
                mySolution.Locks.QueueRecurring(lifetimeDefinition.Lifetime,
                    "Periodic wait EditorState != UnityEditorState.Refresh",
                    TimeSpan.FromSeconds(1), () =>
                    {
                        if (myEditorProtocol.UnityModel.Value == null) return;
                        var rdTask = myEditorProtocol.UnityModel.Value.GetUnityEditorState.Start(Unit.Instance);
                        rdTask?.Result.Advise(lifetime, result =>
                        {
                            // [TODO] Backend ConnectionTracker has IsConnectionEstablished method which has same logic
                            if (result.Result != UnityEditorState.Refresh && result.Result != UnityEditorState.Disconnected)
                            {
                                lifetimeDefinition.Terminate();
                                myLogger.Verbose("lifetimeDefinition.Terminate();");
                            }
                        });
                    });
            }, locks.Tasks.UnguardedMainThreadScheduler);
                
            while (lifetimeDefinition.Lifetime.IsAlive)
            {
                await TaskEx.Delay(TimeSpan.FromMilliseconds(50), lifetimeDefinition.Lifetime);
            }
        }

        private UnitTestLaunch SetupLaunch(IUnitTestRun firstRun)
        {
            var rdUnityModel = mySolution.GetProtocolSolution().GetRdUnityModel();
            
            var unitTestElements = CollectElementsToRunInUnityEditor(firstRun);
            var tests = InitElementsMap(unitTestElements);
            var emptyList = new List<string>();

            var mode = TestMode.Edit;
            if (rdUnityModel.UnitTestPreference.HasValue())
            {
                mode = rdUnityModel.UnitTestPreference.Value == UnitTestLaunchPreference.PlayMode
                    ? TestMode.Play
                    : TestMode.Edit;    
            }
              
            var launch = new UnitTestLaunch(tests, emptyList, emptyList, mode);
            return launch;
        }
        
        private void SubscribeResults(IUnitTestRun firstRun, Lifetime connectionLifetime, TaskCompletionSource<bool> tcs, UnitTestLaunch launch)
        {
            mySolution.Locks.AssertMainThread();
        
            launch.TestResult.AdviseNotNull(connectionLifetime, result =>
            {
                var unitTestElement = GetElementById(result.ProjectName, result.TestId);
                if (unitTestElement == null)
                {
                    // add dynamic tests
                    var parent = GetElementById(result.ProjectName, result.ParentId) as NUnitTestElement;
                    if (parent == null)
                        return;

                    var project = parent.Id.Project;
                    var targetFrameworkId = parent.Id.TargetFrameworkId;
                    
                    var uid = myIDFactory.Create(myUnitTestProvider, project, targetFrameworkId, result.TestId);
                    unitTestElement = new NUnitRowTestElement(mySolution.GetComponent<NUnitServiceProvider>(), uid, parent, parent.TypeName.GetPersistent());
                    firstRun.AddDynamicElement(unitTestElement);
                    myElements.Add(myIDFactory.Create(myUnitTestProvider, project, targetFrameworkId, result.TestId), unitTestElement);
                }
                
                switch (result.Status)
                {
                    case Status.Pending:
                        myUnitTestResultManager.MarkPending(unitTestElement,
                            firstRun.Launch.Session);
                        break;
                    case Status.Running:
                        myUnitTestResultManager.TestStarting(unitTestElement,
                            firstRun.Launch.Session);
                        break;
                    case Status.Success:
                    case Status.Failure:
                    case Status.Ignored:
                    case Status.Inconclusive:
                        string message = result.Status.ToString();
                        TaskResult taskResult = TaskResult.Inconclusive;
                        if (result.Status == Status.Failure)
                            taskResult = TaskResult.Error;
                        else if (result.Status == Status.Ignored)
                            taskResult = TaskResult.Skipped;
                        else if (result.Status == Status.Success)
                            taskResult = TaskResult.Success;
                            
                        myUnitTestResultManager.TestOutput(unitTestElement, firstRun.Launch.Session, result.Output, TaskOutputType.STDOUT);
                        myUnitTestResultManager.TestDuration(unitTestElement, firstRun.Launch.Session, TimeSpan.FromMilliseconds(result.Duration));
                        myUnitTestResultManager.TestFinishing(unitTestElement,
                            firstRun.Launch.Session, message, taskResult);
                        break;
                    default:
                        throw new ArgumentOutOfRangeException(
                            $"Unknown test result from the protocol: {result.Status}");
                }
            });
            
            launch.RunResult.Advise(connectionLifetime, result =>
            {
                tcs.SetResult(true);
            });
        }


        private IEnumerable<IUnitTestElement> CollectElementsToRunInUnityEditor(IUnitTestRun firstRun)
        {
            var result = new JetHashSet<IUnitTestElement>();
            foreach (var unitTestRun in firstRun.Launch.Runs)
            {
                if (unitTestRun.RunStrategy.Equals(this))
                {
                    result.AddRange(unitTestRun.Elements);
                }
            }

            return result.ToList();
        }

        private List<TestFilter> InitElementsMap(IEnumerable<IUnitTestElement> unitTestElements)
        {
            var elements = unitTestElements
                .Where(unitTestElement => unitTestElement is NUnitTestElement || 
                                          unitTestElement is NUnitRowTestElement).ToArray();
            foreach (var unitTestElement in elements)
            {
                myElements[unitTestElement.Id] = unitTestElement;
            }    
            var filters = elements
                .GroupBy(
                p => p.Id.Project.Name, 
                p => p.Id.Id,
                (key, g) => new TestFilter(key, g.ToList()));
            
            return filters.ToList();
        }

        [CanBeNull]
        private IUnitTestElement GetElementById(string projectName, string resultTestId)
        {
            var unitTestElement = myElements.Where(a=>a.Key.Id == resultTestId && a.Key.Project.Name == projectName).Select(b=>b.Value).SingleOrDefault();
            return unitTestElement;
        }

        public void Cancel(IUnitTestRun run)
        {
            mySolution.Locks.ExecuteOrQueueEx(run.Lifetime, "CancellingUnitTests", () =>
            {
                var launchProperty = myEditorProtocol.UnityModel.Value?.UnitTestLaunch;
                if (launchProperty != null && launchProperty.HasValue())
                    launchProperty.Value?.Abort.Start(Unit.Instance);
                run.GetData(ourCompletionSourceKey).NotNull().SetCanceled();
            });
        }

        public void Abort(IUnitTestRun run)
        {
            Cancel(run);
        }

        private class UnityRuntimeEnvironment : IRuntimeEnvironment
        {
            public UnityRuntimeEnvironment(TargetPlatform targetPlatform)
            {
                TargetPlatform = targetPlatform;
            }

            public TargetPlatform TargetPlatform { get; }

            private bool Equals(UnityRuntimeEnvironment other)
            {
                return TargetPlatform == other.TargetPlatform;
            }

            public override bool Equals(object obj)
            {
                if (ReferenceEquals(null, obj)) return false;
                if (ReferenceEquals(this, obj)) return true;
                if (obj.GetType() != GetType()) return false;
                return Equals((UnityRuntimeEnvironment) obj);
            }

            public override int GetHashCode()
            {
                return (int) TargetPlatform;
            }

            public static bool operator ==(UnityRuntimeEnvironment left, UnityRuntimeEnvironment right)
            {
                return Equals(left, right);
            }

            public static bool operator !=(UnityRuntimeEnvironment left, UnityRuntimeEnvironment right)
            {
                return !Equals(left, right);
            }
        }
    }
}