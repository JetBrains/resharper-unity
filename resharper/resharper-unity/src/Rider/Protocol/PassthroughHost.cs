using JetBrains.Application.Threading;
using JetBrains.Collections.Viewable;
using JetBrains.Diagnostics;
using JetBrains.DocumentModel;
using JetBrains.IDE;
using JetBrains.Lifetimes;
using JetBrains.ProjectModel;
using JetBrains.Rd.Tasks;
using JetBrains.ReSharper.Plugins.Unity.ProjectModel;
using JetBrains.Rider.Model.Unity;
using JetBrains.Rider.Model.Unity.BackendUnity;
using JetBrains.Rider.Model.Unity.FrontendBackend;
using JetBrains.TextControl;
using JetBrains.Util;
using JetBrains.Util.dataStructures.TypedIntrinsics;

using UnityApplicationData = JetBrains.Rider.Model.Unity.FrontendBackend.UnityApplicationData;

namespace JetBrains.ReSharper.Plugins.Unity.Rider.Protocol
{
    [SolutionComponent]
    public class PassthroughHost
    {
        private readonly ISolution mySolution;
        private readonly IThreading myThreading;
        private readonly IEditorManager myEditorManager;
        private readonly BackendUnityProtocol myBackendUnityProtocol;
        private readonly FrontendBackendHost myFrontendBackendHost;

        public PassthroughHost(Lifetime lifetime,
                               ISolution solution,
                               IThreading threading,
                               IEditorManager editorManager,
                               UnitySolutionTracker unitySolutionTracker,
                               BackendUnityProtocol backendUnityProtocol,
                               FrontendBackendHost frontendBackendHost)
        {
            mySolution = solution;
            myThreading = threading;
            myEditorManager = editorManager;
            myBackendUnityProtocol = backendUnityProtocol;
            myFrontendBackendHost = frontendBackendHost;

            if (!frontendBackendHost.IsAvailable)
                return;

            unitySolutionTracker.IsUnityProject.View(lifetime, (unityProjectLifetime , args) =>
            {
                var model = frontendBackendHost.Model;
                if (args && model != null)
                {
                    AdviseFrontendToUnityModel(unityProjectLifetime, model);

                    // Advise the backend/Unity model as high priority so we can add our subscriptions first
                    using (Signal.PriorityAdviseCookie.Create())
                    {
                        backendUnityProtocol.BackendUnityModel.ViewNotNull(unityProjectLifetime,
                            AdviseUnityToFrontendModel);
                    }
                }
            });
        }

        private void AdviseFrontendToUnityModel(Lifetime lifetime, FrontendBackendModel frontendBackendModel)
        {
            // BackendUnityModel is recreated frequently (e.g. on each AppDomain reload when changing play/edit mode).
            // So subscribe to the frontendBackendModel once and flow in changes only if backendUnityModel is available.
            // Note that we only flow changes, not the current value. Even though these properties are stateful,
            // frontendBackendModel is not the source of truth - values need to flow from backendUnityModel. Also, due
            // to model reload, we go through a few values before we stabilise. E.g.:
            // * User clicks play, fb.Play is true, flows into bu.Play which switches to play mode and causes an
            //   AppDomain reload.
            // * bu.Play becomes false due to AppDomain teardown, flows into fb.Play
            // * BackendUnityModel is torn down and recreated (<- WARNING!)
            // * bu.Play becomes true as Unity enters play mode, flows into fb.Play
            // If we flowed the current value of fb.Play into backendUnityModel when it is recreated, we'd set it to
            // false, triggering play mode to end.
            // Step is simply since it's a non-stateful ISource<T>
            var backendUnityModelProperty = myBackendUnityProtocol.BackendUnityModel;

            frontendBackendModel.Play.FlowChangesIntoRdDeferred(lifetime,
                () => backendUnityModelProperty.Maybe.ValueOrDefault?.Play);
            frontendBackendModel.Pause.FlowChangesIntoRdDeferred(lifetime,
                () => backendUnityModelProperty.Maybe.ValueOrDefault?.Pause);
            frontendBackendModel.Step.Advise(lifetime, () => backendUnityModelProperty.Maybe.ValueOrDefault?.Step());

            // Called from frontend to generate the UIElements schema files
            frontendBackendModel.GenerateUIElementsSchema.Set((l, u) =>
                backendUnityModelProperty.Maybe.ValueOrDefault?.GenerateUIElementsSchema.Start(l, u).ToRdTask(l));

            // Signalled from frontend to select and ping the object in the Project view
            frontendBackendModel.ShowFileInUnity.Advise(lifetime, file =>
                backendUnityModelProperty.Maybe.ValueOrDefault?.ShowFileInUnity.Fire(file));

            // Signalled from fronted to open the preferences window
            frontendBackendModel.ShowPreferences.Advise(lifetime, _ =>
                backendUnityModelProperty.Maybe.ValueOrDefault?.ShowPreferences.Fire());

            // Called from frontend to run a method in unity
            frontendBackendModel.RunMethodInUnity.Set((l, data) =>
            {
                var backendUnityModel = backendUnityModelProperty.Maybe.ValueOrDefault;
                return backendUnityModel == null
                    ? RdTask<RunMethodResult>.Cancelled()
                    : backendUnityModel.RunMethodInUnity.Start(l, data).ToRdTask(l);
            });
        }

        private void AdviseUnityToFrontendModel(Lifetime lifetime, BackendUnityModel backendUnityModel)
        {
            // *********************************************************************************************************
            //
            // WARNING
            //
            // Be very careful with stateful properties!
            //
            // When the backend/Unity protocol is closed, the existing properties maintain their current values. This
            // doesn't affect BackendUnityModel because we clear the model when the connection is lost. However, it does
            // affect any properties that have had values flowed in from BackedUnityModel - these values are not reset.
            //
            // When the backend/Unity protocol is (re)created and advertised, we *should* have initial values from the
            // Unity end (the model is advertised asynchronously to being created, and the dispatcher *should* have
            // processed messages). However, we cannot guarantee this - during testing, it usually works as expected,
            // but occasionally wouldn't be fully initialised. These means we need to be careful when assuming that
            // initial values are available in the properties. Advise and RdExtensions.FlowIntoRdSafe will correctly set
            // the target value if the source value exists. Avoid BeUtilExtensions.FlowIntoRd, as that will throw an
            // exception if the source value does not yet exist.
            // Note that creating and advertising the model, as well as all callbacks, happen on the main thread.
            //
            // We must ensure that the Unity end (re)initialises properties when the protocol is created, or we could
            // have stale or empty properties here and in the frontend.
            //
            // *********************************************************************************************************

            var frontendBackendModel = myFrontendBackendHost.Model.NotNull("frontendBackendModel != null");
            AdviseApplicationData(lifetime, backendUnityModel, frontendBackendModel);
            AdviseApplicationSettings(lifetime, backendUnityModel, frontendBackendModel);
            AdvisePlayControls(lifetime, backendUnityModel, frontendBackendModel);
            AdviseConsoleEvents(lifetime, backendUnityModel, frontendBackendModel);
            AdviseOpenFile(backendUnityModel, frontendBackendModel);
        }

        private static void AdviseApplicationData(in Lifetime lifetime, BackendUnityModel backendUnityModel,
                                                  FrontendBackendModel frontendBackendModel)
        {
            backendUnityModel.UnityProcessId.FlowIntoRdSafe(lifetime, frontendBackendModel.UnityProcessId);

            backendUnityModel.EditorLogPath.FlowIntoRdSafe(lifetime, frontendBackendModel.EditorLogPath);
            backendUnityModel.PlayerLogPath.FlowIntoRdSafe(lifetime, frontendBackendModel.PlayerLogPath);

            // TODO: Is this application data or application settings?
            backendUnityModel.BuildLocation.FlowIntoRdSafe(lifetime, frontendBackendModel.BuildLocation);

            backendUnityModel.UnityApplicationData.FlowIntoRdSafe(lifetime, data =>
            {
                var version = UnityVersion.Parse(data.ApplicationVersion);
                return new UnityApplicationData(data.ApplicationPath, data.ApplicationContentsPath,
                    data.ApplicationVersion, UnityVersion.RequiresRiderPackage(version));
            }, frontendBackendModel.UnityApplicationData);
        }

        private static void AdviseApplicationSettings(in Lifetime lifetime, BackendUnityModel backendUnityModel,
                                                      FrontendBackendModel frontendBackendModel)
        {
            backendUnityModel.ScriptCompilationDuringPlay.FlowIntoRdSafe(lifetime,
                frontendBackendModel.ScriptCompilationDuringPlay);
        }

        private static void AdvisePlayControls(in Lifetime lifetime, BackendUnityModel backendUnityModel,
                                               FrontendBackendModel frontendBackendModel)
        {
            backendUnityModel.Play.FlowIntoRdSafe(lifetime, frontendBackendModel.Play);
            backendUnityModel.Pause.FlowIntoRdSafe(lifetime, frontendBackendModel.Pause);
        }

        private static void AdviseConsoleEvents(in Lifetime lifetime, BackendUnityModel backendUnityModel,
                                                FrontendBackendModel frontendBackendModel)
        {
            backendUnityModel.Log.Advise(lifetime, frontendBackendModel.OnUnityLogEvent);

            backendUnityModel.LastInitTime.FlowIntoRdSafe(lifetime, frontendBackendModel.LastInitTime);
            backendUnityModel.LastPlayTime.FlowIntoRdSafe(lifetime, frontendBackendModel.LastPlayTime);
        }

        private void AdviseOpenFile(BackendUnityModel backendUnityModel, FrontendBackendModel frontendBackendModel)
        {
            backendUnityModel.OpenFileLineCol.Set(args =>
            {
                var result = false;
                mySolution.Locks.ExecuteWithReadLock(() =>
                {
                    myEditorManager.OpenFile(FileSystemPath.Parse(args.Path), OpenFileOptions.DefaultActivate, myThreading,
                        textControl =>
                        {
                            var line = args.Line;
                            var column = args.Col;

                            if (line > 0 || column > 0) // avoid changing placement when it is not requested
                            {
                                if (line > 0) line--;
                                if (line < 0) line = 0;
                                if (column > 0) column--;
                                if (column < 0) column = 0;
                                textControl.Caret.MoveTo((Int32<DocLine>) line, (Int32<DocColumn>) column,
                                    CaretVisualPlacement.Generic);
                            }

                            frontendBackendModel.ActivateRider();
                            result = true;
                        },
                        () => result = false);
                });

                return result;
            });
        }
    }
}