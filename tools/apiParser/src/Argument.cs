using JetBrains.Annotations;

namespace ApiParser
{
    public class Argument
    {
        public Argument(ApiType type, string name)
        {
            Name = name;
            Type = type;
            Descriptions = new UnityApiDescriptions();
        }

        public UnityApiDescriptions Descriptions;

        [NotNull]
        public string Name { get; set; }

        [NotNull]
        public ApiType Type { get; }
    }
}