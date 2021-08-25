using JetBrains.Annotations;
using Mono.Debugging.Backend.Values.ValueReferences;
using Mono.Debugging.Backend.Values.ValueRoles;

namespace JetBrains.Debugger.Worker.Plugins.Unity.Values.ValueReferences
{
    internal class ExtraDetailValueReferenceDecorator<TValue> : ValueReferenceDecoratorBase<TValue>
        where TValue : class
    {
        public ExtraDetailValueReferenceDecorator([NotNull] IValueReference<TValue> valueReferenceImplementation,
                                                  IValueRoleFactory<TValue> roleFactory,
                                                  string extraDetail)
            : base(valueReferenceImplementation, roleFactory)
        {
            ExtraDetail = extraDetail;
        }

        public string ExtraDetail { get; }
    }
}