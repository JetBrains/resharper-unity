using System.Collections.Generic;
using System.Diagnostics.CodeAnalysis;
using System.Linq;
using JetBrains.Annotations;
using JetBrains.ReSharper.Psi.Tree;
using JetBrains.Util;

#nullable enable

namespace JetBrains.ReSharper.Plugins.Json.Psi.Tree
{
    [PublicAPI]
    public static class JsonNewUtil
    {
        public static IJsonNewObject? GetRootObject(this IJsonNewFile file) => file.Value as IJsonNewObject;

        public static bool IsRootObject([NotNullWhen(true)] this IJsonNewObject? jsonObject) =>
            JsonNewFileNavigator.GetByValue(jsonObject) != null;

        public static bool IsRootProperty([NotNullWhen(true)] this IJsonNewMember? member) =>
            JsonNewObjectNavigator.GetByMember(member).IsRootObject();

        public static bool IsRootPropertyValue([NotNullWhen(true)] this IJsonNewValue? value, string expectedKey)
        {
            var member = JsonNewMemberNavigator.GetByValue(value);
            return member.IsRootProperty() && member.Key == expectedKey;
        }

        [ContractAnnotation("node:null => null")]
        public static IJsonNewLiteralExpression? AsStringLiteralValue(this ITreeNode? node)
        {
            if (node is IJsonNewLiteralExpression { ConstantValueType: ConstantValueTypes.String } literal)
                return literal;
            return null;
        }

        [ContractAnnotation("jsonObject:null => null")]
        public static T? GetFirstPropertyValue<T>(this IJsonNewObject? jsonObject, string key)
            where T : class, IJsonNewValue
        {
            return jsonObject?.MembersEnumerable.FirstOrDefault(member => member.Key == key)?.Value as T;
        }

        [ContractAnnotation("jsonObject:null => null")]
        public static string? GetFirstPropertyValueText(this IJsonNewObject? jsonObject, string key)
        {
            return jsonObject.GetFirstPropertyValue<IJsonNewLiteralExpression>(key)?.GetStringValue();
        }

        [ContractAnnotation("jsonValue:null => null")]
        public static IJsonNewMember? GetNamedMemberByValue(this IJsonNewValue? jsonValue, string key)
        {
            var property = JsonNewMemberNavigator.GetByValue(jsonValue);
            return property?.Key == key ? property : null;
        }

        public static IEnumerable<IJsonNewLiteralExpression> ValuesAsLiteral(this IJsonNewArray? array) =>
            (array?.ValuesEnumerable).SafeOfType<IJsonNewLiteralExpression>();

        public static IEnumerable<IJsonNewObject> ValuesAsObject(this IJsonNewArray? array) =>
            (array?.ValuesEnumerable).SafeOfType<IJsonNewObject>();
    }
}
