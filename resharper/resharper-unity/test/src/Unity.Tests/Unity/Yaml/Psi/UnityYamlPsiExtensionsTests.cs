using JetBrains.ReSharper.Plugins.Unity.Yaml.Psi;
using JetBrains.ReSharper.Plugins.Yaml.Psi.Parsing;
using JetBrains.ReSharper.Plugins.Yaml.Psi.Tree;
using JetBrains.ReSharper.Psi.Parsing;
using JetBrains.Text;
using NUnit.Framework;

namespace JetBrains.ReSharper.Plugins.Tests.Unity.Yaml.Psi
{
    public class UnityYamlPsiExtensionsTests
    {
        [TestCase("", null)]
        [TestCase("\"\"", "")]
        [TestCase("''", "")]
        [TestCase("'", null)]
        [TestCase("abc", "abc")]
        [TestCase("42", "42")]
        [TestCase(@"""\""""", "\"")]
        [TestCase(@"""\a""", "\\a")]
        [TestCase("\"\\\\\"", "\\")]
        [TestCase("'a''b''c'", "a'b'c")]
        [TestCase("\"W\\xFCrzburg\"", "Würzburg")]
        [TestCase("\"\\u27A2 W\\u00FCrzburg\"", "➢ Würzburg")]
        public void TestDecoding(string input, string? expected)
        {
            var lexer = new YamlLexer(new StringBuffer($"key: {input}"), false, false);
            var parser = new YamlParser(lexer.ToCachingLexer());
            var document = parser.ParseDocument();
            var value = ((IBlockMappingNode)document.Body.BlockNode).Entries[0].Content.Value;
            Assert.That(value.GetUnicodeText(), Is.EqualTo(expected));
        }
    }
}