using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using JetBrains.Annotations;
using JetBrains.ReSharper.Plugins.Unity.Rider.Debugger.Values.ValueReferences;
using JetBrains.Util;
using MetadataLite.API;
using MetadataLite.API.Selectors;
using Mono.Debugging.Autofac;
using Mono.Debugging.Backend.Values;
using Mono.Debugging.Backend.Values.ValueReferences;
using Mono.Debugging.Backend.Values.ValueRoles;
using Mono.Debugging.Client.CallStacks;
using Mono.Debugging.Client.Values;
using Mono.Debugging.Client.Values.Render;
using Mono.Debugging.Soft;
using TypeSystem;

// ReSharper disable StaticMemberInGenericType

namespace JetBrains.ReSharper.Plugins.Unity.Rider.Debugger.Values.Render.ChildrenRenderers
{
    [DebuggerSessionComponent(typeof(SoftDebuggerType))]
    public class GameObjectChildrenRenderer<TValue> : DeprecatedPropertyFilteringChildrenRendererBase<TValue>
        where TValue : class
    {
        private static readonly MethodSelector ourGetChildSelector = new MethodSelector(m =>
            m.Name == "GetChild" && m.Parameters.Length == 1 && m.Parameters[0].Type.Is("System.Int32"));

        private static readonly MethodSelector ourGetComponentsSelector = new MethodSelector(m =>
            m.Name == "GetComponents" && m.Parameters.Length == 1 && m.Parameters[0].Type.Is("System.Type"));

        private static readonly MethodSelector ourGetInspectorTitleSelector = new MethodSelector(m =>
            m.IsStatic && m.Name == "GetInspectorTitle" && m.Parameters.Length == 1 &&
            m.Parameters[0].Type.Is("UnityEngine.Object"));

        private readonly IUnityOptions myUnityOptions;
        private readonly ILogger myLogger;

        public GameObjectChildrenRenderer(IUnityOptions unityOptions, ILogger logger)
        {
            myUnityOptions = unityOptions;
            myLogger = logger;
        }

        protected override bool IsApplicable(IMetadataTypeLite type, IPresentationOptions options,
                                             IUserDataHolder dataHolder)
        {
            return myUnityOptions.ExtensionsEnabled && type.Is("UnityEngine.GameObject");
        }

        protected override IEnumerable<IValueEntity> GetChildren(IObjectValueRole<TValue> valueRole,
                                                                 IMetadataTypeLite instanceType,
                                                                 IPresentationOptions options,
                                                                 IUserDataHolder dataHolder, CancellationToken token)
        {
            var scenePathValue = ScenePathValueHelper.GetScenePathValue(valueRole, options, ValueServices, myLogger);
            if (scenePathValue != null) yield return scenePathValue;

            yield return new GameObjectComponentsGroup(valueRole, ValueServices, myLogger);
            yield return new GameObjectChildrenGroup(valueRole, ValueServices, myLogger);

            foreach (var valueEntity in base.GetChildren(valueRole, instanceType, options, dataHolder, token))
                yield return valueEntity;
        }

        private class GameObjectComponentsGroup : ValueGroupBase
        {
            private readonly IObjectValueRole<TValue> myGameObjectRole;
            private readonly IValueServicesFacade<TValue> myValueServices;
            private readonly ILogger myLogger;

            public GameObjectComponentsGroup(IObjectValueRole<TValue> gameObjectRole,
                                             IValueServicesFacade<TValue> valueServices,
                                             ILogger logger)
                : base("Components")
            {
                myGameObjectRole = gameObjectRole;
                myValueServices = valueServices;
                myLogger = logger;
            }

            public override IEnumerable<IValueEntity> GetChildren(IPresentationOptions options,
                                                                  CancellationToken token = new CancellationToken())
            {
                try
                {
                    return GetChildrenImpl(options);
                }
                catch (Exception e)
                {
                    myLogger.Error(e);
                    return EmptyList<IValueEntity>.Enumerable;
                }
            }

            private IEnumerable<IValueEntity> GetChildrenImpl(IPresentationOptions options)
            {
                var frame = myGameObjectRole.ValueReference.OriginatingFrame;
                var componentType =
                    myValueServices.GetReifiedType(frame, "UnityEngine.Component, UnityEngine.CoreModule")
                    ?? myValueServices.GetReifiedType(frame, "UnityEngine.Component, UnityEngine");
                if (componentType == null)
                {
                    myLogger.Warn("Unable to find UnityEngine.Component");
                    yield break;
                }

                var getComponentsMethod = myGameObjectRole.ReifiedType.MetadataType.GetMethods()
                    .FirstOrDefault(ourGetComponentsSelector);
                if (getComponentsMethod == null)
                {
                    myLogger.Warn("Unable to find UnityEngine.GameObject.GetComponents method");
                    yield break;
                }

                // Call Component[] GameObject.GetComponents(typeof(Component))
                var typeObject = (IValueReference<TValue>) componentType.GetTypeObject(frame);
                var componentsArray =
                    myGameObjectRole.CallInstanceMethod(getComponentsMethod, typeObject.GetValue(options));
                var componentArray =
                    new SimpleValueReference<TValue>(componentsArray, frame, myValueServices.RoleFactory)
                        .GetExactPrimaryRoleSafe<TValue, IArrayValueRole<TValue>>(options);
                if (componentArray == null)
                {
                    myLogger.Warn("Cannot get return value of GameObject.GetComponents or method returned null");
                    yield break;
                }

                // string UnityEditor.ObjectNames.GetInspectorTitle(UnityEngine.Object)
                // Returns the name of the component, formatted the same as in the Inspector. Values are also cached per
                // type. This obviously won't be available for standalone players, where we'll display the short type
                // name instead.
                // TODO: Support extra fallback names
                // Unity doesn't use the short name, but will look at the type and use GameObject.name,
                // MonoBehaviour.GetScriptClassName and so on.
                var objectNamesType = myValueServices.GetReifiedType(frame, "UnityEditor.ObjectNames, UnityEditor")
                                      ?? myValueServices.GetReifiedType(frame,
                                          "UnityEditor.ObjectNames, UnityEditor.CoreModule");
                var getInspectorTitleMethod = objectNamesType?.MetadataType.GetMethods()
                    .FirstOrDefault(ourGetInspectorTitleSelector);

                var childReferencesEnumerator = (IChildReferencesEnumerator<TValue>) componentArray;
                foreach (var componentReference in childReferencesEnumerator.GetChildReferences())
                {
                    var componentName = GetComponentName(componentReference, objectNamesType,
                        getInspectorTitleMethod, frame, options, myValueServices, out var isNameFromValue);

                    // Tell the value presenter to hide the name field, if we're using it for the key. Also hide the
                    // default type presentation - we know it's a Component, it's under a group called "Components"
                    yield return new CalculatedValueReferenceDecorator<TValue>(componentReference,
                        myValueServices.RoleFactory, componentName, !isNameFromValue, false).ToValue(myValueServices);
                }
            }

            private string GetComponentName(IValueReference<TValue> componentValue,
                                            [CanBeNull] IReifiedType<TValue> objectNamesType,
                                            [CanBeNull] IMetadataMethodLite getInspectorTitleMethod,
                                            IStackFrame frame,
                                            IValueFetchOptions options,
                                            IValueServicesFacade<TValue> services,
                                            out bool isNameFromValue)
            {
                if (objectNamesType != null && getInspectorTitleMethod != null)
                {
                    try
                    {
                        var inspectorTitle = objectNamesType.CallStaticMethod(frame, options, getInspectorTitleMethod,
                            componentValue.GetValue(options));
                        var stringValueRole =
                            new SimpleValueReference<TValue>(inspectorTitle, frame, services.RoleFactory)
                                .AsStringSafe(options);
                        if (stringValueRole != null)
                        {
                            isNameFromValue = true;
                            return stringValueRole.GetString();
                        }
                    }
                    catch (Exception e)
                    {
                        myLogger.Error(e, "Unable to fetch object names for {0}", componentValue);
                    }
                }

                isNameFromValue = false;
                return componentValue.GetPrimaryRole(options).ReifiedType.MetadataType.ShortName;
            }
        }

        private class GameObjectChildrenGroup : ChunkedValueGroupBase<IObjectValueRole<TValue>>
        {
            private readonly IObjectValueRole<TValue> myGameObjectRole;
            private readonly IValueServicesFacade<TValue> myValueServices;
            private readonly ILogger myLogger;
            private IMetadataMethodLite myGetChildMethod;

            public GameObjectChildrenGroup(IObjectValueRole<TValue> gameObjectRole,
                                           IValueServicesFacade<TValue> valueServices, ILogger logger)
                : base("Children")
            {
                myGameObjectRole = gameObjectRole;
                myValueServices = valueServices;
                myLogger = logger;
            }

            public override IEnumerable<IValueEntity> GetChildren(IPresentationOptions options,
                                                                  CancellationToken token = new CancellationToken())
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
                // The children of a GameObject (as seen in Unity's Hierarchy view) are actually the children of
                // gameObject.transform. This will never be null.
                var transformRole = myGameObjectRole.GetInstancePropertyReference("transform")?.AsObjectSafe(options);
                if (transformRole == null)
                {
                    myLogger.Warn("Unable to retrieve GameObject.transform");
                    yield break;
                }

                var childCount = transformRole.GetInstancePropertyReference("childCount", true)
                    ?.AsPrimitiveSafe(options)?.GetPrimitiveSafe<int>() ?? 0;
                if (childCount == 0)
                {
                    myLogger.Trace("No child transform, or unable to fetch childCount");
                    yield break;
                }

                var transformType = transformRole.ReifiedType.MetadataType.FindTypeThroughHierarchy("UnityEngine.Transform");
                myGetChildMethod = transformType?.GetMethods().FirstOrDefault(ourGetChildSelector);
                if (myGetChildMethod == null)
                {
                    myLogger.Warn("Unable to find Transform.GetChild method");
                    yield break;
                }

                if (options.ClusterArrays)
                {
                    foreach (var valueEntity in GetChunkedChildren(transformRole, 0, childCount, options, token))
                        yield return valueEntity;
                }
                else
                {
                    for (var i = 0; i < childCount; i++)
                        yield return GetElementValueAt(transformRole, i, options);
                }
            }

            protected override IValue GetElementValueAt(IObjectValueRole<TValue> collection, int index, IValueFetchOptions options)
            {
                var frame = myGameObjectRole.ValueReference.OriginatingFrame;
                var indexValue = myValueServices.ValueFactory.CreatePrimitive(frame, options, index);
                var childTransformValue = collection.CallInstanceMethod(myGetChildMethod, indexValue);
                var childTransform = new SimpleValueReference<TValue>(childTransformValue,
                    frame, myValueServices.RoleFactory).AsObjectSafe(options);
                var gameObject = childTransform?.GetInstancePropertyReference("gameObject", true)
                    ?.AsObjectSafe(options);
                if (gameObject == null)
                    return new ErrorValue("Game Object", "Unable to retrieve child game object");

                var name = gameObject.GetInstancePropertyReference("name", true)?.AsStringSafe(options)
                    ?.GetString() ?? "Game Object";

                // Tell the value presenter to not show the name field, we're already showing it as the key. Also don't
                // show the type - a GameObject's child can only be a GameObject
                return new CalculatedValueReferenceDecorator<TValue>(gameObject.ValueReference,
                    myValueServices.RoleFactory, name, false, false).ToValue(myValueServices);
            }
        }
    }
}