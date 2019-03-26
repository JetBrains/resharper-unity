using System;
using JetBrains.Application.Threading;
using JetBrains.Collections.Viewable;
using JetBrains.Core;
using JetBrains.DataFlow;
using JetBrains.Lifetimes;
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
        public readonly IProperty<UnityEditorState> State;

        public ConnectionTracker(Lifetime lifetime, ILogger logger, UnityHost host, UnityEditorProtocol editorProtocol,
            IShellLocks locks, UnitySolutionTracker unitySolutionTracker)
        {
            State = new Property<UnityEditorState>(lifetime, "UnityEditorPlugin::ConnectionState", UnityEditorState.Disconnected);
            
            editorProtocol.UnityModel.View(lifetime, (lt, model) =>
            {
                if (!unitySolutionTracker.IsUnityProjectFolder.HasTrueValue()) // avoid recurring checks for non-unity projects
                    return;

                //check connection between backend and unity editor
                locks.QueueRecurring(lt, "PeriodicallyCheck", TimeSpan.FromSeconds(1), () =>
                {
                    if (model == null)
                        State.Value = UnityEditorState.Disconnected;
                    else
                    {
                        var rdTask = model.GetUnityEditorState.Start(Unit.Instance);
                        rdTask?.Result.Advise(lt, result =>
                        {
                            State.Value = result.Result;
                            logger.Trace($"myIsConnected = {State.Value}");
                        });
                    }

                    logger.Trace($"Sending connection state. State: {State.Value}");
                    host.PerformModelAction(m => m.EditorState.Value = Wrap(State.Value));
                });
            });
        }

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
            return State.Value != UnityEditorState.Refresh && State.Value != UnityEditorState.Disconnected;
        }
    }
}