using JetBrains.ReSharper.Feature.Services.QuickFixes;
using JetBrains.ReSharper.Plugins.Unity.CSharp.Daemon.Stages.BurstCodeAnalysis.Highlightings;
using JetBrains.ReSharper.Psi.CSharp.Tree;
using JetBrains.Util;

namespace JetBrains.ReSharper.Plugins.Unity.CSharp.Feature.Services.CallGraph.BurstCodeAnalysis.AddDiscardAttribute
{
    public struct AddDiscardAttributeBulbActionProvider : IBulbActionProvider<AddDiscardAttributeBulbAction>
    {
        public AddDiscardAttributeBulbAction GetBulbAction(IMethodDeclaration methodDeclaration)
        {
            return new AddDiscardAttributeBulbAction(methodDeclaration);
        }
    }

    [QuickFix]
    public sealed class AddDiscardAttributeQuickFix : ContainingMethodQuickFixBase<AddDiscardAttributeBulbActionProvider, AddDiscardAttributeBulbAction>, IQuickFix
    {
        public AddDiscardAttributeQuickFix(IBurstHighlighting burstHighlighting) : base(burstHighlighting?.Node)
        {
        }
        
        public bool IsAvailable(IUserDataHolder cache)
        {
            var methodDeclaration = MethodDeclaration;
            
            return BurstActionsUtil.IsAvailable(methodDeclaration);
        }
    }
}