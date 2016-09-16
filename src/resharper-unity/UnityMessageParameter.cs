using JetBrains.Annotations;
using JetBrains.Metadata.Reader.API;

namespace JetBrains.ReSharper.Plugins.Unity
{
    public class UnityMessageParameter
    {
        [NotNull]
        public string Name { get; }

        [NotNull]
        public IClrTypeName ClrTypeName { get; }

        public bool IsArray { get; }
        
        public UnityMessageParameter([NotNull] string name, [NotNull] IClrTypeName clrTypeName, bool isArray = false)
        {
            Name = name;
            ClrTypeName = clrTypeName;
            IsArray = isArray;
        }
    }
}