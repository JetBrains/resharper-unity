using Mono.Debugging.Backend.Values.ValueReferences;
using Mono.Debugging.Backend.Values.ValueRoles;
using Mono.Debugging.Client.Values.Render;

namespace JetBrains.Debugger.Worker.Plugins.Unity.Values
{
    public static class SerializedPropertyHelper
    {
        public static string GetEnumValueIndexAsEnumName<TValue>(IObjectValueRole<TValue> serializedProperty,
                                                                 IValueReference<TValue> enumValueIndexReference,
                                                                 IPresentationOptions options)
            where TValue : class
        {
            var primitiveValue = enumValueIndexReference.AsPrimitiveSafe(options)?.GetPrimitive();
            if (primitiveValue is int enumValueIndex
                && serializedProperty.GetInstancePropertyReference("enumNames")?.GetPrimaryRole(options) is
                    IArrayValueRole<TValue> enumNamesArray)
            {
                return enumNamesArray.GetElementReference(enumValueIndex).AsStringSafe(options)?.GetString();
            }

            return null;
        }

        public static string GetIntValueAsPrintableChar<TValue>(IValueReference<TValue> intValueReference,
                                                                IValueFetchOptions options)
            where TValue : class
        {
            var primitiveValue = intValueReference.AsPrimitiveSafe(options)?.GetPrimitive();
            if (primitiveValue != null)
            {
                var value = primitiveValue as long? ?? primitiveValue as int?;
                if (value < char.MaxValue)
                    return $"'{ToPrintableChar((char) value)}'";
            }

            return null;
        }

        private static string ToPrintableChar(char value)
        {
            switch (value)
            {
                case '\'': return @"\'";
                case '\"': return @"\""";
                case '\\': return @"\\";
                case '\0': return @"\0";
                case '\a': return @"\a";
                case '\b': return @"\b";
                case '\f': return @"\f";
                case '\n': return @"\n";
                case '\r': return @"\r";
                case '\t': return @"\t";
                case '\v': return @"\v";
            }

            if (char.IsControl(value))
            {
                // Format as hex, the integer value is most likely being displayed as decimal
                return $"\\u{(ushort) value:x4}";
            }

            return value.ToString();
        }
    }
}