using System.Collections.Generic;
using System.Threading;
using JetBrains.Annotations;
using JetBrains.Util;
using MetadataLite.API;
using Mono.Debugging.Autofac;
using Mono.Debugging.Backend.Values.ValueReferences;
using Mono.Debugging.Backend.Values.ValueRoles;
using Mono.Debugging.Client.Values;
using Mono.Debugging.Client.Values.Render;
using Mono.Debugging.Soft;

namespace JetBrains.ReSharper.Plugins.Unity.Rider.Debugger.Values.Render.ChildrenRenderers
{
    [DebuggerSessionComponent(typeof(SoftDebuggerType))]
    public class ComponentChildrenRenderer<TValue> : DeprecatedPropertyFilteringChildrenRendererBase<TValue>
        where TValue : class
    {
        private readonly IUnityOptions myUnityOptions;
        private readonly ILogger myLogger;

        public ComponentChildrenRenderer(IUnityOptions unityOptions, ILogger logger)
        {
            myUnityOptions = unityOptions;
            myLogger = logger;
        }

        protected override bool IsApplicable(IMetadataTypeLite type, IPresentationOptions options,
                                             IUserDataHolder dataHolder)
        {
            return myUnityOptions.ExtensionsEnabled && type.FindTypeThroughHierarchy("UnityEngine.Component") != null;
        }

        protected override IEnumerable<IValueEntity> GetChildren(IObjectValueRole<TValue> valueRole,
                                                                 IMetadataTypeLite instanceType,
                                                                 IPresentationOptions options,
                                                                 IUserDataHolder dataHolder, CancellationToken token)
        {
            var scenePathValue = GetGameObjectScenePath(valueRole, options);
            if (scenePathValue != null) yield return scenePathValue;

            foreach (var valueEntity in base.GetChildren(valueRole, instanceType, options, dataHolder, token))
                yield return valueEntity;
        }

        [CanBeNull]
        private IValueEntity GetGameObjectScenePath(IObjectValueRole<TValue> componentRole, IPresentationOptions options)
        {
            var gameObjectRole =
                componentRole.GetInstancePropertyReference("gameObject", true)?.AsObjectSafe(options);
            if (gameObjectRole == null)
                return null;
            return ScenePathValueHelper.GetScenePathValue(gameObjectRole, options, ValueServices, myLogger);
        }
    }
}