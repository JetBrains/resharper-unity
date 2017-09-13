using System.Collections.Generic;
using System.Linq;
using JetBrains.ReSharper.Plugins.Unity.ShaderLab.Psi.Parsing;
using NUnit.Framework;

namespace JetBrains.ReSharper.Plugins.Unity.Tests.ShaderLab.Psi.Parsing
{
    [TestFixture]
    public class KeywordTokenTests
    {
        [Test]
        public void EnsureKeywordTokenSet()
        {
            var keywords = new JetHashSet<object>(typeof(ShaderLabTokenType).GetFields().Where(f => f.Name.EndsWith("_KEYWORD"))
                .Select(f => f.GetValue(null)));

            var missingKeywords = keywords.Except(ShaderLabTokenType.KEYWORDS);
            CollectionAssert.IsEmpty(missingKeywords);
        }
    }
}