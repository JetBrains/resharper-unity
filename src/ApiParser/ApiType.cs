using System;
using JetBrains.Annotations;

namespace ApiParser
{
    public class ApiType
    {
        private readonly Type _type;

        public ApiType([NotNull] string name)
        {
            if (string.IsNullOrWhiteSpace(name)) name = "void";

            if (name.EndsWith("[]"))
            {
                name = name.Substring(0, name.Length - 2);
                IsArray = true;
            }

            _type = TypeResolver.Resolve(name);
        }

        public string FullName => _type.FullName;

        public bool IsArray { get; }

        [NotNull]
        public string Identifier => TypeKeyResolver.Resolve(_type);
    }
}