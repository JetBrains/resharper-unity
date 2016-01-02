using System.Diagnostics;
using System.Linq;
using JetBrains.Annotations;

namespace ApiParser
{
    public class Argument
    {
        private string _description;
        private string _name;
        private bool _knownDescription;

        public Argument([NotNull] string type, int index, int total)
        {
            _description = total > 1 ? $"Unknown argument #{index + 1}" : "Unknown argument";
            _name = total > 1 ? $"arg{index + 1}" : @"arg";

            if (type.Contains(' '))
            {
                string[] parts = type.Split(' ');
                Type = new ApiType(parts[0]);
                _name = parts[1];
                _description = $"'{Name}' argument";
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
            get { return _description; }
            set
            {
                _description = value;
                _knownDescription = true;
            }
        }

        [NotNull]
        public string Name
        {
            [DebuggerStepThrough]
            get { return _name; }
            set
            {
                _name = value;
                if (!_knownDescription) _description = $"'{value}' argument";
            }
        }

        [NotNull]
        public ApiType Type { get; }
    }
}