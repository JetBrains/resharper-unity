using System;
using JetBrains.Annotations;

namespace ApiParser
{
    public class ApiType
    {
        public static readonly ApiType Void = new ApiType("void");
        public static readonly ApiType String = new ApiType("string");
        public static readonly ApiType StringArray = new ApiType("string[]");
        public static readonly ApiType Bool = new ApiType("bool");
        public static readonly ApiType StringByRef = new ApiType("string&");

        private readonly Type myType;

        public ApiType([NotNull] string name, string namespaceHint = "")
        {
            if (string.IsNullOrWhiteSpace(name)) name = "void";

            if (name.EndsWith("&"))
            {
                name = name.Substring(0, name.Length - 1);
                IsByRef = true;
            }

            if (name.EndsWith("[]"))
            {
                name = name.Substring(0, name.Length - 2);
                IsArray = true;
            }

            myType = TypeResolver.Resolve(name, namespaceHint);
        }

        public string FullName => myType.FullName;

        public bool IsArray { get; }
        public bool IsByRef { get; }

        public override string ToString()
        {
            return FullName + (IsArray ? "[]" : string.Empty) + (IsByRef ? "&" : string.Empty);
        }
    }
}