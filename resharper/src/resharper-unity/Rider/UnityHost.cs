using System;
using JetBrains.Application.Threading;
using JetBrains.ProjectModel;
using JetBrains.ReSharper.Host.Features;
using JetBrains.Rider.Model;

namespace JetBrains.ReSharper.Plugins.Unity.Rider
{
    [SolutionComponent]
    public class UnityHost
    {
        // TODO: this shouldn't be up in tests until we figure out how to test unity-editor requiring features
        private readonly bool myIsInTests;

        private readonly RdUnityModel myModel;

        public UnityHost(ISolution solution, IShellLocks locks)
        {
            myIsInTests = locks.Dispatcher.IsAsyncBehaviorProhibited;
            if (myIsInTests)
                return;

            myModel = solution.GetProtocolSolution().GetRdUnityModel();
        }

        public void PerformModelAction(Action<RdUnityModel> action)
        {
            if (myIsInTests)
                return;

            action(myModel);
        }
    }
}