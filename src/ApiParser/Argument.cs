using System.Linq;
using JetBrains.Annotations;

namespace ApiParser
{
    public class Argument
    {
        public Argument([NotNull] string type, int index, int total)
        {
            Name = total > 1 ? $"arg{index + 1}" : @"arg";

            if (type.Contains(' '))
            {
                var parts = type.Split(' ');
                Type = new ApiType(parts[0]);
                Name = parts[1];
            }
            else
            {
                Type = new ApiType(type);
            }
        }

        [CanBeNull]
        public string Description { get; set; }

        [NotNull]
        public string Name { get; set; }

        [NotNull]
        public ApiType Type { get; }
    }
}