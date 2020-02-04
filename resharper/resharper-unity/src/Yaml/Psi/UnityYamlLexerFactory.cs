using JetBrains.ReSharper.Plugins.Unity.Yaml.Psi.Parsing;
using JetBrains.ReSharper.Plugins.Yaml.Psi;
using JetBrains.ReSharper.Psi.Parsing;
using JetBrains.Text;

namespace JetBrains.ReSharper.Plugins.Unity.Yaml.Psi
{
    public class UnityYamlLexerFactory : YamlLexerFactory
    {
        protected override ILexer GetDefaultLexer(IBuffer buffer) => new UnityYamlLexer(buffer);
    }
}