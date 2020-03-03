using JetBrains.Annotations;

namespace ApiParser
{
    public class ApiType
    {
        public static readonly ApiType Void = new ApiType("System.Void");
        public static readonly ApiType String = new ApiType("System.String");
        public static readonly ApiType StringArray = new ApiType("System.String[]");
        public static readonly ApiType Bool = new ApiType("System.Boolean");
        public static readonly ApiType StringByRef = new ApiType("System.String&");
        public static readonly ApiType IEnumerator = new ApiType("System.Collections.IEnumerator");
        
        public ApiType([NotNull] string fullName, bool isObsolete = false)
        {
            if (string.IsNullOrWhiteSpace(fullName)) fullName = "void";

            if (fullName.EndsWith("&"))
            {
                fullName = fullName.Substring(0, fullName.Length - 1);
                IsByRef = true;
            }

            if (fullName.EndsWith("[]"))
            {
                fullName = fullName.Substring(0, fullName.Length - 2);
                IsArray = true;
            }

            FullName = fullName;
            IsObsolete = isObsolete;
        }

        public string FullName { get; }

        public bool IsArray { get; }
        public bool IsByRef { get; }
        public bool IsObsolete { get; }

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