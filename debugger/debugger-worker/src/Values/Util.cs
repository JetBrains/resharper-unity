using JetBrains.Annotations;
using Mono.Debugging.Backend.Values;
using Mono.Debugging.Backend.Values.ValueReferences;
using Mono.Debugging.Backend.Values.ValueRoles;
using Mono.Debugging.Client.CallStacks;
using Mono.Debugging.Client.Values.Render;
using TypeSystem;

namespace JetBrains.Debugger.Worker.Plugins.Unity.Values
{
    public static class Util
    {
        [CanBeNull]
        public static IReifiedType<TValue> GetReifiedType<TValue>(this IValueServicesFacade<TValue> valueServices,
                                                                  IStackFrame frame, string aqn)
            where TValue : class
        {
            return (IReifiedType<TValue>) valueServices.TypeUniverse.GetReifiedType(frame, aqn);
        }

        public static bool TryEvaluatePrimitiveProperty<TValue, TPrimitive>(IObjectValueRole<TValue> valueRole,
                                                                            string property, IValueFetchOptions options,
                                                                            out TPrimitive value)
            where TValue : class
            where TPrimitive : struct
        {
            value = default;
            var primitiveValueRole = valueRole.GetInstancePropertyReference(property)?.AsPrimitiveSafe(options);
            if (!(primitiveValueRole?.GetPrimitive() is TPrimitive primitive))
                return false;
            value = primitive;
            return true;
        }
    }
}