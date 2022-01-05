using System;
using JetBrains.Annotations;
using JetBrains.Application.Threading;
using JetBrains.ReSharper.Psi;
using JetBrains.ReSharper.Psi.CSharp;
using JetBrains.ReSharper.Psi.CSharp.Tree;
using JetBrains.ReSharper.Psi.Naming.Extentions;
using JetBrains.ReSharper.Psi.Naming.Impl;
using JetBrains.ReSharper.Psi.Naming.Settings;
using JetBrains.Util;

namespace JetBrains.ReSharper.Plugins.Unity.Utils
{
    internal static class NamingUtil
    {
        [NotNull]
        public static string GetUniqueName<T>([NotNull] T node, [CanBeNull] string baseName,
            NamedElementKinds elementKind, Action<INamesCollection> collectionModifier = null, Func<IDeclaredElement, bool> isConflictingElement = null) where T : ICSharpTreeNode
        {
            node.GetPsiServices().Locks.AssertMainThread();
            
            isConflictingElement = isConflictingElement ?? JetFunc<IDeclaredElement>.True;
            var namingManager = node.GetPsiServices().Naming;
            var policyProvider = namingManager.Policy.GetPolicyProvider(node.Language, node.GetSourceFile());
            var namingRule = policyProvider.GetPolicy(elementKind).NamingRule;
            var namesCollection = namingManager.Suggestion.CreateEmptyCollection(PluralityKinds.Unknown, CSharpLanguage.Instance, true, policyProvider);

            if (baseName != null)
            {
                var name = namingManager.Parsing.Parse(baseName, namingRule, policyProvider);
                var nameRoot = name.GetRootOrDefault(baseName);
                namesCollection.Add(nameRoot, new EntryOptions(PluralityKinds.Plural, SubrootPolicy.Decompose, emphasis: Emphasis.Good));
            }

            collectionModifier?.Invoke(namesCollection);
            var suggestionOptions = new SuggestionOptions
            {
                DefaultName = baseName,
                UniqueNameContext = node,
                IsConflictingElement = isConflictingElement
            };
            var namesSuggestion = namesCollection.Prepare(elementKind, ScopeKind.TypeAndNamespace, suggestionOptions);
            return namesSuggestion.FirstName();
        }
    }
}