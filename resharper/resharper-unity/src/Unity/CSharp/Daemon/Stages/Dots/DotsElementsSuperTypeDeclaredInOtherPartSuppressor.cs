#nullable enable

using JetBrains.ProjectModel;
using JetBrains.ReSharper.Daemon.CSharp.Stages;
using JetBrains.ReSharper.Plugins.Unity.UnityEditorIntegration.Api;
using JetBrains.ReSharper.Psi;
using JetBrains.ReSharper.Psi.CSharp.Tree;
using JetBrains.ReSharper.Psi.Tree;

namespace JetBrains.ReSharper.Plugins.Unity.CSharp.Daemon.Stages.Dots
{
    [SolutionComponent]
    public class DotsElementsSuperTypeDeclaredInOtherPartSuppressor : ISuperTypeDeclaredInOtherPartSuppressor
        , IPartialTypeWithSinglePartSuppressor
    {
        public virtual bool SuppressInspections(IDeclaredType superType, IClassLikeDeclaration declaration,
            ITypeDeclaration otherPartDeclaration)
        {
            return IsPossibleDotsPartialClasses(declaration.DeclaredElement) &&
                   IsPossibleDotsPartialClasses(superType.GetTypeElement()) &&
                   otherPartDeclaration.GetSourceFile().IsSourceGeneratedFile();
        }

        public virtual bool SuppressInspections(IClassLikeDeclaration classLikeDeclaration)
        {
            return IsPossibleDotsPartialClasses(classLikeDeclaration.DeclaredElement);
        }

        private static bool IsPossibleDotsPartialClasses(ITypeElement? typeElement)
        {
            return UnityApi.IsDerivesFromSystemBase(typeElement)
                   || UnityApi.IsDerivesFromISystem(typeElement)
                   || UnityApi.IsDerivesFromIJobEntity(typeElement)
                   || UnityApi.IsDerivesFromIAspect(typeElement);
        }
    }
}