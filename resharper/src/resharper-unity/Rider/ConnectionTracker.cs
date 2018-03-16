using System;
using JetBrains.Application.Threading;
using JetBrains.DataFlow;
using JetBrains.Platform.RdFramework;
using JetBrains.Platform.Unity.Model;
using JetBrains.ProjectModel;
using JetBrains.ReSharper.Host.Features;
using JetBrains.Util;
using ILogger = JetBrains.Util.ILogger;

namespace JetBrains.ReSharper.Plugins.Unity.Rider
{
    [SolutionComponent]
    public class ConnectionTracker
    {
        private UnityEditorState myLastCheckResult = UnityEditorState.Disconnected;
        
        public ConnectionTracker(Lifetime lifetime, ILogger logger, UnityHost host, UnityEditorProtocol unityEditorProtocolController, IShellLocks locks)
        {
            // this shouldn't be up in tests until we figure out how to test unity-editor requiring features
            if (locks.Dispatcher.IsAsyncBehaviorProhibited)
                return;
            
            //check connection between backend and unity editor
            locks.QueueRecurring(lifetime, "PeriodicallyCheck", TimeSpan.FromSeconds(1), () =>
            {
                if (unityEditorProtocolController.UnityModel.Value == null)
                {
                    myLastCheckResult = UnityEditorState.Disconnected;
                }
                else
                {
                    var rdTask = unityEditorProtocolController.UnityModel.Value.GetUnityEditorState.Start(RdVoid.Instance);
                    rdTask?.Result.Advise(lifetime, result =>
                    {
                        myLastCheckResult = result.Result;
                        logger.Trace($"myIsConnected = {myLastCheckResult}");
                    });    
                }

                logger.Trace($"Sending connection state. State: {myLastCheckResult}");
                host.SetModelData("UNITY_EditorState", Wrap(myLastCheckResult));
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