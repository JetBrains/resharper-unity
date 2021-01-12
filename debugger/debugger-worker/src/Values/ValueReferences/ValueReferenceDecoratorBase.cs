using JetBrains.Annotations;
using MetadataLite.API;
using Mono.Debugging.Backend.Values.ValueReferences;
using Mono.Debugging.Backend.Values.ValueRoles;
using Mono.Debugging.Client.CallStacks;
using Mono.Debugging.Client.Values.Render;

namespace JetBrains.ReSharper.Plugins.Unity.Rider.Debugger.Values.ValueReferences
{
    internal abstract class ValueReferenceDecoratorBase<TValue> : IValueReference<TValue>
        where TValue : class
    {
        private readonly IValueReference<TValue> myValueReferenceImplementation;
        private readonly IValueRoleFactory<TValue> myRoleFactory;

        protected ValueReferenceDecoratorBase([NotNull] IValueReference<TValue> valueReferenceImplementation,
                                              IValueRoleFactory<TValue> roleFactory)
        {
            myValueReferenceImplementation = valueReferenceImplementation;
            myRoleFactory = roleFactory;
        }

        public IValueReference<TValue> UnderlyingValueReference => myValueReferenceImplementation;

        public virtual IDebuggerHierarchyObject Parent => myValueReferenceImplementation.Parent;

        public virtual IValueRole GetPrimaryRole(IValueFetchOptions options)
        {
            // Get a role based on *this* reference, not the underlying reference. Otherwise, the returned role has the
            // wrong reference.
            return myRoleFactory.GetPrimaryRole(this, options);
        }

        public virtual IMetadataTypeLite DeclaredType => myValueReferenceImplementation.DeclaredType;

        public virtual string DefaultName => myValueReferenceImplementation.DefaultName;

        public virtual TValue GetValue(IValueFetchOptions options)
        {
            return myValueReferenceImplementation.GetValue(options);
        }

        public virtual void SetValue(TValue value, IValueFetchOptions options)
        {
            myValueReferenceImplementation.SetValue(value, options);
        }

        public virtual bool IsWriteable => myValueReferenceImplementation.IsWriteable;

        public virtual ValueOriginKind OriginKind => myValueReferenceImplementation.OriginKind;

        public virtual ValueFlags DefaultFlags => myValueReferenceImplementation.DefaultFlags;

        public virtual IStackFrame OriginatingFrame => myValueReferenceImplementation.OriginatingFrame;
    }
}