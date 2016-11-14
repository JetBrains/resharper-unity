using JetBrains.Annotations;
using JetBrains.Metadata.Reader.API;

namespace JetBrains.ReSharper.Plugins.Unity
{
    public class UnityEventFunctionParameter
    {
        [NotNull]
        public string Name { get; }

        [NotNull]
        public IClrTypeName ClrTypeName { get; }

        public bool IsArray { get; }

        [CanBeNull]
        public string Description { get; }

        public UnityEventFunctionParameter([NotNull] string name, [NotNull] IClrTypeName clrTypeName, [CanBeNull] string description, bool isArray = false)
        {
            Name = name;
            ClrTypeName = clrTypeName;
            Description = description;
            IsArray = isArray;
        }
    }
}