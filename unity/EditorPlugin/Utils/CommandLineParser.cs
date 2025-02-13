using System.Collections.Generic;

namespace JetBrains.Rider.Unity.Editor.Utils
{
    internal class CommandLineParser
    {
        public Dictionary<string, string> Options = new();

        public CommandLineParser(string[] args)
        {
            var i = 0;
            while (i < args.Length)
            {
                var arg = args[i];
                if (!arg.StartsWith("-"))
                {
                    i++;
                    continue;
                }

                string value = null;
                if (i + 1 < args.Length && !args[i + 1].StartsWith("-"))
                {
                    value = args[i + 1];
                    i++;
                }

                if (!(Options.ContainsKey(arg)))
                {
                    Options.Add(arg, value);
                }
                i++;
            }
        }
    }
}