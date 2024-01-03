using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using JetBrains.Debugger.Worker.Plugins.Unity.Values.ValueReferences;
using JetBrains.Util;
using Mono.Debugging.Autofac;
using Mono.Debugging.Backend.Values;
using Mono.Debugging.Backend.Values.Render.ChildrenRenderers;
using Mono.Debugging.Backend.Values.ValueReferences;
using Mono.Debugging.Backend.Values.ValueRoles;
using Mono.Debugging.Client.Values;
using Mono.Debugging.Client.Values.Render;
using Mono.Debugging.MetadataLite.API;
using Mono.Debugging.MetadataLite.API.Selectors;
using Mono.Debugging.Soft;

namespace JetBrains.Debugger.Worker.Plugins.Unity.Values.Render.ChildrenRenderers
{
    // Adds an additional "Game Objects" child to the Scene type. Does not override the default children renderer.
    [DebuggerSessionComponent(typeof(SoftDebuggerType))]
    public class SceneRootChildrenRenderer<TValue> : ChildrenRendererBase<TValue, IObjectValueRole<TValue>>
        where TValue : class
    {
        // ReSharper disable once StaticMemberInGenericType
        private static readonly MethodSelector ourGetRootGameObjectsSelector =
            new MethodSelector(m => m.Name == "GetRootGameObjects" && m.Parameters.Length == 0);

        private readonly IUnityOptions myUnityOptions;

        public SceneRootChildrenRenderer(IUnityOptions unityOptions)
        {
            myUnityOptions = unityOptions;
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
                return new[] {new GameObjectsGroup(valueRole, getRootObjectsMethod, ValueServices, Logger)};
            return EmptyList<IValueEntity>.Enumerable;
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
                // Keep an eye on iterators and enumeration: we need to eagerly evaluate GetChildrenImpl so we can catch
                // any exceptions. The return value of GetChildren is eagerly evaluated, so we're not changing any
                // semantics. But remember that chunked groups are lazily evaluated, and need try/catch
                return myLogger.CatchEvaluatorException<TValue, IEnumerable<IValueEntity>>(
                           () => GetChildrenImpl(options, token).ToList(),
                           exception => myLogger.LogThrownUnityException(exception,
                               mySceneValueRole.ValueReference.OriginatingFrame, myValueServices, options))
                       ?? EmptyList<IValueEntity>.Enumerable;
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
                try
                {
                    var elementRole = elementReference.AsObject(options);

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
                catch (Exception e)
                {
                    // We must always return a value, as we're effectively showing the contents of an array here. We're
                    // possibly also being evaluated lazily, thanks to chunked arrays, so can't rely on the caller
                    // catching exceptions.
                    myLogger.LogExceptionSilently(e);
                    return myValueServices.ValueRenderers.GetValueStubForException(e, "Game Object",
                               elementReference.OriginatingFrame) as IValue
                           ?? new ErrorValue("Game Object", "Error retrieving child game object");
                }
            }
        }
    }
}