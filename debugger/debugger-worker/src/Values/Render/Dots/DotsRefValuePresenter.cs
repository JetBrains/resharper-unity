using System.Collections.Generic;
using System.Threading;
using JetBrains.Debugger.Worker.Plugins.Unity.Values.Render.ChildrenRenderers;
using JetBrains.Util;
using Mono.Debugger.Soft;
using Mono.Debugging.Autofac;
using Mono.Debugging.Backend.Values.Render.ChildrenRenderers;
using Mono.Debugging.Backend.Values.ValueReferences;
using Mono.Debugging.Backend.Values.ValueRoles;
using Mono.Debugging.Client.Values;
using Mono.Debugging.Client.Values.Render;
using Mono.Debugging.MetadataLite.API;
using Mono.Debugging.Soft;
using Mono.Debugging.Win32;

namespace JetBrains.Debugger.Worker.Plugins.Unity.Values.Render.Dots
{
    internal static class DotsUnityConstants
    {
        internal const string ValueRoPropertyName = "ValueRO";
        internal const string IsValidPropertyName = "IsValid";

        public static readonly HashSet<string> SupportedRefTypes = new(new []
        {
            "Unity.Entities.RefRO`1", 
            "Unity.Entities.RefRW`1",
            "Unity.Entities.EnabledRefRO`1",
            "Unity.Entities.EnabledRefRW`1",
        }); 

        public static readonly HashSet<string> SupportedInternalRefTypes = new(new []
        {
            "Unity.Entities.InternalCompilerInterface+UncheckedRefRO`1",
            "Unity.Entities.InternalCompilerInterface+UncheckedRefRW`1",

            //"com.unity.entities": "1.0.16"
            "Unity.Entities.Internal.InternalCompilerInterface+UncheckedRefRO`1",
            "Unity.Entities.Internal.InternalCompilerInterface+UncheckedRefRW`1",
        });
    }

    [DebuggerSessionComponent(typeof(SoftDebuggerType))]
    public class MonoDotsRefValuePresenter : DotsRefValuePresenter<Value>
    {
        public MonoDotsRefValuePresenter(IUnityOptions unityOptions) : base(unityOptions)
        {
        }
    }

    [DebuggerSessionComponent(typeof(CorDebuggerType))]
    public class CorDotsRefValuePresenter : DotsRefValuePresenter<ICorValue>
    {
        public CorDotsRefValuePresenter(IUnityOptions unityOptions) : base(unityOptions)
        {
        }
    }
    
    public class DotsRefValuePresenter<TValue> : FilteredObjectChildrenRendererBase<TValue>
        where TValue : class
    {
        public override int Priority => UnityRendererUtil.ChildrenRendererPriority;

        private readonly IUnityOptions myUnityOptions;


        protected DotsRefValuePresenter(IUnityOptions unityOptions)
        {
            myUnityOptions = unityOptions;
        }

        protected override IEnumerable<IValueEntity> GetChildren(IObjectValueRole<TValue> valueRole, IMetadataTypeLite instanceType, IPresentationOptions options,
            IUserDataHolder dataHolder, CancellationToken token)
        {
            var genericTypeName = instanceType.GetGenericTypeDefinition().FullName;
            if(!DotsUnityConstants.SupportedInternalRefTypes.Contains(genericTypeName))
            {
                var isValidProperty = valueRole.GetInstancePropertyReference(DotsUnityConstants.IsValidPropertyName);
                if (isValidProperty == null)
                    Logger.Warn("Unable to retrieve IsValid property");
                else
                    yield return isValidProperty.ToValue(ValueServices);
            }
            
            IValueReference<TValue>? valueRoRef = valueRole.GetInstancePropertyReference(DotsUnityConstants.ValueRoPropertyName);
            if (valueRoRef == null)
            {
                Logger.Warn("Unable to retrieve ValueRO property");
            }
            else
            {
                var valueRoRefRole = valueRoRef.GetPrimaryRole(options);

                // in CoreCLR the refs are not automatically dereferenced, so we do it here to get the underlying value
                if (valueRoRefRole is IPointerLikeValueRole<TValue> pointerLike)
                {
                    valueRoRef = pointerLike.UnderlyingValueReference;
                    valueRoRefRole = valueRoRef.GetPrimaryRole(options);
                }

                if (valueRoRefRole is IObjectValueRole<TValue> objectValueRole)
                {
                    var children = options.FlattenHierarchy
                        ? ChildrenRenderingUtil.EnumerateMembersFlat(objectValueRole, options, token, ValueServices)
                        : ChildrenRenderingUtil.EnumerateMembersWithBaseNode(objectValueRole, options, token,
                            ValueServices);

                    foreach (var child in children)
                        yield return child;
                }
                else
                {
                    yield return valueRoRef.ToValue(ValueServices);
                }
            }

            // Disable debugger type proxy options to avoid recursion. See IsApplicable.
            var rawViewOptions = options.WithOverridden(o => o.EvaluateDebuggerTypeProxy = false);
            yield return new SimpleEntityGroup(PresentationOptions.RawViewGroupName,
                base.GetChildren(valueRole, instanceType, rawViewOptions, dataHolder, token));
        }

        protected override bool IsApplicable(IMetadataTypeLite type, IPresentationOptions options, IUserDataHolder dataHolder)
        {
            var genericTypeName = type.GetGenericTypeDefinition().FullName;
            return myUnityOptions.ExtensionsEnabled && 
                   ( DotsUnityConstants.SupportedRefTypes.Contains(genericTypeName)
                     || DotsUnityConstants.SupportedInternalRefTypes.Contains(genericTypeName));
        }
    }
}