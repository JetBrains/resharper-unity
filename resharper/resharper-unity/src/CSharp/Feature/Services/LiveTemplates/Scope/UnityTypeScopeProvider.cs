using System.Collections.Generic;
using JetBrains.Application;
using JetBrains.ProjectModel;
using JetBrains.ReSharper.Feature.Services.LiveTemplates.Context;
using JetBrains.ReSharper.Feature.Services.LiveTemplates.LiveTemplates;
using JetBrains.ReSharper.Feature.Services.LiveTemplates.Scope;
using JetBrains.ReSharper.Psi.CSharp.Tree;
using JetBrains.ReSharper.Psi.Files;
using JetBrains.ReSharper.Psi.Tree;

namespace JetBrains.ReSharper.Plugins.Unity.CSharp.Feature.Services.LiveTemplates.Scope
{
    [ShellComponent]
    public class UnityTypeScopeProvider : ScopeProvider
    {
        public UnityTypeScopeProvider()
        {
            Creators.Add(TryToCreate<MustBeInUnityType>);
            Creators.Add(TryToCreate<IsAvailableForClassAttribute>);
            Creators.Add(TryToCreate<IsAvailableForMethod>);
        }

        public override IEnumerable<ITemplateScopePoint> ProvideScopePoints(TemplateAcceptanceContext context)
        {
            if (!context.GetProject().IsUnityProject())
                yield break;

            var sourceFile = context.SourceFile;
            if (sourceFile == null)
                yield break;

            var caretOffset = context.CaretOffset;
            var prefix = LiveTemplatesManager.GetPrefix(caretOffset);
            var documentOffset = caretOffset - prefix.Length;
            if (!documentOffset.IsValid())
                yield break;

            var psiFile = sourceFile.GetPrimaryPsiFile();
            var element = psiFile?.FindTokenAt(caretOffset - prefix.Length);
            var typeDeclaration = element?.GetContainingNode<ITypeDeclaration>();
            if (typeDeclaration == null)
            {
                var siblingNode = element?.GetNextMeaningfulSibling();
                while (siblingNode is IAttributeList)
                {
                    siblingNode = element.GetNextMeaningfulSibling();
                }
                if (siblingNode is IClassDeclaration) 
                    yield return new IsAvailableForClassAttribute();
                yield break;
            }

            var unityApi = context.Solution.GetComponent<UnityApi>();
            if (unityApi.IsUnityType(typeDeclaration.DeclaredElement))
                yield return new MustBeInUnityType();

            if (element.GetContainingNode<IMethodDeclaration>() == null)
            {
                yield return new IsAvailableForMethod();
            }
        }
    }
}