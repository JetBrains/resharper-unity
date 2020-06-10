using System.Collections.Generic;
using System.Linq;
using System.Threading;
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

namespace JetBrains.ReSharper.Plugins.Unity.Rider.Debugger
{
    [DebuggerSessionComponent(typeof(SoftDebuggerType))]
    public class SceneRootChildrenRenderer<TValue> : ChildrenRendererBase<TValue, IObjectValueRole<TValue>> where TValue : class
    {
        private static MethodSelector GetRootGameObjectsSelector = new MethodSelector(m => m.Name == "GetRootGameObjects" && m.Parameters.Length == 0);
        
        protected override IEnumerable<IValueEntity> GetChildren(IObjectValueRole<TValue> valueRole, IMetadataTypeLite instanceType, IPresentationOptions options, IUserDataHolder dataHolder, CancellationToken token)
        {
            var getRootObjectMethod = valueRole.ReifiedType.MetadataType.GetMethods().FirstOrDefault(GetRootGameObjectsSelector);
            if (getRootObjectMethod == null)
                yield break;
            var gameObjectsArray = new SimpleValueReference<TValue>(valueRole.CallInstanceMethod(getRootObjectMethod), valueRole.ValueReference.OriginatingFrame, ValueServices.RoleFactory)
                .GetExactPrimaryRoleSafe<TValue, IArrayValueRole<TValue>>(options);
            if (gameObjectsArray == null)
                yield break;
            yield return new GameObjectsGroup(gameObjectsArray, ValueServices);
        }

        protected override bool IsApplicable(IMetadataTypeLite type, IPresentationOptions options, IUserDataHolder dataHolder) => type.Is("UnityEngine.SceneManagement.Scene");

        public override int Priority => 100;
        
        private class GameObjectsGroup : IValueGroup
        {
            private readonly IArrayValueRole<TValue> myGameObjectsArray;
            private readonly IValueServicesFacade<TValue> myValueServices;

            public GameObjectsGroup(IArrayValueRole<TValue> gameObjectsArray, IValueServicesFacade<TValue> valueServices)
            {
                myGameObjectsArray = gameObjectsArray;
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
                var childReferencesEnumerator = (IChildReferencesEnumerator<TValue>) myGameObjectsArray;
                foreach (var childReference in childReferencesEnumerator.GetChildReferences())
                {
                    var childRole = childReference.AsObjectSafe(options);
                    if (childRole == null)
                        continue;
                    var name = childRole?.GetInstancePropertyReference("name", true)?.AsStringSafe(options)?.GetString();
                    yield return new NamedReferenceDecorator<TValue>(childRole.ValueReference, name, ValueOriginKind.Property, childRole.ReifiedType.MetadataType, myValueServices.RoleFactory).ToValue(myValueServices);
                }
            }

            public string SimpleName => "Game Objects";

            public bool IsTop => true;
        }
    }
}