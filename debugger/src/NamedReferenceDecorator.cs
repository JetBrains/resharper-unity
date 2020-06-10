using MetadataLite.API;
using Mono.Debugging.Backend.Values.ValueReferences;
using Mono.Debugging.Backend.Values.ValueRoles;
using Mono.Debugging.Client.CallStacks;
using Mono.Debugging.Client.Values;
using Mono.Debugging.Client.Values.Render;

namespace JetBrains.ReSharper.Plugins.Unity.Rider.Debugger
{
    internal class NamedReferenceDecorator<TValue> : IValueReference<TValue> where TValue : class
    {
        private readonly IValueReference<TValue> myOriginalReference;
        private readonly IValueRoleFactory<TValue> myRoleFactory;

        public NamedReferenceDecorator(IValueReference<TValue> originalReference, string name, ValueOriginKind kind, IMetadataTypeLite declaredType, IValueRoleFactory<TValue> roleFactory)
        {
            myOriginalReference = originalReference;
            DefaultName = name;
            OriginKind = kind;
            DeclaredType = declaredType;
            myRoleFactory = roleFactory;
        }

        public IValueRole GetPrimaryRole(IValueFetchOptions options)
        {
            return myRoleFactory.GetPrimaryRole(this, options);
        }

        public IMetadataTypeLite DeclaredType { get; }

        public string DefaultName { get; }

        public TValue GetValue(IValueFetchOptions options) => myOriginalReference.GetValue(options);

        public void SetValue(TValue value, IValueFetchOptions options) => throw ValueErrors.ReadOnlyReference();

        public bool IsWriteable => false;

        public ValueOriginKind OriginKind { get; }

        public ValueFlags DefaultFlags => ValueFlags.None;

        public IStackFrame OriginatingFrame => myOriginalReference.OriginatingFrame;
    }
}