#nullable enable

using JetBrains.ProjectModel;
using JetBrains.ReSharper.Daemon.CSharp.Stages;
using JetBrains.ReSharper.Plugins.Unity.UnityEditorIntegration.Api;
using JetBrains.ReSharper.Plugins.Unity.Utils;
using JetBrains.ReSharper.Psi;
using JetBrains.ReSharper.Psi.CSharp.Tree;
using JetBrains.ReSharper.Psi.Tree;

namespace JetBrains.ReSharper.Plugins.Unity.CSharp.Daemon.Stages.Dots
{
    [SolutionComponent]
    public class DotsElementsSuperTypeDeclaredInOtherPartSuppressor : ISuperTypeDeclaredInOtherPartSuppressor
        , IPartialTypeWithSinglePartSuppressor, ISequentialStructNoDefinedOrderingSuppressor
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
            return typeElement.DerivesFrom(KnownTypes.ComponentSystemBase)
                   || typeElement.DerivesFrom(KnownTypes.ISystem)
                   || typeElement.DerivesFrom(KnownTypes.IJobEntity)
                   || typeElement.DerivesFrom(KnownTypes.IAspect);
        }
    }
}