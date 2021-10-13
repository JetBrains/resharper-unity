using System.Collections.Generic;
using System.Linq;
using JetBrains.Annotations;
using JetBrains.Util;

#nullable enable

namespace JetBrains.ReSharper.Plugins.Unity.JsonNew.Psi.Tree
{
    public static class JsonNewUtil
    {
        public static IJsonNewObject? GetRootObject(this IJsonNewFile file) => file.Value as IJsonNewObject;

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

        public static IEnumerable<IJsonNewLiteralExpression> ValuesAsLiteral(this IJsonNewArray? array) =>
            (array?.ValuesEnumerable).SafeOfType<IJsonNewLiteralExpression>();

        public static IEnumerable<IJsonNewObject> ValuesAsObject(this IJsonNewArray? array) =>
            (array?.ValuesEnumerable).SafeOfType<IJsonNewObject>();
    }
}