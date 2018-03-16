using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using JetBrains.Annotations;
using JetBrains.Application.Threading;
using JetBrains.DataFlow;
using JetBrains.Metadata.Access;
using JetBrains.Metadata.Reader.API;
using JetBrains.Platform.Unity.EditorPluginModel;
using JetBrains.ProjectModel;
using JetBrains.ReSharper.TaskRunnerFramework;
using JetBrains.ReSharper.UnitTestFramework;
using JetBrains.ReSharper.UnitTestFramework.Launch;
using JetBrains.ReSharper.UnitTestFramework.Strategy;
using JetBrains.ReSharper.UnitTestProvider.nUnit.v30.Elements;
using JetBrains.Util;
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
        private readonly UnityEditorProtocol myUnityEditorProtocol;

        private static Key<string> ourLaunchedInUnityKey = new Key<string>("LaunchedInUnityKey");
        
        public RunViaUnityEditorStrategy(ISolution solution,
            IUnitTestResultManager unitTestResultManager, UnityEditorProtocol unityEditorProtocol)
        {
            mySolution = solution;
            myUnitTestResultManager = unitTestResultManager;
            myUnityEditorProtocol = unityEditorProtocol;
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

        public IRuntimeEnvironment GetRuntimeEnvironment(IUnitTestLaunch launch, IProject project, TargetFrameworkId targetFrameworkId)
        {
            var targetPlaform = TargetPlatformCalculator.GetTargetPlatform(launch, project, targetFrameworkId);
            return new UnityRuntimeEnvironment(targetPlaform);
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

            mySolution.Locks.ExecuteOrQueueEx(mySolution.GetLifetime(), "ExecuteRunUT", () =>
            {
                if(myUnityEditorProtocol.UnityModel.Value == null)
                    return;
                
                var currentConnectionLifetime = Lifetimes.Define(mySolution.GetLifetime());
                myUnityEditorProtocol.UnityModel.Change.Advise_NoAcknowledgement(currentConnectionLifetime.Lifetime, (args) =>
                {
                    if (args.HasNew && args.New == null)
                        currentConnectionLifetime.Terminate();
                });
            
                RunInternal(run, currentConnectionLifetime.Lifetime, myUnityEditorProtocol.UnityModel.Value, tcs);
            });

            return tcs.Task;
        }

        private void RunInternal(IUnitTestRun firstRun, Lifetime connectionLifetime, EditorPluginModel unityModel, TaskCompletionSource<bool> tcs)
        {
            mySolution.Locks.AssertMainThread();
            
            var elementToIdMap = new Dictionary<string, IUnitTestElement>();
            var unitTestElements = CollectElementsToRunInUnityEditor(firstRun);
            var allNames = InitElementsMap(unitTestElements, elementToIdMap);
            var emptyList = new List<string>();

            var launch = new UnitTestLaunch(allNames, emptyList, emptyList);

            launch.TestResult.Advise(connectionLifetime, result =>
            {
                var unitTestElement = GetElementById(result.TestId, elementToIdMap);
                if (unitTestElement == null)
                    return;
                
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
                    case Status.Passed:
                    case Status.Failed:
                        var taskResult = result.Status == Status.Failed ? TaskResult.Error : TaskResult.Success;
                        var message = result.Status == Status.Failed ? "Failed" : "Passed";
                        
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

            unityModel.UnitTestLaunch.Value = launch;
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

        private List<string> InitElementsMap(IEnumerable<IUnitTestElement> unitTestElements,
            Dictionary<string, IUnitTestElement> elementToIdMap)
        {
            var result = new JetHashSet<string>();
            foreach (var unitTestElement in unitTestElements)
            {
                if (unitTestElement is NUnitTestElement)
                {
                    var unityName = string.Format(unitTestElement.Id); 
                    elementToIdMap[unityName] = unitTestElement;
                    result.Add(unityName);
                }

                if (unitTestElement is UnityTestElement)
                {
                    var unityName = string.Format(unitTestElement.Id); 
                    elementToIdMap[unityName] = unitTestElement;
                    result.Add(unityName);
                }
            }

            return result.ToList();
        }

        [CanBeNull]
        private IUnitTestElement GetElementById(string resultTestId,
            Dictionary<string, IUnitTestElement> elementToIdMap)
        {
            var unitTestElement = elementToIdMap.TryGetValue(resultTestId);
            return unitTestElement;
        }

        public void Cancel(IUnitTestRun run)
        {
            // TODO: cancel tests in Unity Editor
            run.GetData(ourCompletionSourceKey).NotNull().SetCanceled();
        }

        public void Abort(IUnitTestRun run)
        {
            // TODO: cancel tests in Unity Editor
            run.GetData(ourCompletionSourceKey).NotNull().SetCanceled();
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