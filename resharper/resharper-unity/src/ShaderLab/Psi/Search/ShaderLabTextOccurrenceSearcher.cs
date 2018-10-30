using System;
using System.Collections.Generic;
using JetBrains.ReSharper.Psi;
using JetBrains.ReSharper.Psi.ExtensionsAPI;
using JetBrains.ReSharper.Psi.ExtensionsAPI.Finder;
using JetBrains.ReSharper.Psi.Tree;

namespace JetBrains.ReSharper.Plugins.Unity.ShaderLab.Psi.Search
{
    // Finds the nodes that can contain text. Used in a rename operation when "search in text and comments" is enabled
    public class ShaderLabTextOccurrenceSearcher : TextOccurrenceSearcherBase<ShaderLabLanguage>, IDomainSpecificSearcher
    {
        public ShaderLabTextOccurrenceSearcher(IEnumerable<IDeclaredElement> elements)
            : base(elements)
        {
        }

        public ShaderLabTextOccurrenceSearcher(string subject)
            : base(subject)
        {
        }

        protected override Predicate<ITreeNode> Predicate
        {
            get
            {
                // Allow finding text in comments and string literals. The only string literals in a ShaderLab file are
                // Shader/fallback names, display name for properties, tag name/values. We might want to revisit this
                // later, when some of those names have references on them, but this is good enough for now.
                return node => node is ICommentNode || node.GetTokenType()?.IsStringLiteral == true;
            }
        }
    }
}