using System.Linq;
using JetBrains.Annotations;

#nullable enable

namespace JetBrains.ReSharper.Plugins.Unity.JsonNew.Psi.Tree
{
    public static class JsonNewUtil
    {
        public static IJsonNewObject? GetRootObject(this IJsonNewFile file)
        {
            return file.Value as IJsonNewObject;
        }


        [ContractAnnotation("jsonObject:null => null")]
        public static T? GetFirstPropertyValue<T>(this IJsonNewObject? jsonObject, string key)
            where T : class, IJsonNewValue
        {
            return jsonObject?.MembersEnumerable.FirstOrDefault(member => member.Key == key)?.Value as T;
        }
    }
}