using System.Collections.Generic;
using JetBrains.Annotations;
using JetBrains.Application.Threading;
using JetBrains.ProjectModel;
using JetBrains.ReSharper.Daemon;
using JetBrains.ReSharper.Feature.Services.CSharp.ContextActions;
using JetBrains.ReSharper.Feature.Services.Daemon;
using JetBrains.ReSharper.Feature.Services.Intentions;
using JetBrains.ReSharper.Plugins.Unity.CSharp.Daemon.CallGraph;
using JetBrains.ReSharper.Psi.CSharp.Tree;
using JetBrains.Util;

namespace JetBrains.ReSharper.Plugins.Unity.CSharp.Feature.Services.CallGraph
{
    public abstract class SimpleMethodContextActionBase
    {
        protected SimpleMethodContextActionBase(ICSharpContextActionDataProvider dataProvider)
        {
            myDataProvider = dataProvider;
            mySwa = dataProvider.Solution.GetComponent<SolutionAnalysisService>();
        }

        private readonly ICSharpContextActionDataProvider myDataProvider;
        private readonly SolutionAnalysisService mySwa;
        
        /// <summary>
        /// Current method declaration under caret.
        /// </summary>
        [CanBeNull]
        protected IMethodDeclaration CurrentMethodDeclaration
        {
            get
            {
                var methodDeclaration = UnityCallGraphUtil.GetMethodDeclarationByCaret(myDataProvider);

                return methodDeclaration;
            }
        }
        
        /// <summary>
        /// Backend check. Long activities. IsAvailable is front-end check, should be fast
        /// </summary>
        /// <returns></returns>
        public IEnumerable<IntentionAction> CreateBulbItems()
        {
            var methodDeclaration = CurrentMethodDeclaration;
            var processKind = UnityCallGraphUtil.GetProcessKindForGraph(mySwa);
            
            methodDeclaration?.GetPsiServices().Locks.AssertReadAccessAllowed();
            
            if (methodDeclaration == null || !ShouldCreate(methodDeclaration, processKind))
                return EmptyList<IntentionAction>.Instance;

            return GetActions(methodDeclaration);
        }

        /// <summary>
        /// Checks if actions should be created
        /// </summary>
        /// <param name="methodDeclaration"></param>
        /// <param name="processKind"></param>
        /// <returns></returns>
        protected abstract bool ShouldCreate([NotNull] IMethodDeclaration methodDeclaration, DaemonProcessKind processKind);
        
        /// <summary>
        /// Creates actions only
        /// </summary>
        /// <param name="methodDeclaration"></param>
        /// <returns></returns>
        protected abstract IEnumerable<IntentionAction> GetActions([NotNull] IMethodDeclaration methodDeclaration);
    }
}