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
using Mono.Debugging.Backend.Values.Render.ChildrenRenderers;
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
    public class GameObjectChildrenRenderer<TValue> : ChildrenRendererBase<TValue, IObjectValueRole<TValue>>
        where TValue : class
    {
        private static readonly MethodSelector ourGetChildSelector = new MethodSelector(m =>
            m.Name == "GetChild" && m.Parameters.Length == 1 && m.Parameters[0].Type.Is("System.Int32"));

        private static readonly MethodSelector ourGetComponentsSelector = new MethodSelector(m =>
            m.Name == "GetComponents" && m.Parameters.Length == 1 && m.Parameters[0].Type.Is("System.Type"));

        private static readonly MethodSelector ourGetInspectorTitleSelector = new MethodSelector(m =>
            m.IsStatic && m.Name == "GetInspectorTitle" && m.Parameters.Length == 1 &&
            m.Parameters[0].Type.Is("UnityEngine.Object"));

        private readonly ILogger myLogger;

        public GameObjectChildrenRenderer(ILogger logger)
        {
            myLogger = logger;
        }

        protected override bool IsApplicable(IMetadataTypeLite type, IPresentationOptions options,
                                             IUserDataHolder dataHolder)
        {
            return type.Is("UnityEngine.GameObject");
        }

        protected override IEnumerable<IValueEntity> GetChildren(IObjectValueRole<TValue> valueRole,
                                                                 IMetadataTypeLite instanceType,
                                                                 IPresentationOptions options,
                                                                 IUserDataHolder dataHolder, CancellationToken token)
        {
            yield return new GameObjectComponentsGroup(valueRole, ValueServices, myLogger);

            // The children of the current GameObject (as seen in Unity's Hierarchy view) are actually the children of
            // GameObject.transform. This should never be null
            var transformProperty = valueRole.GetInstancePropertyReference("transform");
            if (transformProperty != null)
                yield return new GameObjectChildrenGroup(transformProperty, ValueServices);
        }

        public override int Priority => 100;
        public override bool IsExclusive => false;

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
                var frame = myGameObjectRole.ValueReference.OriginatingFrame;
                var componentType = myValueServices.GetReifiedType(frame, "UnityEngine.Component, UnityEngine.CoreModule")
                    ?? myValueServices.GetReifiedType(frame, "UnityEngine.Component, UnityEngine");
                if (componentType == null)
                    yield break;

                var getComponentsMethod = myGameObjectRole.ReifiedType.MetadataType.GetMethods()
                    .FirstOrDefault(ourGetComponentsSelector);
                if (getComponentsMethod == null)
                    yield break;

                // Component[] GameObject.GetComponents(typeof(Component))
                var typeObject = (IValueReference<TValue>) componentType.GetTypeObject(frame);
                var componentsArray =
                    myGameObjectRole.CallInstanceMethod(getComponentsMethod, typeObject.GetValue(options));
                var componentArray =
                    new SimpleValueReference<TValue>(componentsArray, frame, myValueServices.RoleFactory)
                        .GetExactPrimaryRoleSafe<TValue, IArrayValueRole<TValue>>(options);
                if (componentArray == null)
                    yield break;

                // string UnityEditor.ObjectNames.GetInspectorTitle(UnityEngine.Object)
                // Returns the name of the component, formatted the same as in the Inspector. Values are also cached per
                // type. This obviously won't be available for standalone players, where we'll display the short type
                // name instead.
                // TODO: Support extra fallback names
                // Unity doesn't use the short name, but will look at the type and use GameObject.name,
                // MonoBehaviour.GetScriptClassName and so on.
                var objectNamesType = myValueServices.GetReifiedType(frame, "UnityEditor.ObjectNames, UnityEditor")
                     ?? myValueServices.GetReifiedType(frame, "UnityEditor.ObjectNames, UnityEditor.CoreModule");
                var getInspectorTitleMethod = objectNamesType?.MetadataType.GetMethods()
                    .FirstOrDefault(ourGetInspectorTitleSelector);

                var childReferencesEnumerator = (IChildReferencesEnumerator<TValue>) componentArray;
                foreach (var componentReference in childReferencesEnumerator.GetChildReferences())
                {
                    var componentName = GetComponentName(componentReference, objectNamesType, getInspectorTitleMethod,
                        frame, options, myValueServices);
                    yield return new NamedReferenceDecorator<TValue>(componentReference, componentName,
                            ValueOriginKind.Property, componentType.MetadataType, myValueServices.RoleFactory)
                        .ToValue(myValueServices);
                }
            }

            private string GetComponentName(IValueReference<TValue> componentValue,
                                            [CanBeNull] IReifiedType<TValue> objectNamesType,
                                            [CanBeNull] IMetadataMethodLite getInspectorTitleMethod,
                                            IStackFrame frame,
                                            IValueFetchOptions options, IValueServicesFacade<TValue> services)
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
                            return stringValueRole.GetString();
                    }
                    catch (Exception e)
                    {
                        myLogger.Error(e, "Unable to fetch object names for {0}", componentValue);
                    }
                }

                return componentValue.GetPrimaryRole(options).ReifiedType.MetadataType.ShortName;
            }
        }

        private class GameObjectChildrenGroup : ValueGroupBase
        {
            private readonly IValueReference<TValue> myTransformReference;
            private readonly IValueServicesFacade<TValue> myServices;

            public GameObjectChildrenGroup(IValueReference<TValue> transformReference,
                                           IValueServicesFacade<TValue> services)
                : base("Children")
            {
                myTransformReference = transformReference;
                myServices = services;
            }

            public override IEnumerable<IValueEntity> GetChildren(IPresentationOptions options,
                                                                  CancellationToken token = new CancellationToken())
            {
                var transformObject = myTransformReference.AsObjectSafe(options);
                var childCountRole = transformObject?.GetInstancePropertyReference("childCount", true)
                    ?.AsPrimitiveSafe(options);
                if (childCountRole == null)
                    yield break;

                if (!(childCountRole.GetPrimitive() is int childCount))
                    yield break;

                var transformType =
                    transformObject.ReifiedType.MetadataType.FindTypeThroughHierarchy("UnityEngine.Transform");
                var getChildMethod = transformType?.GetMethods().FirstOrDefault(ourGetChildSelector);
                if (getChildMethod == null)
                    yield break;

                for (int i = 0; i < childCount; i++)
                {
                    var frame = myTransformReference.OriginatingFrame;
                    var index = myServices.ValueFactory.CreatePrimitive(frame, options, i);
                    var childTransform = new SimpleValueReference<TValue>(
                            transformObject.CallInstanceMethod(getChildMethod, index), frame, myServices.RoleFactory)
                        .AsObjectSafe(options);
                    if (childTransform == null)
                        continue;

                    var name = childTransform.GetInstancePropertyReference("name", true)?.AsStringSafe(options)
                        ?.GetString() ?? "Game Object";
                    yield return new NamedReferenceDecorator<TValue>(childTransform.ValueReference, name,
                        ValueOriginKind.Property, transformType, myServices.RoleFactory).ToValue(myServices);
                }
            }
        }
    }
}