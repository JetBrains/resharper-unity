using System.Collections.Generic;
using System.Linq;
using JetBrains.Application.Settings;
using JetBrains.Lifetimes;
using JetBrains.ReSharper.Plugins.Unity.Core.Application.Settings;
using JetBrains.ReSharper.Plugins.Unity.UnityEditorIntegration.Api;
using JetBrains.ReSharper.Psi;
using JetBrains.ReSharper.Psi.CSharp;
using JetBrains.ReSharper.Psi.CSharp.Tree;
using JetBrains.ReSharper.Psi.ExtensionsAPI;
using JetBrains.ReSharper.Psi.Search;

namespace JetBrains.ReSharper.Plugins.Unity.CSharp.Psi.Search
{
    [PsiSharedComponent]
    public class UnityDotsSearchFactory : DomainSpecificSearcherFactoryBase
    {
        private readonly IContextBoundSettingsStore mySettingsStore;

        public UnityDotsSearchFactory(Lifetime lifetime, ISettingsStore settingsStore)
        {
            mySettingsStore = settingsStore.BindToContextLive(lifetime, ContextRange.ApplicationWide);
        }
        public override bool IsCompatibleWithLanguage(PsiLanguageType languageType)
        {
            return languageType.Is<CSharpLanguage>();
        }

        public override ICollection<FindResult> TransformNavigationTargets(ICollection<FindResult> targets)
        {
            var hideGeneratedCode = mySettingsStore.GetValue((UnitySettings s) => s.HideGeneratedCodeFromNavigation);

            if (!hideGeneratedCode)
                return null;

            foreach (var result in targets)
            {
                if (IsDotsRelatedCodeGeneratedDeclaration(result))
                    return targets.Where(t => !IsDotsRelatedCodeGeneratedDeclaration(t)).ToList();
            }

            return null;
        }

        private static bool IsDotsRelatedCodeGeneratedDeclaration(FindResult result)
        {
            if (result is not FindResultDeclaration { Declaration: IClassLikeDeclaration {IsPartial: true } classLikeDeclaration } resultDeclaration)
                return false;
            
            if (!classLikeDeclaration.DeclaredElement.IsDotsImplicitlyUsedType())
                return false;

            return resultDeclaration.SourceFile.IsSourceGeneratedFile();
        }
    }
}