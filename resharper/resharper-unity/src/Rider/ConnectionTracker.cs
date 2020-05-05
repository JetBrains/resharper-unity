using System;
using System.Threading.Tasks;
using JetBrains.Application.Threading;
using JetBrains.Collections.Viewable;
using JetBrains.Core;
using JetBrains.DataFlow;
using JetBrains.Lifetimes;
using JetBrains.Platform.Unity.EditorPluginModel;
using JetBrains.ProjectModel;
using JetBrains.Rd.Tasks;
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
            IThreading locks, UnitySolutionTracker unitySolutionTracker)
        {
            State = new Property<UnityEditorState>(lifetime, "UnityEditorPlugin::ConnectionState", UnityEditorState.Disconnected);
            
            if (locks.Dispatcher.IsAsyncBehaviorProhibited)
                return;

            unitySolutionTracker.IsUnityProject.AdviseOnce(lifetime, args =>
            {
                if (!args)
                    return;

                //check connection between backend and unity editor
                locks.QueueRecurring(lifetime, "PeriodicallyCheck", TimeSpan.FromSeconds(1), () =>
                {
                    var model = editorProtocol.UnityModel.Value;
                    if (model == null)
                    {
                        State.SetValue(UnityEditorState.Disconnected);
                    }
                    else
                    {
                        if (!model.IsBound)
                            State.SetValue(UnityEditorState.Disconnected);
                        
                        var rdTask = model.GetUnityEditorState.Start(Unit.Instance);
                        rdTask?.Result.Advise(lifetime, result =>
                        {
                            State.SetValue(result.Result);
                            logger.Trace($"Inside Result. Sending connection state. State: {State.Value}");
                            host.PerformModelAction(m => m.EditorState.Value = Wrap(State.Value));
                        });

                        var waitTask = Task.Delay(TimeSpan.FromSeconds(2));
                        waitTask.ContinueWith(_ =>
                        {
                            if (rdTask != null && !rdTask.AsTask().IsCompleted)
                            {
                                logger.Trace($"There were no response from Unity in one second. Set connection state to Disconnected.");
                                State.SetValue(UnityEditorState.Disconnected);
                            }
                        }, locks.Tasks.GuardedMainThreadScheduler);
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
                case UnityEditorState.Pause:
                    return EditorState.ConnectedPause;
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