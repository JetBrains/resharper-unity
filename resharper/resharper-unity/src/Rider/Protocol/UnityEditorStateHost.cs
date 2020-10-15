using System;
using System.Threading.Tasks;
using JetBrains.Application.Threading;
using JetBrains.Application.UI.Components;
using JetBrains.Collections.Viewable;
using JetBrains.Core;
using JetBrains.DataFlow;
using JetBrains.Lifetimes;
using JetBrains.ProjectModel;
using JetBrains.Rd.Tasks;
using JetBrains.ReSharper.Plugins.Unity.ProjectModel;
using JetBrains.ReSharper.Resources.Shell;
using JetBrains.Rider.Model.Unity;
using JetBrains.Util;
using ILogger = JetBrains.Util.ILogger;

namespace JetBrains.ReSharper.Plugins.Unity.Rider.Protocol
{
    [SolutionComponent]
    public class UnityEditorStateHost
    {
        public readonly IProperty<EditorState> State;

        public UnityEditorStateHost(Lifetime lifetime, ILogger logger, FrontendBackendHost frontendBackendHost,
                                    UnityEditorProtocol editorProtocol, IThreading locks,
                                    UnitySolutionTracker unitySolutionTracker,
                                    IIsApplicationActiveState isApplicationActiveState)
        {
            State = new Property<EditorState>(lifetime, "UnityEditorPlugin::ConnectionState", EditorState.Disconnected);

            if (locks.Dispatcher.IsAsyncBehaviorProhibited)
                return;

            unitySolutionTracker.IsUnityProject.AdviseOnce(lifetime, args =>
            {
                if (!args)
                    return;

                var updateConnectionAction = new Action(() =>
                {
                    var model = editorProtocol.BackendUnityModel.Value;
                    if (model == null || !model.IsBound)
                    {
                        State.SetValue(EditorState.Disconnected);
                    }
                    else
                    {
                        var rdTask = model.GetUnityEditorState.Start(Unit.Instance);
                        rdTask?.Result.Advise(lifetime, result =>
                        {
                            State.SetValue(result.Result);
                            logger.Trace($"Inside Result. Sending connection state. State: {State.Value}");
                            frontendBackendHost.Do(m => m.EditorState.Value = State.Value);
                        });

                        var waitTask = Task.Delay(TimeSpan.FromSeconds(2));
                        waitTask.ContinueWith(_ =>
                        {
                            if (rdTask != null && !rdTask.AsTask().IsCompleted)
                            {
                                logger.Trace(
                                    "There were no response from Unity in two seconds. Set connection state to Disconnected.");
                                State.SetValue(EditorState.Disconnected);
                            }
                        }, locks.Tasks.GuardedMainThreadScheduler);
                    }

                    logger.Trace($"Sending connection state. State: {State.Value}");
                    frontendBackendHost.Do(m => m.EditorState.Value = State.Value);
                });

                lifetime.StartAsync(locks.Tasks.GuardedMainThreadScheduler, async () =>
                {
                    while (lifetime.IsAlive)
                    {
                        if (isApplicationActiveState.IsApplicationActive.Value
                            || frontendBackendHost.Model?.RiderFrontendTests.HasTrueValue() == true)
                        {
                            updateConnectionAction();
                        }

                        await Task.Delay(1000, lifetime);
                    }
                });
            });
        }

        public bool IsConnectionEstablished()
        {
            return State.Value != EditorState.Refresh && State.Value != EditorState.Disconnected;
        }
    }
}