using System.Diagnostics;
using System.Linq;
using JetBrains.Annotations;

namespace ApiParser
{
    public class Argument
    {
        private string description;
        private string name;
        private bool knownDescription;

        public Argument([NotNull] string type, int index, int total)
        {
            description = total > 1 ? $"Unknown argument #{index + 1}" : "Unknown argument";
            name = total > 1 ? $"arg{index + 1}" : @"arg";

            if (type.Contains(' '))
            {
                var parts = type.Split(' ');
                Type = new ApiType(parts[0]);
                name = parts[1];
                description = $"'{Name}' argument";
            }
            else
            {
                Type = new ApiType(type);
            }
        }

        [NotNull]
        public string Description
        {
            [DebuggerStepThrough]
            get { return description; }
            set
            {
                description = value;
                knownDescription = true;
            }
        }

        [NotNull]
        public string Name
        {
            [DebuggerStepThrough]
            get { return name; }
            set
            {
                name = value;
                if (!knownDescription) description = $"'{value}' argument";
            }
        }

        [NotNull]
        public ApiType Type { get; }
    }
}