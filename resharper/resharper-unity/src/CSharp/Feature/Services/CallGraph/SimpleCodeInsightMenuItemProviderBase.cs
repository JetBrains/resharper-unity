using System.Collections.Generic;
using JetBrains.Annotations;
using JetBrains.Application.Threading;
using JetBrains.Application.UI.Controls.BulbMenu.Items;
using JetBrains.ProjectModel;
using JetBrains.ReSharper.Plugins.Unity.CSharp.Daemon.Stages.CallGraphStage;
using JetBrains.ReSharper.Plugins.Unity.CSharp.Daemon.Stages.ContextSystem;
using JetBrains.ReSharper.Psi.CSharp.Tree;
using JetBrains.TextControl;
using JetBrains.Util;

namespace JetBrains.ReSharper.Plugins.Unity.CSharp.Feature.Services.CallGraph
{
    public abstract class SimpleCodeInsightMenuItemProviderBase : ICallGraphCodeInsightMenuItemProvider
    {
        protected readonly ISolution Solution;

        protected SimpleCodeInsightMenuItemProviderBase(ISolution solution)
        {
            Solution = solution;
        }

        public IEnumerable<BulbMenuItem> GetMenuItems(IMethodDeclaration methodDeclaration, ITextControl textControl, IReadOnlyCallGraphContext context)
        {
            methodDeclaration.GetPsiServices().Locks.AssertReadAccessAllowed();
            
            if (!CheckCallGraph(methodDeclaration, context))
                return EmptyList<BulbMenuItem>.Enumerable;
            
            var result = GetActions(methodDeclaration, textControl);
            
            return result;
        }

        protected abstract bool CheckCallGraph([NotNull] IMethodDeclaration methodDeclaration, [NotNull] IReadOnlyCallGraphContext context);
        
        protected abstract IEnumerable<BulbMenuItem> GetActions([NotNull] IMethodDeclaration methodDeclaration, [NotNull] ITextControl textControl);
    }
}