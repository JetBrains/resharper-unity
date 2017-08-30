using System.Linq;
using JetBrains.ReSharper.Plugins.Unity.Psi.ShaderLab.Parsing;
using NUnit.Framework;

namespace JetBrains.ReSharper.Plugins.Unity.Tests.Psi.ShaderLab.Parsing
{
    [TestFixture]
    public class KeywordTokenTests
    {
        [Test]
        public void EnsureKeywordTokenSet()
        {
            var keywords = typeof(ShaderLabTokenType).GetFields().Where(f => f.Name.EndsWith("_KEYWORD"))
                .Select(f => f.GetValue(null)).ToHashSet();

            var missingKeywords = keywords.Except(ShaderLabTokenType.KEYWORDS);
            CollectionAssert.IsEmpty(missingKeywords);
        }
    }
}