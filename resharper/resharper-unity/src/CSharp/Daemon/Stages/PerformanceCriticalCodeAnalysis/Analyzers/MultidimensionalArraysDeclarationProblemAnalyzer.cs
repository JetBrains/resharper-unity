using System.Collections.Generic;
using System.Linq;
using JetBrains.Annotations;
using JetBrains.DocumentModel;
using JetBrains.ProjectModel;
using JetBrains.ReSharper.Daemon.UsageChecking;
using JetBrains.ReSharper.Feature.Services.Daemon;
using JetBrains.ReSharper.Plugins.Unity.CSharp.Daemon.Errors;
using JetBrains.ReSharper.Plugins.Unity.CSharp.Daemon.Stages.ContextSystem;
using JetBrains.ReSharper.Psi;
using JetBrains.ReSharper.Psi.CSharp.Tree;
using JetBrains.ReSharper.Psi.Pointers;
using JetBrains.ReSharper.Psi.Search;
using JetBrains.ReSharper.Psi.Tree;

namespace JetBrains.ReSharper.Plugins.Unity.CSharp.Daemon.Stages.PerformanceCriticalCodeAnalysis.Analyzers
{
    [SolutionComponent]
    public class MultidimensionalArraysDeclarationProblemAnalyzer : PerformanceProblemAnalyzerBase<IVariableDeclaration>
    {
        protected override void Analyze(IVariableDeclaration element,
            IHighlightingConsumer consumer, IReadOnlyContext context)
        {
            CreateInefficientMultidimensionalArrayAccessWarning(element.DeclaredElement, element, context.DaemonProcess, context.Kind, consumer, element.NameIdentifier.GetDocumentRange());
        }

        public static void CreateInefficientMultidimensionalArrayAccessWarning([CanBeNull] IDeclaredElement declaredElement,
            [NotNull] IVariableDeclaration variableDeclaration,[NotNull]  IDaemonProcess daemonProcess,[NotNull]  DaemonProcessKind kind,
            [NotNull] IHighlightingConsumer consumer, [NotNull] DocumentRange highlightingRange)
        {
            if (declaredElement == null)
                return;
            
            var type = variableDeclaration.Type as IArrayType;
            if (type == null)
                return;
            
            if (type.Rank == 1)
                return;
            
            IArrayCreationExpression arrayCreationExpression = null;
            IMultipleDeclaration multipleDeclaration = null;

            switch (variableDeclaration)
            {
                case IFieldDeclaration fieldDeclaration:
                    // perfomance optimization
                    if (fieldDeclaration.GetAccessRights() != AccessRights.PRIVATE)
                        return;

                    arrayCreationExpression = (fieldDeclaration.Initial as IExpressionInitializer)?.Value as IArrayCreationExpression;
                    multipleDeclaration = MultipleFieldDeclarationNavigator.GetByDeclarator(fieldDeclaration);
                    
                    break;
                case ILocalVariableDeclaration localVariableDeclaration:
                    var initial = localVariableDeclaration.Initial;
                    arrayCreationExpression = (initial as IExpressionInitializer)?.Value as IArrayCreationExpression;
                    
                    if (initial != null && arrayCreationExpression == null)
                        return;
                    
                    multipleDeclaration =
                        MultipleLocalVariableDeclarationNavigator.GetByDeclarator(localVariableDeclaration);
                    break;
                default:
                    return;
            }
            
            var classDeclaration = variableDeclaration.GetContainingNode<IClassLikeDeclaration>();
            if (classDeclaration == null)
                return;
            
            var usages = FindArrayUsages(declaredElement, classDeclaration, daemonProcess, kind);
            
            foreach (var usage in usages)
            {
                if (ElementAccessExpressionNavigator.GetByOperand(usage as ICSharpExpression) == null) 
                    return;
            }
            
            if (arrayCreationExpression.ContainsErrorElement())
                return;
            
            consumer.AddHighlighting(new InefficientMultidimensionalArrayUsageWarning(usages.Select(t => t.CreateTreeElementPointer()).ToList(),
                arrayCreationExpression, variableDeclaration, multipleDeclaration, highlightingRange));

        }
        

        private static List<ITreeNode> FindArrayUsages(IDeclaredElement declaredElement, IClassLikeDeclaration container, IDaemonProcess daemonProcess, DaemonProcessKind processKind)
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