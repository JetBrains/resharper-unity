using System;
using System.Collections.Generic;
using JetBrains.ReSharper.Plugins.Unity.UnityEditorIntegration.Api;
using JetBrains.ReSharper.Psi;
using JetBrains.ReSharper.Psi.CSharp;
using JetBrains.ReSharper.Psi.Naming.Impl;
using JetBrains.ReSharper.Psi.Naming.Interfaces;
using JetBrains.ReSharper.Psi.Util;

namespace JetBrains.ReSharper.Plugins.Unity.CSharp.Psi.Naming.Dots
{
    [Language(typeof(CSharpLanguage))]
    public class UnityDotsNamingSuggestionAdviser : IClrNamingSuggestionAdviser
    {
        public bool SuggestRoots(IType type, INamingPolicyProvider namingPolicyProvider,
            Func<IType, IEnumerable<NameRoot>> nameProvider, IList<NameRoot> outputNameRoots)
        {
            var typeElement = type.GetTypeElement();
            if (typeElement == null)
                return false;

            if (UnityApi.IsRefRW(typeElement) || UnityApi.IsRefRO(typeElement))
            {
                if (type is IDeclaredType declaredType)
                {
                    var typeParameters = typeElement.TypeParameters;
                    var internalType = declaredType.GetSubstitution()[typeParameters[0]];

                    var namingManager = declaredType.GetPsiServices().Naming;
                    var name = namingManager.Parsing.GetName(typeElement, "unknown", namingPolicyProvider);
                    var originalTypeRoot = name.GetRoot();

                    foreach (var nameRoot in nameProvider.Invoke(internalType))
                    {
                        outputNameRoots.Add(nameRoot);
                        if (originalTypeRoot != null)
                            outputNameRoots.Add(nameRoot.AppendRoot(originalTypeRoot));
                    }

                    return true;
                }
            }

            return false;
        }
    }
}