using System.Collections.Generic;
using System.Threading;
using JetBrains.Debugger.Worker.Plugins.Unity.Values.Render.ChildrenRenderers;
using JetBrains.Util;
using Mono.Debugging.Autofac;
using Mono.Debugging.Backend.Values.Render.ChildrenRenderers;
using Mono.Debugging.Backend.Values.ValueReferences;
using Mono.Debugging.Backend.Values.ValueRoles;
using Mono.Debugging.Client.Values;
using Mono.Debugging.Client.Values.Render;
using Mono.Debugging.MetadataLite.API;
using Mono.Debugging.Soft;

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
            "Unity.Entities.EnabledRefRW`1"
        }); 
    }
    
    [DebuggerSessionComponent(typeof(SoftDebuggerType))]
    public class DotsRefValuePresenter<TValue> : FilteredObjectChildrenRendererBase<TValue>
        where TValue : class
    {
        public override int Priority => UnityRendererUtil.ChildrenRendererPriority;

        private readonly IUnityOptions myUnityOptions;

 
        public DotsRefValuePresenter(IUnityOptions unityOptions)
        {
            myUnityOptions = unityOptions;
        }

        protected override IEnumerable<IValueEntity> GetChildren(IObjectValueRole<TValue> valueRole, IMetadataTypeLite instanceType, IPresentationOptions options,
            IUserDataHolder dataHolder, CancellationToken token)
        {
            //Get all children from ValueRO property
            var isValidProperty = valueRole.GetInstancePropertyReference(DotsUnityConstants.IsValidPropertyName);
            if (isValidProperty == null)
                Logger.Warn("Unable to retrieve IsValid property");
            else
                yield return isValidProperty.ToValue(ValueServices);

            
            var valueRoRefRole = valueRole.GetInstancePropertyReference(DotsUnityConstants.ValueRoPropertyName)?.AsObjectSafe(options);

            if (valueRoRefRole == null)
            {
                Logger.Warn("Unable to retrieve ValueRO property");
            }
            else
            {
                var children = options.FlattenHierarchy
                    ? ChildrenRenderingUtil.EnumerateMembersFlat(valueRoRefRole, options, token, ValueServices)
                    : ChildrenRenderingUtil.EnumerateMembersWithBaseNode(valueRoRefRole, options, token, ValueServices);
            
                foreach (var child in children)
                    yield return child;
            }

            // Disable debugger type proxy options to avoid recursion. See IsApplicable.
            var rawViewOptions = options.WithOverridden(o => o.EvaluateDebuggerTypeProxy = false);
            yield return new SimpleEntityGroup(PresentationOptions.RawViewGroupName,
                base.GetChildren(valueRole, instanceType, rawViewOptions, dataHolder, token));
        }

        protected override bool IsApplicable(IMetadataTypeLite type, IPresentationOptions options, IUserDataHolder dataHolder)
        {
            return myUnityOptions.ExtensionsEnabled && 
                   DotsUnityConstants.SupportedRefTypes.Contains(type.GetGenericTypeDefinition().FullName);
        }
    }
}