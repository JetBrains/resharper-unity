#nullable enable

using JetBrains.ProjectModel;
using JetBrains.ReSharper.Daemon.CSharp.Stages;
using JetBrains.ReSharper.Plugins.Unity.UnityEditorIntegration.Api;
using JetBrains.ReSharper.Psi;
using JetBrains.ReSharper.Psi.CSharp.Tree;

namespace JetBrains.ReSharper.Plugins.Unity.CSharp.Daemon.Stages.BurstCodeAnalysis.Analyzers
{
    [SolutionComponent]
    public class SuperTypeDeclaredInOtherPartSuppressor : ISuperTypeDeclaredInOtherPartSuppressor
        , IPartialTypeWithSinglePartSuppressor
    {
        bool ISuperTypeDeclaredInOtherPartSuppressor.SuppressInspections(IClassLikeDeclaration classLikeDeclaration,
            IDeclaredType currentSuperInterfaceType,
            IDeclaredType otherSuperInterfaceType,
            IPsiSourceFile otherSuperTypeSourceFile)
        {
            return otherSuperTypeSourceFile.IsSourceGeneratedFile();
        }

        bool IPartialTypeWithSinglePartSuppressor.SuppressInspections(IClassLikeDeclaration classLikeDeclaration)
        {
            return UnityApi.IsDotsImplicitlyUsedType(classLikeDeclaration.DeclaredElement);
        }
    }
}