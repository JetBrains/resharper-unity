using System.Linq;
using Mono.Debugging.Backend.Values.ValueReferences;

namespace JetBrains.ReSharper.Plugins.Unity.Rider.Debugger.Values.Render.ChildrenRenderers
{
    // GameObject and Component have a number of deprecated properties that always throw. They are marked with
    // [Obsolete] and [EditorBrowsable(EditorBrowsableState.Never)] but not [DebuggerBrowsable(DebuggerBrowsableState.Never)],
    // which the default renderer would handle. So, filter out any properties on this type that are marked as [Obsolete]
    public abstract class DeprecatedPropertyFilteringChildrenRendererBase<TValue> : FilteredObjectChildrenRendererBase<TValue>
        where TValue : class
    {
        public override int Priority => UnityRendererUtil.ChildrenRendererPriority;
        public override bool IsExclusive => true;

        protected override bool IsAllowedReference(IValueReference<TValue> reference)
        {
            if (reference is IMetadataEntityValueReference metadataEntityValueReference &&
                metadataEntityValueReference.Entity != null)
            {
                // We can happily ignore all obsolete properties - GameObject is a sealed class, and we know
                // that all of the deprecated properties have obsolete. We could also check for
                // [EditorBrowsable(EditorBrowsableState.Never)] but that's a bit belt and braces
                return metadataEntityValueReference.Entity.CustomAttributes.All(a =>
                    a.UsedConstructor?.OwnerType.FullName != "System.ObsoleteAttribute");
            }

            return true;
        }
    }
}