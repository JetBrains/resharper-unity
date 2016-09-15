using JetBrains.Annotations;
using JetBrains.Metadata.Reader.API;
using JetBrains.Util;

namespace JetBrains.ReSharper.Plugins.Unity
{
    public class UnityTypeEvent
    {
        [NotNull]
        public string Name { get; }

        [NotNull]
        public UnityTypeParameter[] Parameters { get; }

        public UnityTypeEvent([NotNull] string name, [NotNull] params UnityTypeParameter[] parameters)
        {
            Name = name;
            Parameters = parameters.Length > 0 ? parameters : EmptyArray<UnityTypeParameter>.Instance;
        }
    }

    public class UnityTypeParameter
    {
        [NotNull]
        public string Name { get; }

        [NotNull]
        public IClrTypeName ClrTypeName { get; }

        public bool IsArray { get; }
        
        public UnityTypeParameter([NotNull] string name, [NotNull] IClrTypeName clrTypeName, bool isArray = false)
        {
            Name = name;
            ClrTypeName = clrTypeName;
            IsArray = isArray;
        }
    }
}