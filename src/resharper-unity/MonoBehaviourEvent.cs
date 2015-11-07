using JetBrains.Annotations;
using JetBrains.Metadata.Reader.API;
using JetBrains.Util;

namespace JetBrains.ReSharper.Plugins.Unity
{
    public class MonoBehaviourEvent
    {
        [NotNull]
        public string Name { get; }

        [NotNull]
        public MonoBehaviourEventParameter[] Parameters { get; }

        public MonoBehaviourEvent([NotNull] string name, [NotNull] params MonoBehaviourEventParameter[] parameters)
        {
            Name = name;
            Parameters = parameters.Length > 0 ? parameters : EmptyArray<MonoBehaviourEventParameter>.Instance;
        }
    }

    public class MonoBehaviourEventParameter
    {
        [NotNull]
        public string Name { get; }

        [NotNull]
        public IClrTypeName ClrTypeName { get; }

        public bool IsArray { get; }
        
        public MonoBehaviourEventParameter([NotNull] string name, [NotNull] IClrTypeName clrTypeName, bool isArray = false)
        {
            Name = name;
            ClrTypeName = clrTypeName;
            IsArray = isArray;
        }
    }
}