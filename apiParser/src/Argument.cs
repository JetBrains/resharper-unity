using JetBrains.Annotations;

namespace ApiParser
{
    public class Argument
    {
        public Argument(ApiType type, string name)
        {
            Name = name;
            Type = type;
        }

        [CanBeNull]
        public string Description { get; set; }

        [NotNull]
        public string Name { get; set; }

        [NotNull]
        public ApiType Type { get; }
    }
}