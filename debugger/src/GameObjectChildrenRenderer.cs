using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using JetBrains.Annotations;
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

namespace JetBrains.ReSharper.Plugins.Unity.Rider.Debugger
{
    [DebuggerSessionComponent(typeof(SoftDebuggerType))]
    public class GameObjectChildrenRenderer<TValue> : ChildrenRendererBase<TValue, IObjectValueRole<TValue>> where TValue : class
    {
        private static readonly MethodSelector GetChildSelector = new MethodSelector(m => m.Name == "GetChild" 
                                                                                 && m.Parameters.Length == 1 && m.Parameters[0].Type.Is("System.Int32"));
        
        private static readonly MethodSelector GetComponentsSelector = new MethodSelector(m => m.Name == "GetComponents" 
                                                                                               && m.Parameters.Length == 1 && m.Parameters[0].Type.Is("System.Type"));
        
        private static readonly MethodSelector GetInspectorTitleSelector = new MethodSelector(m => m.Name == "GetInspectorTitle" 
                                                                                           && m.Parameters.Length == 1 && m.Parameters[0].Type.Is("UnityEngine.Object"));

        protected override IEnumerable<IValueEntity> GetChildren(IObjectValueRole<TValue> valueRole, IMetadataTypeLite instanceType, IPresentationOptions options, IUserDataHolder dataHolder, CancellationToken token)
        {
            yield return new GameObjectComponentsGroup(valueRole, ValueServices);
            var transformProperty = valueRole.GetInstancePropertyReference("transform");
            if (transformProperty != null)
            {
                yield return new GameObjectChildrenGroup(transformProperty, ValueServices);
            }
        }

        protected override bool IsApplicable(IMetadataTypeLite type, IPresentationOptions options, IUserDataHolder dataHolder)
        {
            return type.Is("UnityEngine.GameObject");
        }

        public override int Priority => 100;
        
        private class GameObjectComponentsGroup : IValueGroup
        {
            private readonly IObjectValueRole<TValue> myGameObjectRole;
            private readonly IValueServicesFacade<TValue> myValueServices;

            public GameObjectComponentsGroup(IObjectValueRole<TValue> gameObjectRole, IValueServicesFacade<TValue> valueServices)
            {
                myGameObjectRole = gameObjectRole;
                myValueServices = valueServices;
            }

            public IValueKeyPresentation GetKeyPresentation(IPresentationOptions options, CancellationToken token = new CancellationToken())
            {
                return new ValueKeyPresentation(SimpleName, ValueOriginKind.Group, ValueFlags.None);
            }

            public IValuePresentation GetValuePresentation(IPresentationOptions options, CancellationToken token = new CancellationToken())
            {
                return SimplePresentation.EmptyPresentation;
            }

            public IEnumerable<IValueEntity> GetChildren(IPresentationOptions options, CancellationToken token = new CancellationToken())
            {
                var frame = myGameObjectRole.ValueReference.OriginatingFrame;
                var componentType = myValueServices.TypeUniverse.GetReifiedType(frame, "UnityEngine.Component, UnityEngine.CoreModule");
                if (componentType == null)
                    yield break;
                var typeObject = ((IValueReference<TValue>) componentType.GetTypeObject(frame));
                var getComponentMethod = myGameObjectRole.ReifiedType.MetadataType.GetMethods().FirstOrDefault(GetComponentsSelector);
                if (getComponentMethod == null)
                    yield break;
                var componentArray = new SimpleValueReference<TValue>(myGameObjectRole.CallInstanceMethod(getComponentMethod, typeObject.GetValue(options)), frame, myValueServices.RoleFactory)
                    .GetExactPrimaryRoleSafe<TValue, IArrayValueRole<TValue>>(options);
                if (componentArray == null)
                    yield break;
                var objectNamesType = ((IReifiedType<TValue>) myValueServices.TypeUniverse.GetReifiedType(frame, "UnityEditor.ObjectNames"));

                var childReferencesEnumerator = ((IChildReferencesEnumerator<TValue>)componentArray);
                foreach (var componentReference in childReferencesEnumerator.GetChildReferences())
                {
                    var componentName = GetComponentName(componentReference, objectNamesType, frame, options, myValueServices);
                    yield return new NamedReferenceDecorator<TValue>(componentReference, componentName, ValueOriginKind.Property, componentType.MetadataType, myValueServices.RoleFactory)
                        .ToValue(myValueServices);
                }
            }

            private string GetComponentName(IValueReference<TValue> componentValue, [CanBeNull] IReifiedType<TValue> objectNamesType, IStackFrame frame, IValueFetchOptions options, IValueServicesFacade<TValue> services)
            {
                if (objectNamesType != null)
                {
                    try
                    {
                        var stringValueRole = new SimpleValueReference<TValue>(objectNamesType.CallStaticMethod(frame, options, GetInspectorTitleSelector, componentValue.GetValue(options)),
                            frame, services.RoleFactory).AsStringSafe(options);
                        if (stringValueRole != null)
                            return stringValueRole.GetString();
                    }
                    catch (Exception)
                    {
                        // TODO:
                        // ourLogger.Error(e, "Unable to fetch object names for {0}", componentValue);
                    }
                }

                return componentValue.GetPrimaryRole(options).ReifiedType.MetadataType.FullName;
            }
            
            public string SimpleName => "Components";

            public bool IsTop => true;
        }
        
        private class GameObjectChildrenGroup : IValueGroup
        {
            private readonly IValueReference<TValue> myTransformReference;
            private readonly IValueServicesFacade<TValue> myServices;

            public GameObjectChildrenGroup(IValueReference<TValue> transformReference, IValueServicesFacade<TValue> services)
            {
                myTransformReference = transformReference;
                myServices = services;
            }

            public IValueKeyPresentation GetKeyPresentation(IPresentationOptions options, CancellationToken token = new CancellationToken())
            {
                return new ValueKeyPresentation(SimpleName, ValueOriginKind.Group, ValueFlags.None);
            }

            public IValuePresentation GetValuePresentation(IPresentationOptions options, CancellationToken token = new CancellationToken())
            {
                return SimplePresentation.EmptyPresentation;
            }

            public IEnumerable<IValueEntity> GetChildren(IPresentationOptions options, CancellationToken token = new CancellationToken())
            {
                var transformObject = myTransformReference.AsObjectSafe(options);
                if (transformObject == null)
                    yield break;

                var childCountRole = transformObject.GetInstancePropertyReference("childCount", true)?.AsPrimitiveSafe(options);
                if (childCountRole == null)
                    yield break;
                
                if (!(childCountRole.GetPrimitive() is int childCount))
                    yield break;
                

                var transformType = transformObject.ReifiedType.MetadataType.FindTypeThroughHierarchy("UnityEngine.Transform");
                var getChildMethod = transformType?.GetMethods().FirstOrDefault(GetChildSelector);
                if (getChildMethod == null)
                    yield break;

                for (int i = 0; i < childCount; i++)
                {
                    var frame = myTransformReference.OriginatingFrame;
                    var index = myServices.ValueFactory.CreatePrimitive(frame, options, i);
                    var childTransform = new SimpleValueReference<TValue>(transformObject.CallInstanceMethod(getChildMethod, index), frame, myServices.RoleFactory)
                        .AsObjectSafe(options);
                    if (childTransform == null)
                        continue;
                    // TODO do we need to take a name of childTransform.gameObject instead of childTransform itself 
                    var name = childTransform.GetInstancePropertyReference("name", true)?.AsStringSafe(options)?.GetString();
                    yield return new NamedReferenceDecorator<TValue>(childTransform.ValueReference, name ?? "Game Object", ValueOriginKind.Property, transformType, myServices.RoleFactory).ToValue(myServices);
                }
            }

            public string SimpleName => "Children";

            public bool IsTop => true;
        }
    }
}