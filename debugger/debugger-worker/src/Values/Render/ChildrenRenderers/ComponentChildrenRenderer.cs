using JetBrains.Util;
using MetadataLite.API;
using Mono.Debugging.Autofac;
using Mono.Debugging.Client.Values.Render;
using Mono.Debugging.Soft;

namespace JetBrains.ReSharper.Plugins.Unity.Rider.Debugger.Values.Render.ChildrenRenderers
{
    [DebuggerSessionComponent(typeof(SoftDebuggerType))]
    public class ComponentChildrenRenderer<TValue> : DeprecatedPropertyFilteringChildrenRendererBase<TValue>
        where TValue : class
    {
        private readonly IUnityOptions myUnityOptions;

        public ComponentChildrenRenderer(IUnityOptions unityOptions)
        {
            myUnityOptions = unityOptions;
        }

        protected override bool IsApplicable(IMetadataTypeLite type, IPresentationOptions options, IUserDataHolder dataHolder)
        {
            return myUnityOptions.ExtensionsEnabled && type.FindTypeThroughHierarchy("UnityEngine.Component") != null;
        }
    }
}