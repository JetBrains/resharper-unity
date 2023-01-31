using System;
using System.Collections.Generic;
using JetBrains.ReSharper.Plugins.Unity.UnityEditorIntegration.Api;
using JetBrains.ReSharper.Psi;
using JetBrains.ReSharper.Psi.CSharp;
using JetBrains.ReSharper.Psi.Naming.Impl;
using JetBrains.ReSharper.Psi.Util;
using JetBrains.Util;

namespace JetBrains.ReSharper.Plugins.Unity.CSharp.Psi.Naming.Dots
{
    [Language(typeof(CSharpLanguage))]
    public class UnityDotsNamingSuggestionAdviser : IClrNamingSuggestionAdviser
    {
        public IReadOnlyList<NameRoot> SuggestRoots(IType type, Func<IType, IEnumerable<NameRoot>> nameProvider)
        {
            var typeElement = type.GetTypeElement();
            if (typeElement == null)
                return EmptyList<NameRoot>.Instance;

            var isRefRw = UnityApi.IsRefRW(typeElement);
            var isRefRo = UnityApi.IsRefRO(typeElement);
            
            if (isRefRw || isRefRo)
            {
                if (type is IDeclaredType declaredType)
                {
                    var typeParameters = typeElement.TypeParameters;
                    var internalType = declaredType.GetSubstitution()[typeParameters[0]];

                    var outputNameRoots = new List<NameRoot>();
                    foreach (var nameRoot in nameProvider.Invoke(internalType))
                        outputNameRoots.Add(nameRoot);

                    return outputNameRoots;
                }
            }

            return EmptyList<NameRoot>.Instance;
        }
    }
}