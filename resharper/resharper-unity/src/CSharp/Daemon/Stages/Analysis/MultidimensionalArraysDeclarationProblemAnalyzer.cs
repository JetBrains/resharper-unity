using System.Linq;
using JetBrains.Annotations;
using JetBrains.ReSharper.Daemon.CSharp.Stages;
using JetBrains.ReSharper.Feature.Services.Daemon;
using JetBrains.ReSharper.Plugins.Unity.CSharp.Daemon.Errors;
using JetBrains.ReSharper.Plugins.Unity.CSharp.Daemon.Stages.Dispatcher;
using JetBrains.ReSharper.Psi;
using JetBrains.ReSharper.Psi.CSharp;
using JetBrains.ReSharper.Psi.CSharp.Tree;
using JetBrains.ReSharper.Psi.Pointers;
using JetBrains.ReSharper.Psi.Tree;

namespace JetBrains.ReSharper.Plugins.Unity.CSharp.Daemon.Stages.Analysis
{
    [ElementProblemAnalyzer(typeof(IVariableDeclaration), typeof(InefficientMultidimensionalArrayUsageWarning))]
    public class MultidimensionalArraysDeclarationProblemAnalyzer : UnityElementProblemAnalyzer<IVariableDeclaration>
    {
        public MultidimensionalArraysDeclarationProblemAnalyzer([NotNull] UnityApi unityApi)
            : base(unityApi)
        {
        }

        protected override void Analyze(IVariableDeclaration element, ElementProblemAnalyzerData data, IHighlightingConsumer consumer)
        {
            var declaredElement = element.DeclaredElement;
            if (declaredElement == null)
                return;

            var classDeclaration = element.GetContainingNode<IClassLikeDeclaration>();
            if (classDeclaration == null)
                return;

            var type = element.Type as IArrayType;
            if (type == null)
                return;
            
            if (type.Rank == 1)
                return;
            
            IArrayCreationExpression arrayCreationExpression = null;
            IMultipleDeclaration multipleDeclaration = null;

            switch (element)
            {
                case IFieldDeclaration fieldDeclaration:
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
            }

            // Rare case, ignore it
            if (multipleDeclaration == null || multipleDeclaration.Declarators.Count > 1)
                return;

            var usages = data.GetUsagesProvider().GetUsages(declaredElement, classDeclaration).ToList();

            foreach (var usage in usages)
            {
                if (ElementAccessExpressionNavigator.GetByOperand(usage as ICSharpExpression) == null) 
                    return;
            }
            
            // Don't know where we should insert array initialization (constructor?, which constructor if there are several constructors?)
            if (usages.Count > 0 && element is IFieldDeclaration && arrayCreationExpression?.ArrayInitializer == null)
                return;

            if (arrayCreationExpression.ContainsErrorElement())
                return;
            
            consumer.AddHighlighting(new InefficientMultidimensionalArrayUsageWarning(usages.Select(t => t.CreateTreeElementPointer()).ToList(),
                arrayCreationExpression, element));
        }
        
    }
}