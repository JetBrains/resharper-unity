using JetBrains.Annotations;
using Mono.Debugging.Backend.Values.ValueReferences;
using Mono.Debugging.Backend.Values.ValueRoles;
using Mono.Debugging.Client.Values;
using Mono.Debugging.Client.Values.Render;

namespace JetBrains.ReSharper.Plugins.Unity.Rider.Debugger.Values.ValueReferences
{
    internal class CalculatedValueReferenceDecorator<TValue> : ValueReferenceDecoratorBase<TValue>
        where TValue : class
    {
        public CalculatedValueReferenceDecorator([NotNull] IValueReference<TValue> valueReferenceImplementation,
                                                 IValueRoleFactory<TValue> roleFactory,
                                                 string name,
                                                 bool allowNameInValue = true,
                                                 bool allowDefaultTypePresentation = true)
            : base(valueReferenceImplementation, roleFactory)
        {
            DefaultName = name;
            AllowNameInValue = allowNameInValue;
            AllowDefaultTypePresentation = allowDefaultTypePresentation;
        }

        // Returns false if the name of this reference is already used in the key. E.g. "My Component = {GameObject} My Component"
        // Avoids the unnecessary repetition and noise
        public bool AllowNameInValue { get; }

        public bool AllowDefaultTypePresentation { get; }

        public override string DefaultName { get; }

        // Calculated value, must be read only
        public override ValueOriginKind OriginKind => ValueOriginKind.Other;
        public override ValueFlags DefaultFlags => ValueFlags.None | ValueFlags.IsReadOnly;
        public override bool IsWriteable => false;

        public override void SetValue(TValue value, IValueFetchOptions options) =>
            throw ValueErrors.ReadOnlyReference();
    }
}