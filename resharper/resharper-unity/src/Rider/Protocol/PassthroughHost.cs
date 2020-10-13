using JetBrains.Collections.Viewable;
using JetBrains.Lifetimes;
using JetBrains.ProjectModel;
using JetBrains.ReSharper.Plugins.Unity.ProjectModel;
using JetBrains.Rider.Model.Unity.FrontendBackend;

namespace JetBrains.ReSharper.Plugins.Unity.Rider.Protocol
{
    [SolutionComponent]
    public class PassthroughHost
    {
        private readonly UnityEditorProtocol myUnityEditorProtocol;
        private readonly FrontendBackendHost myFrontendBackendHost;

        public PassthroughHost(Lifetime lifetime,
                               UnitySolutionTracker unitySolutionTracker,
                               UnityEditorProtocol unityEditorProtocol,
                               FrontendBackendHost frontendBackendHost)
        {
            myUnityEditorProtocol = unityEditorProtocol;
            myFrontendBackendHost = frontendBackendHost;

            if (!frontendBackendHost.IsAvailable)
                return;

            unitySolutionTracker.IsUnityProject.View(lifetime, (lf, args) =>
            {
                var model = frontendBackendHost.Model;
                if (args && model != null)
                {
                    AdviseModelData(lf, model);
                }
            });
        }

        private void AdviseModelData(Lifetime lifetime, FrontendBackendModel frontendBackendModel)
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
            var backendUnityModelProperty = myUnityEditorProtocol.BackendUnityModel;
            frontendBackendModel.Play.FlowChangesIntoRdDeferred(lifetime,
                () => backendUnityModelProperty.Maybe.ValueOrDefault?.Play);
            frontendBackendModel.Pause.FlowChangesIntoRdDeferred(lifetime,
                () => backendUnityModelProperty.Maybe.ValueOrDefault?.Pause);
            frontendBackendModel.Step.Advise(lifetime, () => backendUnityModelProperty.Maybe.ValueOrDefault?.Step());
        }
    }
}