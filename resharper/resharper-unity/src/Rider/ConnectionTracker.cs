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
        private readonly IProperty<UnityEditorState> myState;

        public ConnectionTracker(Lifetime lifetime, ILogger logger, UnityHost host, UnityEditorProtocol editorProtocol,
            IThreading locks, UnitySolutionTracker unitySolutionTracker)
        {
            myState = new Property<UnityEditorState>(lifetime, "UnityEditorPlugin::ConnectionState", UnityEditorState.Disconnected);
            
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
                        myState.SetValue(UnityEditorState.Disconnected);
                    }
                    else
                    {
                        try
                        {
                            var rdTask = model.GetUnityEditorState.Start(Unit.Instance);
                            rdTask?.Result.Advise(lifetime, result =>
                            {
                                myState.SetValue(result.Result);
                                logger.Trace($"myIsConnected = {myState.Value}");
                            });
                        }
                        catch (Exception e)
                        {
                            e.Data.Add("UnityModel", editorProtocol.UnityModel.Value);
                            throw;
                        }
                    }

                    logger.Trace($"Sending connection state. State: {myState.Value}");
                    host.PerformModelAction(m => m.EditorState.Value = Wrap(myState.Value));
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
            return myState.Value != UnityEditorState.Refresh && myState.Value != UnityEditorState.Disconnected;
        }
    }
}