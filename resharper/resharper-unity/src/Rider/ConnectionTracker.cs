using System;
using JetBrains.Application.Threading;
using JetBrains.Collections.Viewable;
using JetBrains.Core;
using JetBrains.Lifetimes;
using JetBrains.Platform.RdFramework.Util;
using JetBrains.Platform.Unity.EditorPluginModel;
using JetBrains.ProjectModel;
using JetBrains.ReSharper.Plugins.Unity.ProjectModel;
using JetBrains.Rider.Model;
using JetBrains.Util;
using ILogger = JetBrains.Util.ILogger;

namespace JetBrains.ReSharper.Plugins.Unity.Rider
{
    [SolutionComponent]
    public class ConnectionTracker
    {
        private UnityEditorState myLastCheckResult = UnityEditorState.Disconnected;

        public ConnectionTracker(Lifetime lifetime, ILogger logger, UnityHost host, UnityEditorProtocol editorProtocol, IShellLocks locks, UnitySolutionTracker unitySolutionTracker)
        {
            unitySolutionTracker.IsUnityProjectFolder.AdviseOnce(lifetime, args =>
            {
                //check connection between backend and unity editor
                locks.QueueRecurring(lifetime, "PeriodicallyCheck", TimeSpan.FromSeconds(1), () =>
                {
                    if (editorProtocol.UnityModel.Value == null)
                    {
                        myLastCheckResult = UnityEditorState.Disconnected;
                    }
                    else
                    {
                        var rdTask = editorProtocol.UnityModel.Value.GetUnityEditorState.Start(Unit.Instance);
                        rdTask?.Result.Advise(lifetime, result =>
                        {
                            myLastCheckResult = result.Result;
                            logger.Trace($"myIsConnected = {myLastCheckResult}");
                        });
                    }

                    logger.Trace($"Sending connection state. State: {myLastCheckResult}");
                    host.PerformModelAction(m => m.EditorState.Value = Wrap(myLastCheckResult));
                });
            });
        }

        // ReSharper disable once UnusedMember.Global
        public UnityEditorState LastCheckResult => myLastCheckResult;

        private EditorState Wrap(UnityEditorState state)
        {
            switch (state)
            {
                case UnityEditorState.Disconnected:
                    return EditorState.Disconnected;
                case UnityEditorState.Idle:
                    return EditorState.ConnectedIdle;
                case UnityEditorState.Play:
                    return EditorState.ConnectedPlay;
                case UnityEditorState.Refresh:
                    return EditorState.ConnectedRefresh;
                default:
                    throw new ArgumentOutOfRangeException(nameof(state), state, null);
            }
        }

        public bool IsConnectionEstablished()
        {
            return LastCheckResult != UnityEditorState.Refresh && LastCheckResult != UnityEditorState.Disconnected;
        }
    }
}