using System.Collections.Generic;
using System.Threading;
using JetBrains.Util;
using Mono.Debugging.Autofac;
using Mono.Debugging.Backend.Values.ValueReferences;
using Mono.Debugging.Backend.Values.ValueRoles;
using Mono.Debugging.Client;
using Mono.Debugging.Client.Values;
using Mono.Debugging.Client.Values.Render;
using Mono.Debugging.MetadataLite.API;
using Mono.Debugging.Soft;

namespace JetBrains.Debugger.Worker.Plugins.Unity.Values.Render.ChildrenRenderers
{
    // Replaces the default children renderer for UnityEngine.Component. Filters out deprecated properties and adds a
    // Scene Path value, showing the path to the component's gameObject in the scene.
    [DebuggerSessionComponent(typeof(SoftDebuggerType))]
    public class ComponentChildrenRenderer<TValue> : DeprecatedPropertyFilteringChildrenRendererBase<TValue>
        where TValue : class
    {
        private readonly IDebuggerSession mySession;
        private readonly IUnityOptions myUnityOptions;

        public ComponentChildrenRenderer(IDebuggerSession session, IUnityOptions unityOptions)
        {
            mySession = session;
            myUnityOptions = unityOptions;
        }

        protected override bool IsApplicable(IMetadataTypeLite type, IPresentationOptions options,
                                             IUserDataHolder dataHolder)
        {
            return myUnityOptions.ExtensionsEnabled && type.FindTypeThroughHierarchy("UnityEngine.Component") != null;
        }

        protected override IEnumerable<IValueEntity> GetChildren(IObjectValueRole<TValue> valueRole,
                                                                 IMetadataTypeLite instanceType,
                                                                 IPresentationOptions options,
                                                                 IUserDataHolder dataHolder,
                                                                 CancellationToken token)
        {
            // GetChildren is passed options that always allow evaluation, e.g. to calculate IEnumerable's "Results"
            // node. We eagerly evaluate Scene Path here, we should return a lazy reference to allow evaluating during
            // presentation, so that we get the "Refresh" link if the user has disabled evaluation
            // TODO: Make "Scene Path" lazy in 212
            if (mySession.EvaluationOptions.AllowTargetInvoke)
            {
                // Only add "Scene Path" to the most derived type, not every "base" node back to Component
                var valueType = valueRole.ValueReference.GetValueType(options, ValueServices.ValueMetadataProvider);
                if (valueType.Equals(instanceType))
                {
                    var scenePathValue = GetGameObjectScenePath(valueRole, options);
                    if (scenePathValue != null) yield return scenePathValue;
                }
            }

            foreach (var valueEntity in base.GetChildren(valueRole, instanceType, options, dataHolder, token))
                yield return valueEntity;
        }

        private IValueEntity? GetGameObjectScenePath(IObjectValueRole<TValue> componentRole,
                                                     IPresentationOptions options)
        {
            var gameObjectRole = Logger.CatchEvaluatorException<TValue, IObjectValueRole<TValue>?>(
                () => componentRole.GetInstancePropertyReference("gameObject", true)
                    ?.AsObjectSafe(options),
                exception => Logger.LogThrownUnityException(exception, componentRole.ValueReference.OriginatingFrame,
                    ValueServices, options));
            return ScenePathValueHelper.GetScenePathValue(gameObjectRole, options, ValueServices, Logger);
        }
    }
}