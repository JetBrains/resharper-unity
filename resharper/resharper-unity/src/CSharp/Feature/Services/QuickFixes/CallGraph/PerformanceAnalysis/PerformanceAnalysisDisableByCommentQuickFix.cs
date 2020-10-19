using System.Collections.Generic;
using JetBrains.Annotations;
using JetBrains.DocumentManagers;
using JetBrains.ReSharper.Feature.Services.Bulbs;
using JetBrains.ReSharper.Feature.Services.Intentions;
using JetBrains.ReSharper.Feature.Services.QuickFixes;
using JetBrains.ReSharper.Plugins.Unity.CSharp.Daemon.Errors;
using JetBrains.ReSharper.Plugins.Unity.CSharp.Daemon.Stages.PerformanceCriticalCodeAnalysis;
using JetBrains.ReSharper.Psi;
using JetBrains.ReSharper.Psi.CSharp;
using JetBrains.ReSharper.Psi.CSharp.Tree;
using JetBrains.ReSharper.Psi.Tree;
using JetBrains.Util;

namespace JetBrains.ReSharper.Plugins.Unity.CSharp.Feature.Services.QuickFixes.CallGraph.PerformanceAnalysis
{
    [QuickFix]
    public class PerformanceAnalysisDisableByCommentQuickFix : IQuickFix
    {
        [CanBeNull] private readonly IMethodDeclaration myMethodDeclaration;
        [CanBeNull] private readonly PerformanceAnalysisDisableByCommentBulbAction myBulbAction;

        public PerformanceAnalysisDisableByCommentQuickFix(UnityPerformanceInvocationWarning performanceHighlighting)
        {
            var range = performanceHighlighting.CalculateRange();
            var document = range.Document;
            var solution = document.TryGetSolution();

            if (solution == null)
                return;
            
            var psiSourceFile = document.GetPsiSourceFile(solution);
            var file = psiSourceFile?.GetTheOnlyPsiFile(CSharpLanguage.Instance);
            
            if (file == null)
                return;
            
            var node = file.FindNodeAt(range);
            myMethodDeclaration = node?.GetContainingNode<IMethodDeclaration>();
            
            if (myMethodDeclaration != null)
                myBulbAction = new PerformanceAnalysisDisableByCommentBulbAction(myMethodDeclaration);
        }

        public IEnumerable<IntentionAction> CreateBulbItems()
        {
            if (myMethodDeclaration == null || myBulbAction == null)
                return EmptyList<IntentionAction>.Instance;

            return myBulbAction.ToQuickFixIntentions();
        }

        public bool IsAvailable(IUserDataHolder cache)
        {
            var methodDeclaration = myMethodDeclaration;
            
            // CGTD overlook. performance and validity
            if (methodDeclaration == null)
                return false;

            var declaredElement = methodDeclaration.DeclaredElement;

            return declaredElement != null && methodDeclaration.IsValid() &&
                   !PerformanceCriticalCodeStageUtil.IsPerformanceCriticalRootMethod(methodDeclaration);
        }
    }
}