using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using JetBrains.ReSharper.Plugins.Unity.Rider.Debugger.Values.ValueReferences;
using JetBrains.Util;
using MetadataLite.API;
using MetadataLite.API.Selectors;
using Mono.Debugging.Autofac;
using Mono.Debugging.Backend.Values;
using Mono.Debugging.Backend.Values.Render.ChildrenRenderers;
using Mono.Debugging.Backend.Values.ValueReferences;
using Mono.Debugging.Backend.Values.ValueRoles;
using Mono.Debugging.Client.Values;
using Mono.Debugging.Client.Values.Render;
using Mono.Debugging.Soft;

namespace JetBrains.ReSharper.Plugins.Unity.Rider.Debugger.Values.Render.ChildrenRenderers
{
    [DebuggerSessionComponent(typeof(SoftDebuggerType))]
    public class SceneRootChildrenRenderer<TValue> : ChildrenRendererBase<TValue, IObjectValueRole<TValue>>
        where TValue : class
    {
        // ReSharper disable once StaticMemberInGenericType
        private static readonly MethodSelector ourGetRootGameObjectsSelector =
            new MethodSelector(m => m.Name == "GetRootGameObjects" && m.Parameters.Length == 0);

        private readonly IUnityOptions myUnityOptions;
        private readonly ILogger myLogger;

        public SceneRootChildrenRenderer(IUnityOptions unityOptions, ILogger logger)
        {
            myUnityOptions = unityOptions;
            myLogger = logger;
        }

        public override int Priority => UnityRendererUtil.ChildrenRendererPriority;
        public override bool IsExclusive => false;

        protected override bool IsApplicable(IMetadataTypeLite type, IPresentationOptions options,
                                             IUserDataHolder dataHolder)
        {
            // UnityEngine.SceneManagement.Scene was introduced in Unity 5.3
            return myUnityOptions.ExtensionsEnabled && type.Is("UnityEngine.SceneManagement.Scene");
        }

        protected override IEnumerable<IValueEntity> GetChildren(IObjectValueRole<TValue> valueRole,
                                                                 IMetadataTypeLite instanceType,
                                                                 IPresentationOptions options,
                                                                 IUserDataHolder dataHolder, CancellationToken token)
        {
            // GetRootGameObjects was introduced in Unity 5.3.2
            var getRootObjectsMethod = valueRole.ReifiedType.MetadataType.GetMethods()
                .FirstOrDefault(ourGetRootGameObjectsSelector);
            if (getRootObjectsMethod != null)
                yield return new GameObjectsGroup(valueRole, getRootObjectsMethod, ValueServices, myLogger);
        }

        private class GameObjectsGroup : ChunkedValueGroupBase<IArrayValueRole<TValue>>
        {
            private readonly IObjectValueRole<TValue> mySceneValueRole;
            private readonly IMetadataMethodLite myGetRootObjectsMethod;
            private readonly IValueServicesFacade<TValue> myValueServices;
            private readonly ILogger myLogger;

            public GameObjectsGroup(IObjectValueRole<TValue> sceneValueRole, IMetadataMethodLite getRootObjectsMethod,
                                    IValueServicesFacade<TValue> valueServices, ILogger logger)
                : base("Game objects")
            {
                mySceneValueRole = sceneValueRole;
                myGetRootObjectsMethod = getRootObjectsMethod;
                myValueServices = valueServices;
                myLogger = logger;
            }

            public override IEnumerable<IValueEntity> GetChildren(IPresentationOptions options, CancellationToken token = new CancellationToken())
            {
                try
                {
                    return GetChildrenImpl(options, token);
                }
                catch (Exception e)
                {
                    myLogger.Error(e);
                    return EmptyList<IValueEntity>.Enumerable;
                }
            }

            private IEnumerable<IValueEntity> GetChildrenImpl(IPresentationOptions options, CancellationToken token)
            {
                // GameObject[] Scene.GetRootObjects()
                var gameObjectArray = new SimpleValueReference<TValue>(
                        mySceneValueRole.CallInstanceMethod(myGetRootObjectsMethod),
                        mySceneValueRole.ValueReference.OriginatingFrame, myValueServices.RoleFactory)
                    .GetExactPrimaryRoleSafe<TValue, IArrayValueRole<TValue>>(options);
                if (gameObjectArray == null)
                {
                    myLogger.Warn("Unable to retrieve GameObject array, or unexpectedly returned null");
                    yield break;
                }

                if (options.ClusterArrays)
                {
                    var absoluteElementCount = ArrayIndexUtil.GetAbsoluteElementCount(gameObjectArray.Dimensions);
                    foreach (var valueEntity in GetChunkedChildren(gameObjectArray, 0, absoluteElementCount, options, token))
                        yield return valueEntity;
                }
                else
                {
                    var enumerator = (IChildReferencesEnumerator<TValue>) gameObjectArray;
                    foreach (var childReference in enumerator.GetChildReferences())
                        yield return GetElementValue(childReference, options);
                }
            }

            protected override IValue GetElementValueAt(IArrayValueRole<TValue> collection, int index,
                                                        IValueFetchOptions options)
            {
                var elementReference = collection.GetElementReference(index);
                return GetElementValue(elementReference, options);
            }

            private IValue GetElementValue(IValueReference<TValue> elementReference, IValueFetchOptions options)
            {
                var elementRole = elementReference.AsObjectSafe(options);
                if (elementRole == null)
                    return null;

                var isNameFromValue = true;
                var name = elementRole.GetInstancePropertyReference("name", true)?.AsStringSafe(options)
                    ?.GetString();
                if (name == null)
                {
                    name = "Game Object";
                    isNameFromValue = false;
                }

                // Tell the value presenter to hide the name field, if we're using it for the key. Also hide the default
                // type presentation - we know it's a GameObject, it's under a group called "Game Objects"
                return new CalculatedValueReferenceDecorator<TValue>(elementRole.ValueReference,
                    myValueServices.RoleFactory, name, !isNameFromValue, false).ToValue(myValueServices);
            }
        }
    }
}