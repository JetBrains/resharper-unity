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
            var getRootObjectMethod = valueRole.ReifiedType.MetadataType.GetMethods()
                .FirstOrDefault(ourGetRootGameObjectsSelector);
            if (getRootObjectMethod == null)
                yield break;

            yield return new GameObjectsGroup(valueRole, getRootObjectMethod, ValueServices);
        }

        private class GameObjectsGroup : ValueGroupBase
        {
            private readonly IObjectValueRole<TValue> myValueRole;
            private readonly IMetadataMethodLite myGetRootObjectMethod;
            private readonly IValueServicesFacade<TValue> myValueServices;

            public GameObjectsGroup(IObjectValueRole<TValue> valueRole, IMetadataMethodLite getRootObjectMethod,
                                    IValueServicesFacade<TValue> valueServices)
                : base("Game Objects")
            {
                myValueRole = valueRole;
                myGetRootObjectMethod = getRootObjectMethod;
                myValueServices = valueServices;
            }

            public override IEnumerable<IValueEntity> GetChildren(IPresentationOptions options,
                                                                  CancellationToken token = new CancellationToken())
            {
                var gameObjectsArray = new SimpleValueReference<TValue>(
                        myValueRole.CallInstanceMethod(myGetRootObjectMethod),
                        myValueRole.ValueReference.OriginatingFrame, myValueServices.RoleFactory)
                    .GetExactPrimaryRoleSafe<TValue, IArrayValueRole<TValue>>(options);
                if (gameObjectsArray == null)
                   yield break;

                var childReferencesEnumerator = (IChildReferencesEnumerator<TValue>) gameObjectsArray;
                foreach (var childReference in childReferencesEnumerator.GetChildReferences())
                {
                    var childRole = childReference.AsObjectSafe(options);
                    if (childRole == null)
                        continue;

                    var name = childRole.GetInstancePropertyReference("name", true)?.AsStringSafe(options)
                        ?.GetString() ?? "Game Object";
                    yield return new NamedReferenceDecorator<TValue>(childRole.ValueReference, name,
                            ValueOriginKind.Property, ValueFlags.IsDefaultTypePresentation,
                            childRole.ReifiedType.MetadataType, myValueServices.RoleFactory)
                        .ToValue(myValueServices);
                }
            }
        }
    }
}