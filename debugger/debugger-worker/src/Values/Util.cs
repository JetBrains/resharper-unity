using JetBrains.Annotations;
using Mono.Debugging.Backend.Values;
using Mono.Debugging.Client.CallStacks;
using TypeSystem;

namespace JetBrains.ReSharper.Plugins.Unity.Rider.Debugger.Values
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
    }
}