using System;
using JetBrains.Application.Threading;
using JetBrains.DataFlow;
using JetBrains.Platform.RdFramework;
using JetBrains.Platform.RdFramework.Tasks;
using JetBrains.Platform.Unity.Model;
using JetBrains.ProjectModel;
using JetBrains.ReSharper.Host.Features;
using JetBrains.ReSharper.Host.Features.Documents;
using JetBrains.Util;
using JetBrains.Util.Logging;
using ILogger = JetBrains.Util.ILogger;

namespace JetBrains.ReSharper.Plugins.Unity.Rider
{
    [SolutionComponent]
    public class ConnectionTracker
    {
        private UnityEditorState myLastCheckResult = UnityEditorState.Disconnected;
        
        public ConnectionTracker(Lifetime lifetime, ILogger logger, UnityPluginProtocolController unityEditorProtocolController, IShellLocks locks, ISolution solution)
        {
            //check connection between backend and unity editor
            locks.QueueRecurring(lifetime, "PeriodicallyCheck", TimeSpan.FromSeconds(1), () =>
            {
                if (unityEditorProtocolController.UnityModel == null)
                    myLastCheckResult = UnityEditorState.Disconnected;

                var rdTask = unityEditorProtocolController.UnityModel?.GetUnityEditorState.Start(RdVoid.Instance);
                rdTask?.Result.Advise(lifetime, result =>
                {
                    myLastCheckResult = result.Result;
                    logger.Trace($"myIsConnected = {myLastCheckResult}");
                });

                logger.Trace($"Sending connection state. State: {myLastCheckResult}");
                solution.GetProtocolSolution().SetCustomData("UNITY_EditorState", Wrap(myLastCheckResult));
            });
        }

        // ReSharper disable once UnusedMember.Global
        public UnityEditorState LastCheckResult => myLastCheckResult;

        private string Wrap(UnityEditorState state)
        {
            switch (state)
            {
                case UnityEditorState.Disconnected:
                    return "Disconnected";
                case UnityEditorState.Idle:
                    return "ConnectedIdle";
                case UnityEditorState.Play:
                    return "ConnectedPlay";
                case UnityEditorState.Refresh:
                    return "ConnectedRefresh";
                default:
                    throw new ArgumentOutOfRangeException(nameof(state), state, null);
            }
        }
    }
}