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
        public static readonly ApiType IEnumerator = new ApiType("System.Collections.IEnumerator");

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

            FullName = name.Contains(".") ? name : TypeResolver.ResolveFullName(name, namespaceHint);
        }

        public string FullName { get; }

        public bool IsArray { get; }
        public bool IsByRef { get; }

        public override string ToString()
        {
            return FullName + (IsArray ? "[]" : string.Empty) + (IsByRef ? "&" : string.Empty);
        }

        protected bool Equals(ApiType other)
        {
            return string.Equals(FullName, other.FullName) && IsArray == other.IsArray && IsByRef == other.IsByRef;
        }

        public override bool Equals(object obj)
        {
            if (ReferenceEquals(null, obj)) return false;
            if (ReferenceEquals(this, obj)) return true;
            if (obj.GetType() != this.GetType()) return false;
            return Equals((ApiType) obj);
        }

        public override int GetHashCode()
        {
            unchecked
            {
                var hashCode = (FullName != null ? FullName.GetHashCode() : 0);
                hashCode = (hashCode * 397) ^ IsArray.GetHashCode();
                hashCode = (hashCode * 397) ^ IsByRef.GetHashCode();
                return hashCode;
            }
        }
    }
}