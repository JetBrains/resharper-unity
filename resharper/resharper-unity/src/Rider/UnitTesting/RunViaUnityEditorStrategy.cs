using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using JetBrains.Annotations;
using JetBrains.Application.Threading;
using JetBrains.Application.Threading.Tasks;
using JetBrains.Collections.Viewable;
using JetBrains.Core;
using JetBrains.DataFlow;
using JetBrains.Diagnostics;
using JetBrains.Lifetimes;
using JetBrains.Metadata.Access;
using JetBrains.Platform.RdFramework.Util;
using JetBrains.Platform.Unity.EditorPluginModel;
using JetBrains.ProjectModel;
using JetBrains.ProjectModel.Features.SolutionBuilders.Prototype.Services.Execution;
using JetBrains.Rd.Base;
using JetBrains.ReSharper.Host.Features;
using JetBrains.ReSharper.Host.Features.UnitTesting;
using JetBrains.ReSharper.Plugins.Unity.Rider.Packages;
using JetBrains.ReSharper.Resources.Shell;
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
using JetBrains.Util.Threading;
using Status = JetBrains.Platform.Unity.EditorPluginModel.Status;
using UnitTestLaunch = JetBrains.Platform.Unity.EditorPluginModel.UnitTestLaunch;

namespace JetBrains.ReSharper.Plugins.Unity.Rider.UnitTesting
{
    [SolutionComponent]
    public class RunViaUnityEditorStrategy : IExternalRunnerUnitTestRunStrategy
    {
        private static readonly Key<CancellationTokenSource> ourCancellationTokenSourceKey =
            new Key<CancellationTokenSource>("RunViaUnityEditorStrategy.CancellationTokenSource");

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
        private readonly Lifetime myLifetime;
        private readonly PackageValidator myPackageValidator;
        private readonly ConnectionTracker myConnectionTracker;

        private static readonly Key<string> ourLaunchedInUnityKey = new Key<string>("LaunchedInUnityKey");
        private readonly WeakToWeakDictionary<UnitTestElementId, IUnitTestElement> myElements;

        private readonly object myCurrentLaunchesTaskAccess = new object();
        private Task myCurrentLaunchesTask = Task.CompletedTask;

        private readonly IProperty<int?> myUnityProcessId;

        public RunViaUnityEditorStrategy(ISolution solution,
            IUnitTestResultManager unitTestResultManager,
            UnityEditorProtocol editorProtocol,
            NUnitTestProvider unitTestProvider,
            IUnitTestElementIdFactory idFactory,
            ISolutionSaver riderSolutionSaver,
            UnityRefresher unityRefresher,
            NotificationsModel notificationsModel,
            UnityHost unityHost,
            ILogger logger,
            Lifetime lifetime,
            PackageValidator packageValidator,
            ConnectionTracker connectionTracker
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
            myLifetime = lifetime;
            myPackageValidator = packageValidator;
            myConnectionTracker = connectionTracker;
            myElements = new WeakToWeakDictionary<UnitTestElementId, IUnitTestElement>();

            myUnityProcessId = new Property<int?>(lifetime, "RunViaUnityEditorStrategy.UnityProcessId");

            myUnityProcessId.ForEachValue_NotNull(lifetime, (lt, processId) =>
            {
                var process = myLogger.CatchIgnore(() => Process.GetProcessById(processId.NotNull()));
                if (process == null)
                {
                    myUnityProcessId.Value = null;
                    return;
                }

                process.EnableRaisingEvents = true;

                void OnProcessExited(object sender, EventArgs a) => myUnityProcessId.Value = null;
                lt.Bracket(() => process.Exited += OnProcessExited, () => process.Exited -= OnProcessExited);

                if (process.HasExited)
                    myUnityProcessId.Value = null;
            });

            myEditorProtocol.UnityModel.ViewNotNull(lifetime, (lt, model) =>
            {
                if (model.UnityProcessId.HasValue())
                    myUnityProcessId.Value = model.UnityProcessId.Value;

                model.UnityProcessId.FlowInto(lt, myUnityProcessId, id => id);
            });
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
            lock (myCurrentLaunchesTaskAccess)
            {
                var key = run.Launch.GetData(ourLaunchedInUnityKey);
                if (key != null)
                {
                    return Task.FromResult(false);
                }

                run.Launch.PutData(ourLaunchedInUnityKey, "smth");

                var cancellationTs = new CancellationTokenSource();
                run.Lifetime.OnTermination(cancellationTs.Cancel);
                run.PutData(ourCancellationTokenSourceKey, cancellationTs);

                var newLaunchTask = myCurrentLaunchesTask.ContinueWith(_ => Run(run), cancellationTs.Token).Unwrap();
                myCurrentLaunchesTask = Task.WhenAll(myCurrentLaunchesTask, newLaunchTask);

                return newLaunchTask;
            }
        }

        private Task Run(IUnitTestRun run)
        {
            if (myUnityProcessId.Value == null)
                return Task.FromException(new Exception("Unity Editor is not available."));

            var tcs = new TaskCompletionSource<bool>();
            var taskLifetimeDef = Lifetime.Define(myLifetime);
            taskLifetimeDef.Lifetime.OnTermination(() => tcs.TrySetCanceled());
            tcs.Task.ContinueWith(_ => taskLifetimeDef.Terminate(), myLifetime);

            var hostId = run.HostController.HostId;
            switch (hostId)
            {
                case WellKnownHostProvidersIds.DebugProviderId:
                    mySolution.Locks.ExecuteOrQueueEx(myLifetime, "AttachDebuggerToUnityEditor", () =>
                    {
                        if (!run.Lifetime.IsAlive)
                        {
                            tcs.SetCanceled();
                            return;
                        }

                        var task = myUnityHost.GetValue(model => model.AttachDebuggerToUnityEditor.Start(Unit.Instance));
                        task.Result.AdviseNotNull(myLifetime, result =>
                        {
                            if (!run.Lifetime.IsAlive)
                                tcs.SetCanceled();
                            else if (!result.Result)
                                tcs.SetException(new Exception("Unable to attach debugger."));
                            else
                                RefreshAndRunTask(run, tcs, taskLifetimeDef.Lifetime);
                        });
                    });
                    break;

                default:
                    RefreshAndRunTask(run, tcs, taskLifetimeDef.Lifetime);
                    break;
            }

            return tcs.Task;
        }

        private void RefreshAndRunTask(IUnitTestRun run, TaskCompletionSource<bool> tcs, Lifetime taskLifetime)
        {
            var cancellationTs = run.GetData(ourCancellationTokenSourceKey);
            var cancellationToken = cancellationTs.NotNull().Token;

            myLogger.Trace("Before calling Refresh.");
            Refresh(run.Lifetime, cancellationToken).ContinueWith(__ =>
            {
                myLogger.Trace("Refresh. OnCompleted.");
                // KS: Can't use run.Lifetime for ExecuteOrQueueEx here and in all similar places: run.Lifetime is terminated when
                // Unit Test Session is closed from UI without cancelling the run. This will leave task completion source in running state forever.
                mySolution.Locks.ExecuteOrQueueEx(myLifetime, "Check compilation", () =>
                {
                    if (!run.Lifetime.IsAlive || cancellationTs.IsCancellationRequested)
                    {
                        tcs.SetCanceled();
                        return;
                    }

                    if (myEditorProtocol.UnityModel.Value == null)
                    {
                        myLogger.Verbose("Unity Editor connection unavailable.");
                        tcs.SetException(new Exception("Unity Editor connection unavailable."));
                        return;
                    }

                    var task = myEditorProtocol.UnityModel.Value.GetCompilationResult.Start(Unit.Instance);
                    task.Result.AdviseNotNull(myLifetime, result =>
                    {
                        if (!run.Lifetime.IsAlive || cancellationTs.IsCancellationRequested)
                            tcs.SetCanceled();
                        else if (!result.Result)
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
                            mySolution.Locks.ExecuteOrQueueEx(myLifetime, "ExecuteRunUT", () =>
                            {
                                if (!run.Lifetime.IsAlive || cancellationTs.IsCancellationRequested)
                                {
                                    tcs.SetCanceled();
                                    return;
                                }

                                if (myEditorProtocol.UnityModel.Value == null)
                                {
                                    tcs.SetException(new Exception("Unity Editor connection unavailable."));
                                    return;
                                }

                                myEditorProtocol.UnityModel.ViewNotNull(taskLifetime, (lt, model) =>
                                {
                                    // recreate UnitTestLaunch in case of AppDomain.Reload, which is the case with PlayMode tests
                                    model.UnitTestLaunch.SetValue(launch);
                                    SubscribeResults(run, lt, tcs, launch);
                                });

                                myUnityProcessId.When(taskLifetime, (int?)null, _ => tcs.TrySetException(new Exception("Unity Editor has been closed.")));

                                var rdTask = myEditorProtocol.UnityModel.Value.RunUnitTestLaunch.Start(Unit.Instance);
                                rdTask?.Result.Advise(taskLifetime, res =>
                                {
                                    myLogger.Trace($"RunUnitTestLaunch result = {res.Result}");
                                    if (!res.Result)
                                    {
                                        var defaultMessage = "Failed to start tests in Unity.";

                                        var isCoverage =
                                            run.HostController.HostId != WellKnownHostProvidersIds.DebugProviderId &&
                                            run.HostController.HostId != WellKnownHostProvidersIds.RunProviderId;

                                        if (myPackageValidator.HasNonCompatiblePackagesCombination(isCoverage, out var message))
                                            defaultMessage = $"{defaultMessage} {message}";

                                        if (myEditorProtocol.UnityModel.Value.UnitTestLaunch.Value.TestMode == TestMode.Play)
                                        {
                                            if (!myPackageValidator.CanRunPlayModeTests(out var playMessage))
                                                defaultMessage = $"{defaultMessage} {playMessage}";
                                        }

                                        tcs.TrySetException(new Exception(defaultMessage));
                                    }
                                });
                            });
                        }
                    });
                });
            }, cancellationToken);
        }

        private Task Refresh(Lifetime lifetime, CancellationToken cancellationToken)
        {
            return WaitForUnityEditorConnectedAndIdle(lifetime, cancellationToken).ContinueWith(_ =>
                {
                    return RefreshTask(lifetime, cancellationToken)
                        .ContinueWith(__ => 
                            WaitForUnityEditorConnectedAndIdle(lifetime, cancellationToken), cancellationToken)
                        .Unwrap();
                }, cancellationToken)
                .Unwrap();
        }

        private Task RefreshTask(Lifetime lifetime, CancellationToken cancellationToken)
        {
            var waitingLifetimeDefinition = Lifetime.Define(lifetime);
            cancellationToken.Register(() => waitingLifetimeDefinition.Terminate());
            mySolution.Locks.Tasks.StartNew(lifetime, Scheduling.MainDispatcher, () =>
            {
                if (cancellationToken.IsCancellationRequested)
                    return;

                myRiderSolutionSaver.Save(lifetime, mySolution, () =>
                {
                    myLogger.Trace("onSavedCallback.");
                    if (!cancellationToken.IsCancellationRequested)
                    {
                        myUnityRefresher.Refresh(waitingLifetimeDefinition.Lifetime, RefreshType.Force)
                            .ContinueWith(_ =>
                            {
                                myLogger.Trace("After onSavedCallback and myUnityRefresher.Refresh");
                                waitingLifetimeDefinition.Terminate();
                            }, cancellationToken);
                    }
                });
            });

            return JetTaskEx.While(() => waitingLifetimeDefinition.Lifetime.IsAlive);
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

            UnitTestLaunchClientControllerInfo unityClientControllerInfo = null;

            var clientControllerInfo = firstRun.HostController.GetClientControllerInfo(firstRun);
            if (clientControllerInfo != null)
                unityClientControllerInfo = new UnitTestLaunchClientControllerInfo(clientControllerInfo.ControllerType.Assembly.CodeBase,
                                                                                   clientControllerInfo.ExtraDependencies?.ToList(),
                                                                                   clientControllerInfo.ControllerType.FullName.NotNull());

            var launch = new UnitTestLaunch(firstRun.Launch.Session.Id, tests, emptyList, emptyList, mode, unityClientControllerInfo);
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
                        myUnitTestResultManager.TestStarting(unitTestElement, firstRun.Launch.Session);
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
                        myUnitTestResultManager.TestFinishing(unitTestElement, firstRun.Launch.Session, message, taskResult);
                        break;
                    default:
                        throw new ArgumentOutOfRangeException($"Unknown test result from the protocol: {result.Status}");
                }
            });

            launch.RunResult.Advise(connectionLifetime, result =>
            {
                tcs.SetResult(result.Passed);
            });
        }

        private Task WaitForUnityEditorConnectedAndIdle(Lifetime lifetime, CancellationToken cancellationToken)
        {
            myLogger.Trace("WaitForUnityEditorConnectedAndIdle");
            var tcs = new TaskCompletionSource<Unit>();
            var waitingLifetimeDef = Lifetime.Define(lifetime);
            var waitingLifetime = waitingLifetimeDef.Lifetime;
            waitingLifetime.OnTermination(() => tcs.TrySetCanceled());
            tcs.Task.ContinueWith(_ => waitingLifetimeDef.Terminate(), lifetime);
            if (cancellationToken.IsCancellationRequested) tcs.TrySetCanceled();
            
            // todo: Avoid pausing without focus
            waitingLifetime.StartMainUnguarded(() =>
            {
                if (myConnectionTracker.State.Value == UnityEditorState.Idle)
                {
                    tcs.TrySetResult(Unit.Instance);
                    return;
                }

                myConnectionTracker.State.Change.Advise_When(waitingLifetimeDef.Lifetime, UnityEditorState.Idle, () => tcs.TrySetResult(Unit.Instance));
                
                myUnityProcessId.When(waitingLifetime, (int?) null, _ => tcs.TrySetException(new Exception("Unity Editor has been closed.")));
                waitingLifetime.TryOnTermination(cancellationToken.Register(() => tcs.TrySetCanceled()));
            });

            return tcs.Task;
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
            mySolution.Locks.ExecuteOrQueueEx(myLifetime, "CancellingUnitTests", () =>
            {
                var launchProperty = myEditorProtocol.UnityModel.Value?.UnitTestLaunch;
                var launch = launchProperty?.Maybe.ValueOrDefault;
                if (launch != null && launch.SessionId == run.Launch.Session.Id)
                    launch.Abort.Start(Unit.Instance);
                run.GetData(ourCancellationTokenSourceKey).NotNull().Cancel();
            });
        }

        public void Abort(IUnitTestRun run)
        {
            Cancel(run);
        }

        public int? TryGetRunnerProcessId() => myUnityProcessId.Value;

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