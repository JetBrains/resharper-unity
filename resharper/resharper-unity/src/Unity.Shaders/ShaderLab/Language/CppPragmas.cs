#nullable enable
using System.Collections.Generic;
using JetBrains.ReSharper.Psi.Cpp.Language;

namespace JetBrains.ReSharper.Plugins.Unity.Shaders.ShaderLab.Language
{
    public class CppPragmas : Dictionary<string, PragmaCommand>
    {
        protected CppPragmas(IEnumerable<PragmaCommand> pragmaCommands)
        {
            foreach (var command in pragmaCommands)
                Add(command.Name, command);
        }
    }
}