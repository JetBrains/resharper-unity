#nullable enable

using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using JetBrains.Application.Parts;
using JetBrains.Application.Threading;
using JetBrains.Collections.Viewable;
using JetBrains.Core;
using JetBrains.DataFlow;
using JetBrains.Diagnostics;
using JetBrains.Lifetimes;
using JetBrains.Metadata.Access;
using JetBrains.ProjectModel;
using JetBrains.ProjectModel.Features.SolutionBuilders.Prototype.Services.Execution;
using JetBrains.Rd.Base;
using JetBrains.ReSharper.Feature.Services.Protocol;
using JetBrains.ReSharper.Plugins.Unity.Rider.Integration.Protocol;
using JetBrains.ReSharper.Plugins.Unity.Rider.Integration.UnityEditorIntegration;
using JetBrains.ReSharper.Plugins.Unity.Rider.Integration.UnityEditorIntegration.Packages;
using JetBrains.ReSharper.Resources.Shell;
using JetBrains.ReSharper.UnitTestFramework.Elements;
using JetBrains.ReSharper.UnitTestFramework.Execution;
using JetBrains.ReSharper.UnitTestFramework.Execution.Hosting;
using JetBrains.ReSharper.UnitTestFramework.Execution.Launch;
using JetBrains.ReSharper.UnitTestProvider.nUnit.v30.Elements;
using JetBrains.Rider.Backend.Features.UnitTesting;
using JetBrains.Rider.Model.Notifications;
using JetBrains.Rider.Model.Unity.BackendUnity;
using JetBrains.Rider.Model.Unity.FrontendBackend;
using JetBrains.Util;
using JetBrains.Util.Dotnet.TargetFrameworkIds;
using JetBrains.Util.Extension;
using JetBrains.Util.Threading;
using IUnitTestLaunch = JetBrains.ReSharper.UnitTestFramework.Execution.Launch.IUnitTestLaunch;
using IUnitTestRun = JetBrains.ReSharper.UnitTestFramework.Execution.Launch.IUnitTestRun;
using Status = JetBrains.Rider.Model.Unity.BackendUnity.Status;
using UnitTestLaunch = JetBrains.Rider.Model.Unity.BackendUnity.UnitTestLaunch;

namespace JetBrains.ReSharper.Plugins.Unity.Rider.Integration.Core.Feature.UnitTesting
{
    [SolutionComponent(Instantiation.DemandAnyThread)]
    public class RunViaUnityEditorStrategy : IExternalRunnerUnitTestRunStrategy
    {
        private static readonly Key<CancellationTokenSource> ourCancellationTokenSourceKey =
            new("RunViaUnityEditorStrategy.CancellationTokenSource");

        private readonly ISolution mySolution;
        private readonly IUnitTestResultManager myUnitTestResultManager;
        private readonly BackendUnityHost myBackendUnityHost;
        private readonly ISolutionSaver myRiderSolutionSaver;
        private readonly UnityRefresher myUnityRefresher;
        private readonly NotificationsModel myNotificationsModel;
        private readonly FrontendBackendHost myFrontendBackendHost;
        private readonly ILogger myLogger;
        private readonly Lifetime myLifetime;
        private readonly UnityProcessTracker myProcessTracker;
        private readonly PackageCompatibilityValidator myPackageCompatibilityValidator;

        private readonly object myCurrentLaunchesTaskAccess = new();
        private Task myCurrentLaunchesTask = Task.CompletedTask;

        public RunViaUnityEditorStrategy(ISolution solution,
                                         IUnitTestResultManager unitTestResultManager,
                                         BackendUnityHost backendUnityHost,
                                         ISolutionSaver riderSolutionSaver,
                                         UnityRefresher unityRefresher,
                                         NotificationsModel notificationsModel,
                                         FrontendBackendHost frontendBackendHost,
                                         ILogger logger,
                                         Lifetime lifetime,
                                         UnityProcessTracker processTracker,
                                         PackageCompatibilityValidator packageCompatibilityValidator)
        {
            mySolution = solution;
            myUnitTestResultManager = unitTestResultManager;
            myBackendUnityHost = backendUnityHost;
            myRiderSolutionSaver = riderSolutionSaver;
            myUnityRefresher = unityRefresher;
            myNotificationsModel = notificationsModel;
            myFrontendBackendHost = frontendBackendHost;
            myLogger = logger;
            myLifetime = lifetime;
            myProcessTracker = processTracker;
            myPackageCompatibilityValidator = packageCompatibilityValidator;

        }

        public bool RequiresProjectBuild(IProject project) => false;
        public bool RequiresProjectExplorationAfterBuild(IProject project) => false;
        public IProject? GetProjectForPropertiesRefreshBeforeLaunch(IUnitTestElement element) => null;

        public IRuntimeDescriptor GetRuntimeDescriptor(IUnitTestLaunch launch, IUnitTestElement element)
        {
            var targetPlatform = TargetPlatformCalculator.GetTargetPlatform(launch, element.Project, element.TargetFrameworkId);
            return new UnityRuntimeEnvironment(targetPlatform, element.Project);
        }

        Task IUnitTestRunStrategy.Run(IUnitTestRun run)
        {
            lock (myCurrentLaunchesTaskAccess)
            {
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
            if (myProcessTracker.UnityProcessId.Value == null)
                return Task.FromException(new Exception("Unity Editor is not available."));

            var tcs = new TaskCompletionSource<bool>();
            var taskLifetimeDef = Lifetime.Define(myLifetime);
            taskLifetimeDef.SynchronizeWith(tcs);

            myProcessTracker.UnityProcessId.When(run.Lifetime, (int?) null,
                _ => { tcs.TrySetException(new Exception("Unity Editor has been closed.")); });

            var hostId = run.HostController.HostId;
            switch (hostId)
            {
                case WellKnownHostProvidersIds.DebugProviderId:
                    mySolution.Locks.ExecuteOrQueueEx(myLifetime, "AttachDebuggerToUnityEditor", () =>
                    {
                        if (!run.Lifetime.IsAlive || myFrontendBackendHost.Model == null)
                        {
                            tcs.TrySetCanceled();
                            return;
                        }

                        var task = myFrontendBackendHost.Model.AttachDebuggerToUnityEditor.Start(myLifetime, Unit.Instance);
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
            var cancellationTs = run.GetData(ourCancellationTokenSourceKey).NotNull();
            var cancellationToken = cancellationTs.Token;

            myLogger.Trace("Before calling Refresh.");
            Refresh(run.Lifetime, tcs, cancellationToken).ContinueWith(_ =>
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

                    var backendUnityModel = myBackendUnityHost.BackendUnityModel.Value;
                    if (backendUnityModel == null)
                    {
                        tcs.SetException(new Exception("Unity Editor connection unavailable."));
                        return;
                    }

                    var filters = GetFilters(run);

                    UnitTestLaunchClientControllerInfo? unityClientControllerInfo = null;
                    var clientControllerInfo = run.HostController.GetClientControllerInfo(run);
                    if (clientControllerInfo != null)
                    {
                        unityClientControllerInfo = new UnitTestLaunchClientControllerInfo(
                            clientControllerInfo.AssemblyLocation,
                            clientControllerInfo.ExtraDependencies?.ToList(),
                            clientControllerInfo.TypeName);
                    }

                    var preference = mySolution.GetProtocolSolution().GetFrontendBackendModel().UnitTestPreference.Value;
                    if (preference == null)
                        return;

                    // If we select Both, then start with Edit mode tests
                    var mode = preference == UnitTestLaunchPreference.PlayMode ? TestMode.Play : TestMode.Edit;
                    var launch = new UnitTestLaunch(run.Launch.Session.Id, filters, mode, unityClientControllerInfo);

                    // Set up the launch and subscribe to results. Called immediately, because we know the model isn't
                    // null. Also called when the appdomain is reloaded for play mode tests. The lifetime will correctly
                    // unsubscribe when the appdomain is unloaded
                    myBackendUnityHost.BackendUnityModel!.ViewNotNull<BackendUnityModel>(taskLifetime, (lt, model) =>
                    {
                        myLogger.Trace("UnitTestLaunch.SetValue.");

                        model.UnitTestLaunch.SetValue(launch);
                        SubscribeResults(run, lt, launch);

                        if (preference == UnitTestLaunchPreference.Both)
                        {
                            launch.RunResult.Advise(lt, result =>
                            {
                                if (launch.TestMode == TestMode.Play)
                                    tcs.SetResult(result.Passed);
                                else
                                {
                                    // Now run Play mode
                                    launch = new UnitTestLaunch(launch.SessionId, launch.TestFilters, TestMode.Play,
                                        launch.ClientControllerInfo);
                                    model.UnitTestLaunch.SetValue(launch);
                                    SubscribeResults(run, lt, launch);
                                    launch.RunResult.Advise(lt, result => { tcs.SetResult(result.Passed); });
                                    StartTests(model, run, tcs, lt);
                                }
                            });
                        }
                        else
                        {
                            launch.RunResult.Advise(lt, result => { tcs.SetResult(result.Passed); });
                        }
                    });

                    StartTests(backendUnityModel, run, tcs, taskLifetime);

                    // set results for explicit tests
                    foreach (var element in run.Elements.OfType<INUnitTestElement>().Where(a =>
                        a.RunState == RunState.Explicit && !run.Launch.Criterion.Explicit.Contains(a.Id)))
                    {
                        myUnitTestResultManager.TestFinishing(element, run.Launch.Session,
                            UnitTestStatus.Ignored, "Test should be run explicitly");
                    }
                });
            }, cancellationToken);
        }

        private void StartTests(BackendUnityModel model, IUnitTestRun run, TaskCompletionSource<bool> tcs, Lifetime taskLifetime)
        {
            myLogger.Trace("RunUnitTestLaunch.Start.");
            var rdTask = model.RunUnitTestLaunch.Start(taskLifetime, Unit.Instance);
            rdTask.Result.Advise(taskLifetime, res =>
            {
                myLogger.Trace($"RunUnitTestLaunch result = {res.Result}");
                if (!res.Result)
                {
                    var defaultMessage = "Failed to start tests in Unity.";

                    var isCoverage =
                        run.HostController.HostId != WellKnownHostProvidersIds.DebugProviderId &&
                        run.HostController.HostId != WellKnownHostProvidersIds.RunProviderId;

                    if (myPackageCompatibilityValidator.HasNonCompatiblePackagesCombination(isCoverage, out var message))
                        defaultMessage = $"{defaultMessage} {message}";

                    if (model.UnitTestLaunch.Value.TestMode == TestMode.Play)
                    {
                        if (!myPackageCompatibilityValidator.CanRunPlayModeTests(out var playMessage))
                            defaultMessage = $"{defaultMessage} {playMessage}";
                    }

                    tcs.TrySetException(new Exception(defaultMessage, res.Error));
                }
            });
        }


        private Task Refresh(Lifetime lifetime, TaskCompletionSource<bool> tcs, CancellationToken cancellationToken)
        {
            var refreshLifetimeDef = Lifetime.Define(lifetime);
            var refreshLifetime = refreshLifetimeDef.Lifetime;

            cancellationToken.Register(() =>
            {
                // We may not be able to stop tests in Unity, but still we need to allow stopping Rider part of the tests
                tcs.TrySetCanceled();
                refreshLifetimeDef.Terminate();
            });

            WaitForUnityEditorConnectedAndIdle(refreshLifetime)
                .ContinueWith(_ =>
                {
                    return RefreshTask(refreshLifetime, tcs)
                        .ContinueWith(_ =>
                            WaitForUnityEditorConnectedAndIdle(refreshLifetime), refreshLifetime)
                        .Unwrap();
                }, refreshLifetime)
                .Unwrap()
                .ContinueWith(_ =>
                {
                    if (myBackendUnityHost.BackendUnityModel.Value == null)
                    {
                        tcs.SetException(new Exception("Unity Editor connection unavailable."));
                        refreshLifetimeDef.Terminate();
                        return;
                    }

                    var task = myBackendUnityHost.BackendUnityModel.Value.GetCompilationResult.Start(refreshLifetime, Unit.Instance);
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
                                    var notification = new NotificationModel(mySolution.GetRdProjectId(),
                                        "Compilation failed",
                                        "Script compilation in Unity failed, so tests were not started.", true,
                                        RdNotificationEntryType.INFO, new List<NotificationHyperlink>());
                                    myNotificationsModel.Notification(notification);
                                });
                            myFrontendBackendHost.Do(model => model.ActivateUnityLogView());
                            refreshLifetimeDef.Terminate();
                        }
                    });
                }, refreshLifetime, TaskContinuationOptions.None, mySolution.Locks.Tasks.UnguardedMainThreadScheduler);

            return JetTaskEx.While(() => refreshLifetime.IsAlive);
        }

        private Task RefreshTask(Lifetime lifetime, TaskCompletionSource<bool> tcs)
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
                        myUnityRefresher.Refresh(waitingLifetime, RefreshType.Normal) // RIDER-90121
                            .ContinueWith(result =>
                            {
                                if (result.Exception != null )
                                    tcs.SetException(result.Exception);
                                myLogger.Trace("After myUnityRefresher.Refresh");
                                waitingLifetimeDefinition.Terminate();
                            }, waitingLifetime);
                    }
                }, waitingLifetime, TaskContinuationOptions.None, mySolution.Locks.Tasks.GuardedMainThreadScheduler);
            });

            return JetTaskEx.While(() => waitingLifetime.IsAlive);
        }

        private void SubscribeResults(IUnitTestRun run, Lifetime connectionLifetime, UnitTestLaunch launch)
        {
            mySolution.Locks.AssertMainThread();

            launch.TestResult.AdviseNotNull(connectionLifetime, result =>
            {
                var unitTestElement = GetElementById(run, result.ProjectName, result.TestId);
                if (unitTestElement == null)
                {
                    // add dynamic tests
                    var parent = GetElementById(run, result.ProjectName, result.ParentId);
                    if (parent is NUnitTestElement elementParent)
                    {
                        //TODO: add ShortName to TestResult and populate it on Unity side
                        // Test cases in NUnit3 have the following ids:
                        //   - {namespace}.{class}.{method}{parameters} if the test case is not given a custom name
                        //   - {namespace}.{class}.{custom_name} if the test case is given a custom name
                        // We want to extract either {method}{parameters} or {custom_name}. For that we remove the last 
                        // part of parent id, which has the form {namespace}.{class}.{method}
                        var shortName = result.TestId.SubstringAfter(result.ParentId.SubstringBeforeLast(".") + ".");
                        
                        run.CreateDynamicElement(() => new NUnitRowTestElement(elementParent, shortName));
                    }
                    else if (parent is NUnitTestFixtureElement fixtureParent)
                    {
                        run.CreateDynamicElement(() => new NUnitTestElement(fixtureParent,
                            result.TestId.SubstringAfter($"{result.ParentId}."), null));
                    }
                }

                if (unitTestElement == null)
                    return;

                switch (result.Status)
                {
                    case Status.Pending:
                        myUnitTestResultManager.MarkPending(unitTestElement, run.Launch.Session);
                        break;
                    case Status.Running:
                        myUnitTestResultManager.TestStarting(unitTestElement, run.Launch.Session);
                        break;
                    case Status.Success:
                    case Status.Failure:
                    case Status.Ignored:
                    case Status.Inconclusive:
                        var message = string.Empty;
                        var messageHeader = "Message: " + Environment.NewLine; // header is hardcoded in Rider package
                        if (result.Output.StartsWith(messageHeader))
                            message = result.Output.Substring(messageHeader.Length);
                        var taskResult = UnitTestStatus.Inconclusive;
                        if (result.Status == Status.Failure)
                            taskResult = UnitTestStatus.Failed;
                        else if (result.Status == Status.Ignored)
                            taskResult = UnitTestStatus.Ignored;
                        else if (result.Status == Status.Inconclusive)
                            taskResult = UnitTestStatus.Inconclusive;
                        else if (result.Status == Status.Success)
                            taskResult = UnitTestStatus.Success;

                        myUnitTestResultManager.TestOutput(unitTestElement, run.Launch.Session, result.Output, TestOutputType.STDOUT);
                        myUnitTestResultManager.TestFinishing(unitTestElement, run.Launch.Session, taskResult, message, TimeSpan.FromMilliseconds(result.Duration));
                        break;
                    default:
                        throw new ArgumentOutOfRangeException($"Unknown test result from the protocol: {result.Status}");
                }
            });
        }

        private Task WaitForUnityEditorConnectedAndIdle(Lifetime lifetime)
        {
            myLogger.Trace("WaitForUnityEditorConnectedAndIdle");

            var waitingLifetimeDef = Lifetime.Define(lifetime);
            var waitingLifetime = waitingLifetimeDef.Lifetime;

            waitingLifetime.StartMainUnguarded(() =>
            {
                myBackendUnityHost.BackendUnityModel.Advise(waitingLifetime, model =>
                {
                    if (model != null)
                        waitingLifetimeDef.Terminate();
                });
            });

            return JetTaskEx.While(() => waitingLifetime.IsAlive);
        }

        private List<TestFilter> GetFilters(IUnitTestRun run)
        {
            var filters = new List<TestFilter>();
            var groups = new List<string>();
            var categories = new List<string>();
            var testNames = run.Elements
                .Where(unitTestElement => unitTestElement is NUnitTestElement or NUnitRowTestElement)
                .OfType<INUnitTestElement>()
                .Where(a => a.RunState != RunState.Explicit || run.Launch.Criterion.Explicit.Contains(a.Id))
                .Select(p => p.NaturalId.TestId)
                .ToList();

            filters.Add(new TestFilter(((UnityRuntimeEnvironment) run.RuntimeDescriptor).Project.Name, testNames, groups, categories));
            return filters;

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
        }

        private IUnitTestElement? GetElementById(IUnitTestRun run, string projectName, string resultTestId)
        {
            return run.Elements.SingleOrDefault(
                a => a is INUnitTestElement nunitElement &&
                     nunitElement.Project.Name == projectName &&
                     nunitElement.NaturalId.TestId == resultTestId
            );
        }

        public void Cancel(IUnitTestRun run)
        {
            mySolution.Locks.ExecuteOrQueueEx(run.Lifetime, "CancellingUnitTests", () =>
            {
                var launchProperty = myBackendUnityHost.BackendUnityModel.Value?.UnitTestLaunch;
                var launch = launchProperty?.Maybe.ValueOrDefault;
                if (launch != null && launch.SessionId == run.Launch.Session.Id)
                    launch.Abort.Start(run.Lifetime, Unit.Instance);
                // Operation Cancel can be called before Run by design.
                run.GetData(ourCancellationTokenSourceKey)?.Cancel();
            });
        }

        public void Abort(IUnitTestRun run)
        {
            Cancel(run);
        }

        public int? TryGetRunnerProcessId() => myProcessTracker.UnityProcessId.Value;

        private class UnityRuntimeEnvironment : IRuntimeDescriptorWithProject
        {
            public UnityRuntimeEnvironment(TargetPlatform targetPlatform, IProject project)
            {
                Platform = targetPlatform;
                Project = project;
            }

            public TargetPlatform Platform { get; }
            public IProject Project { get; }

            private bool Equals(UnityRuntimeEnvironment other)
            {
                return Platform == other.Platform && Equals(Project, other.Project);
            }

            public override bool Equals(object? obj)
            {
                if (ReferenceEquals(null, obj)) return false;
                if (ReferenceEquals(this, obj)) return true;
                if (obj.GetType() != GetType()) return false;
                return Equals((UnityRuntimeEnvironment) obj);
            }

            public override int GetHashCode()
            {
                return (int) Platform;
            }

            public static bool operator ==(UnityRuntimeEnvironment? left, UnityRuntimeEnvironment? right)
            {
                return Equals(left, right);
            }

            public static bool operator !=(UnityRuntimeEnvironment? left, UnityRuntimeEnvironment? right)
            {
                return !Equals(left, right);
            }

            public bool HasNativeCode => false;
        }
    }
}
