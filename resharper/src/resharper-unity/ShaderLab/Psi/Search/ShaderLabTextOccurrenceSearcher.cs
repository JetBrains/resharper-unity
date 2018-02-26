using System;
using System.Collections.Generic;
using JetBrains.ReSharper.Psi;
using JetBrains.ReSharper.Psi.ExtensionsAPI;
using JetBrains.ReSharper.Psi.ExtensionsAPI.Finder;
using JetBrains.ReSharper.Psi.Tree;

namespace JetBrains.ReSharper.Plugins.Unity.ShaderLab.Psi.Search
{
    // TODO: I don't know when this gets used
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
            get { return node => true; }
        }
    }
}