using JetBrains.Annotations;
using JetBrains.ReSharper.Feature.Services.Bulbs;
using JetBrains.ReSharper.Plugins.Unity.CSharp.Daemon.Stages.ContextSystem;
using JetBrains.ReSharper.Psi.CSharp.Tree;

namespace JetBrains.ReSharper.Plugins.Unity.CSharp.Feature.Services.CallGraph
{
    public interface ILensBulbActionProvider
    {
        bool IsApplicable(CallGraphContext context);

        [NotNull]
        IBulbAction GetAction([NotNull] IMethodDeclaration containingMethod);
    }
}