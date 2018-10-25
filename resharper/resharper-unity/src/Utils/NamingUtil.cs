using System;
using JetBrains.Annotations;
using JetBrains.ReSharper.Psi;
using JetBrains.ReSharper.Psi.CSharp;
using JetBrains.ReSharper.Psi.CSharp.Tree;
using JetBrains.ReSharper.Psi.Naming.Extentions;
using JetBrains.ReSharper.Psi.Naming.Impl;
using JetBrains.ReSharper.Psi.Naming.Settings;
using JetBrains.Util;

namespace JetBrains.ReSharper.Plugins.Unity.Utils
{
    public static class NamingUtil
    {
        [NotNull]
        public static string GetUniqueName<T>([NotNull] T node, [NotNull] string baseName,
            NamedElementKinds elementKind, Func<IDeclaredElement, bool> isConflictingElement = null) where T : ICSharpTreeNode
        {
            isConflictingElement = isConflictingElement ?? JetFunc<IDeclaredElement>.True;
            var namingManager = node.GetPsiServices().Naming;
            var policyProvider = namingManager.Policy.GetPolicyProvider(node.Language, node.GetSourceFile());
            var namingRule = policyProvider.GetPolicy(elementKind).NamingRule;
            var name = namingManager.Parsing.Parse(baseName, namingRule, policyProvider);
            var nameRoot = name.GetRootOrDefault(baseName);
            var namesCollection = namingManager.Suggestion.CreateEmptyCollection(PluralityKinds.Unknown, CSharpLanguage.Instance, true, policyProvider);
            namesCollection.Add(nameRoot, new EntryOptions(PluralityKinds.Unknown, SubrootPolicy.Decompose, emphasis: Emphasis.Good));
            var suggestionOptions = new SuggestionOptions
            {
                DefaultName = baseName,
                UniqueNameContext = node,
                IsConflictingElement = isConflictingElement
            };
            var namesSuggestion = namesCollection.Prepare(elementKind, ScopeKind.Common, suggestionOptions);
            return namesSuggestion.FirstName();
        }
    }
}