using System;
using System.Collections.Generic;
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
using JetBrains.Util;
using UnitTestLaunch = JetBrains.Platform.Unity.Model.UnitTestLaunch;

namespace JetBrains.ReSharper.Plugins.Unity.Rider.UnitTesting
{
    public class RunViaUnityEditorStrategy : IUnitTestRunStrategy
    {
        private readonly ISolution mySolution;
        private readonly UnityModel myUnityModel;
        private readonly Lifetime myCurrentConnectionLifetime;
        private readonly IUnitTestResultManager myUnitTestResultManager;
        private readonly Dictionary<string, IUnitTestElement> myElements = new Dictionary<string, IUnitTestElement>();

        public RunViaUnityEditorStrategy(ISolution solution, UnityModel unityModel, Lifetime currentConnectionLifetime,
            IUnitTestResultManager unitTestResultManager)
        {
            mySolution = solution;
            myUnityModel = unityModel;
            myCurrentConnectionLifetime = currentConnectionLifetime;
            myUnitTestResultManager = unitTestResultManager;
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
            mySolution.Locks.ExecuteOrQueueEx(myCurrentConnectionLifetime, "RunUnitTestThroughUE", () =>
            {
                var allNames = InitElementsMap(run.Elements);
                var emptyList = new List<string>();

                var launch = new UnitTestLaunch(allNames, emptyList, emptyList);
                
                launch.TestResult.Advise(myCurrentConnectionLifetime, result =>
                {
                    switch (result.Status)
                    {
                        case Status.Pending:
                            myUnitTestResultManager.MarkPending(GetElementById(result.TestId), run.Launch.Session);
                            break;
                        case Status.Running:
                            myUnitTestResultManager.TestStarting(GetElementById(result.TestId), run.Launch.Session);
                            break;
                        case Status.Passed:
                            myUnitTestResultManager.TestFinishing(GetElementById(result.TestId), run.Launch.Session, "Passed", TaskResult.Success);
                            break;
                        case Status.Failed:
                            myUnitTestResultManager.TestFinishing(GetElementById(result.TestId), run.Launch.Session, "Passed", TaskResult.Error);
                            break;
                        default:
                            throw new ArgumentOutOfRangeException($"Unknown test result from the protocol: {result.Status}");
                    }
                });

                myUnityModel.UnitTestLaunch.Value = launch;
            });
        }

        private List<string> InitElementsMap(IEnumerable<IUnitTestElement> unitTestElements)
        {
            var result = new List<string>();
            foreach (var unitTestElement in unitTestElements)
            {
                var unityName = string.Format($"{unitTestElement.Parent.ShortName}.{unitTestElement.ShortName}"); 
                myElements[unityName] = unitTestElement;
            }

            return result;
        }

        [NotNull]
        private IUnitTestElement GetElementById(string resultTestId)
        {
            var unitTestElement = myElements.TryGetValue(resultTestId);
            Assertion.AssertNotNull(unitTestElement, $"Could not find unitTestElement by id {resultTestId}");
            return unitTestElement;
        }

        public void Cancel(IUnitTestRun run)
        {
        }

        public void Abort(IUnitTestRun run)
        {
        }
    }
}