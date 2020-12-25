using System.Collections.Generic;
using System.Linq;
using JetBrains.ProjectModel;
using JetBrains.ReSharper.Daemon.UsageChecking;
using JetBrains.ReSharper.Feature.Services.Daemon;
using JetBrains.ReSharper.Plugins.Unity.CSharp.Daemon.Stages.ContextSystem;
using JetBrains.ReSharper.Psi;
using JetBrains.ReSharper.Psi.CSharp.Tree;
using JetBrains.ReSharper.Psi.Search;
using JetBrains.ReSharper.Psi.Tree;
using JetBrains.ReSharper.Psi.Util;

namespace JetBrains.ReSharper.Plugins.Unity.CSharp.Daemon.Stages.PerformanceCriticalCodeAnalysis.Analyzers
{
    [SolutionComponent]
    public class MultidimensionalArraysAccessAnalyzer : PerformanceProblemAnalyzerBase<IElementAccessExpression>
    {
        protected override void Analyze(IElementAccessExpression element, IHighlightingConsumer consumer, IReadOnlyCallGraphContext context)
        {
            var sourceFile = element.GetSourceFile();
            if (sourceFile == null)
                return;
            
            if (!(element.ConditionalQualifier is IReferenceExpression referenceExpression))
                return;

            var declaredElement = referenceExpression.Reference.Resolve().DeclaredElement;
            
            // Before taking declaration, ensure that it is multidim type and declaration is contained in same source file (avoid parsing another psi file)
            var type = declaredElement?.Type() as IArrayType;
            if (type == null)
                return;
            
            if (type.Rank == 1)
                return;

            if (declaredElement is ITypeMember typeMember && typeMember.GetAccessRights() != AccessRights.PRIVATE)
                return;
            
            // Safe to take declaration
            var declaration = declaredElement.GetDeclarationsIn(sourceFile).FirstOrDefault() as IVariableDeclaration;
            if (declaration == null)
                return;
            
            MultidimensionalArraysDeclarationProblemAnalyzer.CreateInefficientMultidimensionalArrayAccessWarning(declaredElement, declaration, context.DaemonProcess, context.Kind, consumer, element.GetDocumentRange());
        }


        private List<ITreeNode> FindArrayUsages(IDeclaredElement declaredElement, IClassLikeDeclaration container, IDaemonProcess daemonProcess, DaemonProcessKind processKind)
        {
            var result = new List<ITreeNode>();
            if (processKind != DaemonProcessKind.GLOBAL_WARNINGS)
            {
                var collectUsagesStageProcess = daemonProcess?.GetStageProcess<CollectUsagesStageProcess>();
                if (collectUsagesStageProcess == null)
                    return result;

                foreach (var usage in collectUsagesStageProcess.GetDeclaredElementUsages(declaredElement)
                    .Where(container.Contains))
                {
                    result.Add(usage);
                }

                return result;
            }

            var finder = declaredElement.GetPsiServices().Finder;
            foreach (var reference in finder.FindAllReferences(declaredElement))
            {
                result.Add(reference.GetTreeNode());
            }

            return result;
        }
        
    }
}