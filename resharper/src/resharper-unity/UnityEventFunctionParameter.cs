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
        public bool IsByRef { get; }
        public bool IsOptional { get; }
        public string Justification { get; }

        [CanBeNull]
        public string Description { get; }

        public UnityEventFunctionParameter([NotNull] string name, [NotNull] IClrTypeName clrTypeName, [CanBeNull] string description, bool isArray, bool isByRef, bool isOptional, string justification)
        {
            Name = name;
            ClrTypeName = clrTypeName;
            Description = description;
            IsArray = isArray;
            IsByRef = isByRef;
            IsOptional = isOptional;
            Justification = justification;
        }
    }
}