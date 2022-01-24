using System;
using JetBrains.Application.Threading;
using JetBrains.Diagnostics;
using JetBrains.ReSharper.Psi;
using JetBrains.ReSharper.Psi.CSharp;
using JetBrains.ReSharper.Psi.CSharp.Tree;
using JetBrains.ReSharper.Psi.Naming.Extentions;
using JetBrains.ReSharper.Psi.Naming.Impl;
using JetBrains.ReSharper.Psi.Naming.Settings;
using JetBrains.Util;

#nullable enable

namespace JetBrains.ReSharper.Plugins.Unity.Utils
{
    internal static class NamingUtil
    {
        public static string GetUniqueName<T>(T node,
                                              string? baseName,
                                              NamedElementKinds elementKind,
                                              Action<INamesCollection>? collectionModifier = null,
                                              Func<IDeclaredElement, bool>? isConflictingElement = null)
            where T : ICSharpTreeNode
        {
            node.GetPsiServices().Locks.AssertMainThread();

            var namingManager = node.GetPsiServices().Naming;
            var policyProvider = namingManager.Policy.GetPolicyProvider(node.Language, node.GetSourceFile());
            var namingRule = policyProvider.GetPolicy(elementKind).NamingRule;
            var namesCollection = namingManager.Suggestion.CreateEmptyCollection(PluralityKinds.Unknown,
                CSharpLanguage.Instance.NotNull("CSharpLanguage.Instance != null"), true, policyProvider);

            if (baseName != null)
            {
                var name = namingManager.Parsing.Parse(baseName, namingRule, policyProvider);
                var nameRoot = name.GetRootOrDefault(baseName);
                namesCollection.Add(nameRoot,
                    new EntryOptions(PluralityKinds.Plural, SubrootPolicy.Decompose, emphasis: Emphasis.Good));
            }

            collectionModifier?.Invoke(namesCollection);
            var suggestionOptions = new SuggestionOptions
            {
                DefaultName = baseName,
                UniqueNameContext = node,
                IsConflictingElement = isConflictingElement ?? JetFunc<IDeclaredElement>.True
            };
            var namesSuggestion = namesCollection.Prepare(elementKind, ScopeKind.TypeAndNamespace, suggestionOptions);
            return namesSuggestion.FirstName();
        }

        public static bool IsIdentifier(string name)
        {
            if (name.Length == 0)
                return false;
            char[] charArray = name.ToCharArray();
            if (!char.IsLetter(charArray[0]) && charArray[0] != '_')
                return false;
            for (int index = 1; index < charArray.Length; ++index)
            {
                char c = charArray[index];
                if (!char.IsLetterOrDigit(c) && c != '_')
                    return false;
            }

            return true;
        }
    }
}
