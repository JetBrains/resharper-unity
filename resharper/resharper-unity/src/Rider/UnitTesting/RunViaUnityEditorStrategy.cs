using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using JetBrains.Annotations;
using JetBrains.Application.Threading;
using JetBrains.Collections.Viewable;
using JetBrains.Core;
using JetBrains.DataFlow;
using JetBrains.Diagnostics;
using JetBrains.Lifetimes;
using JetBrains.Metadata.Access;
using JetBrains.Platform.RdFramework.Util;
using JetBrains.Rider.Model.Unity.BackendUnity;
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
using Status = JetBrains.Rider.Model.Unity.BackendUnity.Status;
using UnitTestLaunch = JetBrains.Rider.Model.Unity.BackendUnity.UnitTestLaunch;

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
        private static readonly Key<string> ourLaunchedInUnityKey = new Key<string>("LaunchedInUnityKey");

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
            PackageValidator packageValidator
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

            myEditorProtocol.BackendUnityModel.ViewNotNull(lifetime, (lt, model) =>
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

        public IRuntimeEnvironment GetRuntimeEnvironment(IUnitTestLaunch launch, IProject project,
            TargetFrameworkId targetFrameworkId,
            IUnitTestElement element)
        {
            var targetPlatform = TargetPlatformCalculator.GetTargetPlatform(launch, project, targetFrameworkId);
            return new UnityRuntimeEnvironment(targetPlatform, project);
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
            taskLifetimeDef.SynchronizeWith(tcs);

            myUnityProcessId.When(run.Lifetime, (int?) null,
                _ => { tcs.TrySetException(new Exception("Unity Editor has been closed.")); });

            var hostId = run.HostController.HostId;
            switch (hostId)
            {
                case WellKnownHostProvidersIds.DebugProviderId:
                    mySolution.Locks.ExecuteOrQueueEx(myLifetime, "AttachDebuggerToUnityEditor", () =>
                    {
                        if (!run.Lifetime.IsAlive)
                        {
                            tcs.TrySetCanceled();
                            return;
                        }

                        var task = myUnityHost.GetValue(model => model.AttachDebuggerToUnityEditor.Start(Unit.Instance));
                        task.Result.AdviseNotNull(myLifetime, result =>
                        {
                            if (!run.Lifetime.IsAlive)
                                tcs.TrySetCanceled();
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
            Refresh(run.Lifetime, tcs, cancellationToken).ContinueWith(__ =>
            {
                if (tcs.Task.IsCanceled || tcs.Task.IsFaulted) // Refresh failed or was stopped
                    return;

                myLogger.Trace("Refresh. OnCompleted.");
                // KS: Can't use run.Lifetime for ExecuteOrQueueEx here and in all similar places: run.Lifetime is terminated when
                // Unit Test Session is closed from UI without cancelling the run. This will leave task completion source in running state forever.
                mySolution.Locks.ExecuteOrQueueEx(myLifetime, "Check compilation", () =>
                {
                    if (!run.Lifetime.IsAlive || cancellationTs.IsCancellationRequested)
                    {
                        tcs.TrySetCanceled();
                        return;
                    }

                    var launch = SetupLaunch(run);

                    if (myEditorProtocol.BackendUnityModel.Value == null)
                    {
                        tcs.SetException(new Exception("Unity Editor connection unavailable."));
                        return;
                    }

                    myEditorProtocol.BackendUnityModel.ViewNotNull(taskLifetime, (lt, model) =>
                    {
                        // recreate UnitTestLaunch in case of AppDomain.Reload, which is the case with PlayMode tests
                        myLogger.Trace("UnitTestLaunch.SetValue.");
                        model.UnitTestLaunch.SetValue(launch);
                        SubscribeResults(run, lt, tcs, launch);
                    });

                    myLogger.Trace("RunUnitTestLaunch.Start.");
                    var rdTask = myEditorProtocol.BackendUnityModel.Value.RunUnitTestLaunch.Start(Unit.Instance);
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

                            if (myEditorProtocol.BackendUnityModel.Value.UnitTestLaunch.Value.TestMode == TestMode.Play)
                            {
                                if (!myPackageValidator.CanRunPlayModeTests(out var playMessage))
                                    defaultMessage = $"{defaultMessage} {playMessage}";
                            }

                            tcs.TrySetException(new Exception(defaultMessage));
                        }
                    });
                });
            }, cancellationToken);
        }

        private Task Refresh(Lifetime lifetime, TaskCompletionSource<bool> tcs, CancellationToken cancellationToken)
        {
            var refreshLifetimeDef = Lifetime.Define(lifetime);
            var refreshLifetime = refreshLifetimeDef.Lifetime;

            cancellationToken.Register(() =>
            {
                // On cancel we don't want to stop run, if tests are already running, but we want to stop, if we are waiting for refresh
                if (refreshLifetimeDef.Lifetime.IsAlive)
                {
                    tcs.TrySetCanceled();
                    refreshLifetimeDef.Terminate();
                }
            });

            WaitForUnityEditorConnectedAndIdle(refreshLifetime)
                .ContinueWith(_ =>
                {
                    return RefreshTask(refreshLifetime)
                        .ContinueWith(__ =>
                            WaitForUnityEditorConnectedAndIdle(refreshLifetime), refreshLifetime)
                        .Unwrap();
                }, refreshLifetime)
                .Unwrap()
                .ContinueWith(__ =>
                {
                    if (myEditorProtocol.BackendUnityModel.Value == null)
                    {
                        tcs.SetException(new Exception("Unity Editor connection unavailable."));
                        refreshLifetimeDef.Terminate();
                        return;
                    }

                    var task = myEditorProtocol.BackendUnityModel.Value.GetCompilationResult.Start(Unit.Instance);
                    task.Result.AdviseNotNull(refreshLifetime, result =>
                    {
                        if (result.Result)
                            refreshLifetimeDef.Terminate();
                        else
                        {
                            tcs.SetException(new Exception("There are errors during compilation in Unity."));

                            mySolution.Locks.ExecuteOrQueueEx(refreshLifetime,
                                "RunViaUnityEditorStrategy compilation failed",
                                () =>
                                {
                                    var notification = new NotificationModel("Compilation failed",
                                        "Script compilation in Unity failed, so tests were not started.", true,
                                        RdNotificationEntryType.INFO);
                                    myNotificationsModel.Notification(notification);
                                });
                            myUnityHost.PerformModelAction(model => model.ActivateUnityLogView());
                            refreshLifetimeDef.Terminate();
                        }
                    });
                }, refreshLifetime, TaskContinuationOptions.None, mySolution.Locks.Tasks.UnguardedMainThreadScheduler);

            return JetTaskEx.While(() => refreshLifetime.IsAlive);
        }

        private Task RefreshTask(Lifetime lifetime)
        {
            var waitingLifetimeDefinition = Lifetime.Define(lifetime);
            var waitingLifetime = waitingLifetimeDefinition.Lifetime;

            mySolution.Locks.ExecuteOrQueueEx(waitingLifetime, "myRiderSolutionSaver.Save", () =>
            {
                myRiderSolutionSaver.Save(waitingLifetime).ContinueWith(_ =>
                {
                    myLogger.Trace("After myRiderSolutionSaver.Save");
                    if (waitingLifetime.IsAlive)
                    {
                        myUnityRefresher.Refresh(waitingLifetime, RefreshType.Force)
                            .ContinueWith(__ =>
                            {
                                myLogger.Trace("After myUnityRefresher.Refresh");
                                waitingLifetimeDefinition.Terminate();
                            }, waitingLifetime);
                    }
                }, waitingLifetime, TaskContinuationOptions.None, mySolution.Locks.Tasks.UnguardedMainThreadScheduler);
            });

            return JetTaskEx.While(() => waitingLifetime.IsAlive);
        }

        private UnitTestLaunch SetupLaunch(IUnitTestRun firstRun)
        {
            var frontendBackendModel = mySolution.GetProtocolSolution().GetFrontendBackendModel();
            var filters = GetFilters(firstRun);

            var mode = TestMode.Edit;
            if (frontendBackendModel.UnitTestPreference.HasValue())
            {
                mode = frontendBackendModel.UnitTestPreference.Value == UnitTestLaunchPreference.PlayMode
                    ? TestMode.Play
                    : TestMode.Edit;
            }

            UnitTestLaunchClientControllerInfo unityClientControllerInfo = null;

            var clientControllerInfo = firstRun.HostController.GetClientControllerInfo(firstRun);
            if (clientControllerInfo != null)
                unityClientControllerInfo = new UnitTestLaunchClientControllerInfo(
                    clientControllerInfo.AssemblyLocation,
                    clientControllerInfo.ExtraDependencies?.ToList(),
                    clientControllerInfo.TypeName);

            var launch = new UnitTestLaunch(firstRun.Launch.Session.Id, filters, mode, unityClientControllerInfo);
            return launch;
        }

        private void SubscribeResults(IUnitTestRun firstRun, Lifetime connectionLifetime, TaskCompletionSource<bool> tcs, UnitTestLaunch launch)
        {
            mySolution.Locks.AssertMainThread();

            launch.TestResult.AdviseNotNull(connectionLifetime, result =>
            {
                var unitTestElement = GetElementById(firstRun, result.ProjectName, result.TestId);
                if (unitTestElement == null)
                {
                    // add dynamic tests
                    var parent = GetElementById(firstRun, result.ProjectName, result.ParentId);
                    if (parent is NUnitTestElement elementParent)
                    {
                        var project = elementParent.Id.Project;
                        var targetFrameworkId = elementParent.Id.TargetFrameworkId;
                        var uid = myIDFactory.Create(myUnitTestProvider, project, targetFrameworkId, result.TestId);
                        unitTestElement = new NUnitRowTestElement(mySolution.GetComponent<NUnitServiceProvider>(), uid,
                            elementParent, elementParent.TypeName.GetPersistent());
                        firstRun.AddDynamicElement(unitTestElement);
                    }
                    else if (parent is NUnitTestFixtureElement fixtureParent)
                    {
                        var project = fixtureParent.Id.Project;
                        var targetFrameworkId = fixtureParent.Id.TargetFrameworkId;
                        var uid = myIDFactory.Create(myUnitTestProvider, project, targetFrameworkId, result.TestId);
                        unitTestElement = new NUnitTestElement(mySolution.GetComponent<NUnitServiceProvider>(), uid,
                            fixtureParent, fixtureParent.TypeName.GetPersistent(), result.TestId);

                        firstRun.AddDynamicElement(unitTestElement);
                    }
                }

                if (unitTestElement == null)
                    return;

                switch (result.Status)
                {
                    case Status.Pending:
                        myUnitTestResultManager.MarkPending(unitTestElement, firstRun.Launch.Session);
                        break;
                    case Status.Running:
                        myUnitTestResultManager.TestStarting(unitTestElement, firstRun.Launch.Session);
                        break;
                    case Status.Success:
                    case Status.Failure:
                    case Status.Ignored:
                    case Status.Inconclusive:
                        string message = result.Status.ToString();
                        var taskResult = UnitTestStatus.Inconclusive;
                        if (result.Status == Status.Failure)
                            taskResult = UnitTestStatus.Failed;
                        else if (result.Status == Status.Ignored)
                            taskResult = UnitTestStatus.Aborted;
                        else if (result.Status == Status.Success)
                            taskResult = UnitTestStatus.Success;

                        myUnitTestResultManager.TestOutput(unitTestElement, firstRun.Launch.Session, result.Output, TaskOutputType.STDOUT);
                        myUnitTestResultManager.TestDuration(unitTestElement, firstRun.Launch.Session, TimeSpan.FromMilliseconds(result.Duration));
                        myUnitTestResultManager.TestFinishing(unitTestElement, firstRun.Launch.Session, message, taskResult);
                        break;
                    default:
                        throw new ArgumentOutOfRangeException($"Unknown test result from the protocol: {result.Status}");
                }
            });

            launch.RunResult.Advise(connectionLifetime, result => { tcs.SetResult(result.Passed); });
        }

        private Task WaitForUnityEditorConnectedAndIdle(Lifetime lifetime)
        {
            myLogger.Trace("WaitForUnityEditorConnectedAndIdle");

            var waitingLifetimeDef = Lifetime.Define(lifetime);
            var waitingLifetime = waitingLifetimeDef.Lifetime;

            waitingLifetime.StartMainUnguarded(() =>
            {
                myEditorProtocol.UnityWire.Advise(waitingLifetime, wire =>
                {
                    wire.HeartbeatAlive.Advise(waitingLifetime, res =>
                    {
                        if (res)
                        {
                            myEditorProtocol.BackendUnityModel.Advise(waitingLifetime, model =>
                            {
                                if (model != null)
                                    waitingLifetimeDef.Terminate();
                            });
                        }
                    });
                });
            });

            return JetTaskEx.While(() => waitingLifetime.IsAlive);
        }

        private List<TestFilter> GetFilters(IUnitTestRun run)
        {
            var filters = new List<TestFilter>();

            var unitTestElements = new JetHashSet<IUnitTestElement>();
            unitTestElements.AddRange(run.Elements);
            var elements = unitTestElements
                .Where(unitTestElement => unitTestElement is NUnitTestElement ||
                                          unitTestElement is NUnitRowTestElement).ToArray();

            var testNames = elements.Where(a => !a.Explicit)
                .Union(run.Launch.Criterion.Explicit)
                .Select(p => p.Id.Id).ToList();

            var groups = new List<string>();
            var categories = new List<string>();
            // https://github.com/JetBrains/resharper-unity/pull/1801#discussion_r472383244
/*var criterion = run.Launch.Criterion.Criterion;
if (criterion is ConjunctiveCriterion conjunctiveCriterion)
{
   groups.AddRange(conjunctiveCriterion.Criteria.Where(a => a is TestAncestorCriterion).SelectMany(b =>
       ((TestAncestorCriterion) b).AncestorIds.Select(a => $"^{Regex.Escape(a.Id)}$")));
   categories.AddRange(conjunctiveCriterion.Criteria.Where(a => a is CategoryCriterion).Select(b =>
       ((CategoryCriterion) b).Category.Name));
}
else if (criterion is TestAncestorCriterion ancestorCriterion)
   groups.AddRange(ancestorCriterion.AncestorIds.Select(a => $"^{Regex.Escape(a.Id)}$"));
else if (criterion is CategoryCriterion categoryCriterion)
   categories.Add(categoryCriterion.Category.Name);*/

            filters.Add(new TestFilter(((UnityRuntimeEnvironment) run.RuntimeEnvironment).Project.Name, testNames, groups, categories));
            return filters;
        }

        [CanBeNull]
        private IUnitTestElement GetElementById(IUnitTestRun run, string projectName, string resultTestId)
        {
            return run.Elements.SingleOrDefault(a => a.Id.Project.Name == projectName && resultTestId == a.Id.Id);
        }

        public void Cancel(IUnitTestRun run)
        {
            mySolution.Locks.ExecuteOrQueueEx(run.Lifetime, "CancellingUnitTests", () =>
            {
                var launchProperty = myEditorProtocol.BackendUnityModel.Value?.UnitTestLaunch;
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
            public UnityRuntimeEnvironment(TargetPlatform targetPlatform, IProject project)
            {
                TargetPlatform = targetPlatform;
                Project = project;
            }

            public TargetPlatform TargetPlatform { get; }
            public IProject Project { get; }

            private bool Equals(UnityRuntimeEnvironment other)
            {
                return TargetPlatform == other.TargetPlatform && Equals(Project, other.Project);
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