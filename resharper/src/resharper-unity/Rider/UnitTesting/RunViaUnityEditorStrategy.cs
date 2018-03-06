using System;
using System.Collections.Generic;
using System.Linq;
using JetBrains.Annotations;
using JetBrains.Application.Threading;
using JetBrains.DataFlow;
using JetBrains.Metadata.Access;
using JetBrains.Platform.Unity.Model;
using JetBrains.ProjectModel;
using JetBrains.ReSharper.TaskRunnerFramework;
using JetBrains.ReSharper.UnitTestFramework;
using JetBrains.ReSharper.UnitTestFramework.Launch;
using JetBrains.ReSharper.UnitTestFramework.Strategy;
using JetBrains.ReSharper.UnitTestProvider.nUnit.v30.Elements;
using JetBrains.Util;
using UnitTestLaunch = JetBrains.Platform.Unity.Model.UnitTestLaunch;

namespace JetBrains.ReSharper.Plugins.Unity.Rider.UnitTesting
{
    public class RunViaUnityEditorStrategy : IUnitTestRunStrategy
    {
        private readonly ISolution mySolution;
        private readonly IUnitTestResultManager myUnitTestResultManager;
        private readonly UnityEditorProtocol myUnityEditorProtocol;

        public RunViaUnityEditorStrategy(ISolution solution, UnityModel unityModel,
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

        public bool RequiresSeparateRunPerProject(IProject project)
        {
            return false;
        }

        public bool RequiresProjectPropertiesRefreshBeforeLaunch()
        {
            return false;
        }

        public RuntimeEnvironment GetRuntimeEnvironment(IProject project, RuntimeEnvironment projectRuntimeEnvironment,
            TargetPlatform targetPlatform, IUserDataHolder userData)
        {
            return projectRuntimeEnvironment;
        }

        public void Run(IUnitTestRun run)
        {
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
            
                RunInternal(run, currentConnectionLifetime.Lifetime, myUnityEditorProtocol.UnityModel.Value);    
            });
        }

        private void RunInternal(IUnitTestRun run, Lifetime connectionLifetime, UnityModel unityModel)
        {
            mySolution.Locks.AssertMainThread();
            
            var elementToIdMap = new Dictionary<string, IUnitTestElement>();
            var allNames = InitElementsMap(run.Elements, elementToIdMap);
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
                            run.Launch.Session);
                        break;
                    case Status.Running:
                        myUnitTestResultManager.TestStarting(unitTestElement,
                            run.Launch.Session);
                        break;
                    case Status.Passed:
                        myUnitTestResultManager.TestFinishing(unitTestElement,
                            run.Launch.Session, "Passed", TaskResult.Success);
                        break;
                    case Status.Failed:
                        myUnitTestResultManager.TestFinishing(unitTestElement,
                            run.Launch.Session, "Passed", TaskResult.Error);
                        break;
                    default:
                        throw new ArgumentOutOfRangeException(
                            $"Unknown test result from the protocol: {result.Status}");
                }
            });
            
            launch.RunResult.Advise(connectionLifetime, result =>
            {
                run.Finish();
            });

            unityModel.UnitTestLaunch.Value = launch;
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
        }

        public void Abort(IUnitTestRun run)
        {
        }

        protected bool Equals(RunViaUnityEditorStrategy other)
        {
            return Equals(mySolution, other.mySolution);
        }

        public override bool Equals(object obj)
        {
            if (ReferenceEquals(null, obj)) return false;
            if (ReferenceEquals(this, obj)) return true;
            if (obj.GetType() != this.GetType()) return false;
            return Equals((RunViaUnityEditorStrategy) obj);
        }

        public override int GetHashCode()
        {
            return (mySolution != null ? mySolution.GetHashCode() : 0);
        }
    }
}