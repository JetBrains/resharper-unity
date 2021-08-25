using Mono.Debugging.Backend.Values.ValueReferences;
using Mono.Debugging.Backend.Values.ValueRoles;

namespace JetBrains.Debugger.Worker.Plugins.Unity.Values.ValueReferences
{
    // Decorator that is effectively a marker class for TextValuePresenter. Used to present a string value as plain text
    internal class TextValueReference<TValue> : ValueReferenceDecoratorBase<TValue>
        where TValue : class
    {
        public TextValueReference(IValueReference<TValue> valueReferenceImplementation,
                                  IValueRoleFactory<TValue> roleFactory)
            : base(valueReferenceImplementation, roleFactory)
        {
        }
    }
}