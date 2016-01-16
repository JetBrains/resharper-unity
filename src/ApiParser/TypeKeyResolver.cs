using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using System.Text.RegularExpressions;
using JetBrains.Annotations;
using JetBrains.Metadata.Reader.API;
using JetBrains.ReSharper.Psi;

namespace ApiParser
{
    public static class TypeKeyResolver
    {
        private static readonly Regex CamelCut = new Regex("(?<!(^|[A-Z0-9]))(?=[A-Z0-9])|(?<!^)(?=[A-Z][a-z])");
        private static readonly Dictionary<Type, string> Entries = new Dictionary<Type, string>();
        private static readonly Dictionary<Type, string> Predefined = new Dictionary<Type, string>();

        static TypeKeyResolver()
        {
            FieldInfo[] fields = typeof(PredefinedType).GetFields(BindingFlags.Static | BindingFlags.Public);
            FieldInfo[] matching = fields.Where(f => typeof(IClrTypeName).IsAssignableFrom(f.FieldType)).ToArray();
            Type[] types = matching.Select(f => Type.GetType(((IClrTypeName)f.GetValue(null)).FullName)).ToArray();

            for (var i = 0; i < types.Length; ++i)
            {
                if (types[i] != null) Predefined[types[i]] = matching[i].Name;
            }
        }

        [NotNull]
        public static IEnumerable<KeyValuePair<Type, string>> CustomEntries => Entries;

        [NotNull]
        public static string Resolve([NotNull] Type type)
        {
            if (Entries.ContainsKey(type)) return Entries[type];
            if (Predefined.ContainsKey(type))
            {
                return typeof(PredefinedType).FullName + "." + Predefined[type];
            }

            return Entries[type] = string.Join("_", CamelCut.Split(type.Name)).ToUpper() + "_FQN";
        }
    }
}