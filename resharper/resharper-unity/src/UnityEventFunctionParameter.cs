using JetBrains.Annotations;
using JetBrains.Metadata.Reader.API;

namespace JetBrains.ReSharper.Plugins.Unity
{
    public class UnityEventFunctionParameter
    {
        [NotNull]
        public string Name { get; }

        [NotNull]
        public UnityTypeSpec TypeSpec { get; }

        public bool IsByRef { get; }
        public bool IsOptional { get; }
        public string Justification { get; }

        [CanBeNull]
        public string Description { get; }

        public UnityEventFunctionParameter([NotNull] string name, [NotNull] UnityTypeSpec typeSpec,
                                           [CanBeNull] string description, bool isByRef, bool isOptional,
                                           string justification)
        {
            Name = name;
            TypeSpec = typeSpec;
            Description = description;
            IsByRef = isByRef;
            IsOptional = isOptional;
            Justification = justification;
        }
    }
}