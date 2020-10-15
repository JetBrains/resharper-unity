using System;
using System.Threading.Tasks;
using JetBrains.Application.Threading;
using JetBrains.Application.UI.Components;
using JetBrains.Collections.Viewable;
using JetBrains.Core;
using JetBrains.Lifetimes;
using JetBrains.ProjectModel;
using JetBrains.Rd.Tasks;
using JetBrains.ReSharper.Plugins.Unity.ProjectModel;
using JetBrains.Rider.Model.Unity;
using JetBrains.Util;
using ILogger = JetBrains.Util.ILogger;

namespace JetBrains.ReSharper.Plugins.Unity.Rider.Protocol
{
    [SolutionComponent]
    public class UnityEditorStateHost
    {
        private EditorState myState;

        public UnityEditorStateHost(Lifetime lifetime, ILogger logger, IThreading locks,
                                    FrontendBackendHost frontendBackendHost, BackendUnityHost backendUnityHost,
                                    UnitySolutionTracker unitySolutionTracker,
                                    IIsApplicationActiveState isApplicationActiveState)
        {
            myState = EditorState.Disconnected;

            if (locks.Dispatcher.IsAsyncBehaviorProhibited)
                return;

            unitySolutionTracker.IsUnityProject.AdviseOnce(lifetime, args =>
            {
                if (!args)
                    return;

                var updateConnectionAction = new Action(() =>
                {
                    var model = backendUnityHost.BackendUnityModel.Value;
                    if (model == null || !model.IsBound)
                    {
                        myState = EditorState.Disconnected;
                    }
                    else
                    {
                        var rdTask = model.GetUnityEditorState.Start(Unit.Instance);
                        rdTask?.Result.Advise(lifetime, result =>
                        {
                            myState = result.Result;
                            logger.Trace($"Inside Result. Sending connection state. State: {myState}");
                            frontendBackendHost.Do(m => m.EditorState.Value = myState);
                        });

                        var waitTask = Task.Delay(TimeSpan.FromSeconds(2));
                        waitTask.ContinueWith(_ =>
                        {
                            if (rdTask != null && !rdTask.AsTask().IsCompleted)
                            {
                                logger.Trace(
                                    "There were no response from Unity in two seconds. Set connection state to Disconnected.");
                                myState = EditorState.Disconnected;
                            }
                        }, locks.Tasks.GuardedMainThreadScheduler);
                    }

                    logger.Trace($"Sending connection state. State: {myState}");
                    frontendBackendHost.Do(m => m.EditorState.Value = myState);
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
            return myState != EditorState.Refresh && myState != EditorState.Disconnected;
        }
    }
}