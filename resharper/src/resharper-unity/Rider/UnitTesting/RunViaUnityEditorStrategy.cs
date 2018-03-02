using JetBrains.Metadata.Access;
using JetBrains.Platform.Unity.Model;
using JetBrains.ProjectModel;
using JetBrains.ReSharper.TaskRunnerFramework;
using JetBrains.ReSharper.UnitTestFramework.Launch;
using JetBrains.ReSharper.UnitTestFramework.Strategy;
using JetBrains.Util;

namespace JetBrains.ReSharper.Plugins.Unity.Rider.UnitTesting
{
    public class RunViaUnityEditorStrategy : IUnitTestRunStrategy
    {
        private readonly UnityModel myUnityModel;

        public RunViaUnityEditorStrategy(UnityModel unityModel)
        {
            myUnityModel = unityModel;
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
            //run.
        }

        public void Cancel(IUnitTestRun run)
        {
        }

        public void Abort(IUnitTestRun run)
        {
        }
    }
}