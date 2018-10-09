using System.Collections.Generic;
using System.Linq;
using JetBrains.Annotations;
using JetBrains.ReSharper.Daemon.CSharp.Stages;
using JetBrains.ReSharper.Feature.Services.Daemon;
using JetBrains.ReSharper.Plugins.Unity.CSharp.Daemon.Errors;
using JetBrains.ReSharper.Plugins.Unity.CSharp.Daemon.Stages.Dispatcher;
using JetBrains.ReSharper.Plugins.Unity.Utils;
using JetBrains.ReSharper.Psi;
using JetBrains.ReSharper.Psi.CSharp.Tree;
using JetBrains.ReSharper.Psi.CSharp.Util;
using JetBrains.ReSharper.Psi.Resolve;
using JetBrains.ReSharper.Psi.Tree;
using JetBrains.Util;
using QualifierEqualityComparer = JetBrains.ReSharper.Psi.CSharp.Impl.ControlFlow.ControlFlowWeakVariableInfo.QualifierEqualityComparer;

namespace JetBrains.ReSharper.Plugins.Unity.CSharp.Daemon.Stages.Analysis
{
    [ElementProblemAnalyzer(typeof(IInvocationExpression), HighlightingTypes = new[] { typeof(InstantiateWithoutParentWarning) })]
    public class InstantiateWithoutParentProblemAnalyzer : UnityElementProblemAnalyzer<IInvocationExpression>
    {
        private static readonly string ourKnownMethod = "Instantiate";
        private static readonly QualifierEqualityComparer ourComparer = new QualifierEqualityComparer();

        public InstantiateWithoutParentProblemAnalyzer(UnityApi unityApi)
            : base(unityApi)
        {
        }

        protected override void Analyze(IInvocationExpression expression, ElementProblemAnalyzerData data, IHighlightingConsumer consumer)
        {
            var reference = (expression.InvokedExpression as IReferenceExpression)?.Reference;
            if (reference == null)
                return;
            
            var info = reference.Resolve();
            if (info.ResolveErrorType != ResolveErrorType.OK) 
                return;

            var method = info.DeclaredElement as IMethod;
            if (method == null)
                return;
            
            if (!method.ShortName.Equals(ourKnownMethod)) 
                return;

            if (method.GetContainingType()?.GetClrName().Equals(KnownTypes.Object) != true)
                return;

           
            var parameters = method.Parameters;
            if (parameters.Count != 1) 
                return;

            var scope = expression.GetContainingFunctionLikeDeclarationOrClosure().GetCodeBody().GetAnyTreeNode();
            if (scope == null)
                return;
            
            IEnumerable<ITreeNode> usages = null;
            ITreeNode storage = null;
            var containingParenthesizedExpression = expression.GetContainingParenthesizedExpression();
            var castExpression = CastExpressionNavigator.GetByOp(containingParenthesizedExpression);
            var asExpression = AsExpressionNavigator.GetByOperand(containingParenthesizedExpression);
            var initializer = ExpressionInitializerNavigator.GetByValue(castExpression ?? asExpression ?? containingParenthesizedExpression);
            var declaration = LocalVariableDeclarationNavigator.GetByInitial(initializer);


            var usageProvider = data.GetUsagesProvider();
            if (declaration != null)
            {
                usages = usageProvider.GetUsages(declaration.DeclaredElement, scope).Where(t => t is IReferenceExpression);
                storage = declaration;
            }
            else
            {
                var assignment = AssignmentExpressionNavigator.GetBySource(castExpression ?? asExpression ?? containingParenthesizedExpression);
                var dest = assignment?.Dest as IReferenceExpression;
                var destInfo = dest?.Reference.Resolve();
                if (destInfo != null && destInfo.ResolveErrorType == ResolveErrorType.OK)
                {
                    usages = usageProvider.GetUsages(destInfo.DeclaredElement.NotNull(), scope).Where(t => IsSameReferenceUsed(t, dest));
                    storage = dest;
                }
                else
                {
                    return;
                }
            }

            foreach (var usage in usages)
            {
                if (usage is IReferenceExpression referenceExpression)
                {
                    var fullReferenceExpression = ReferenceExpressionNavigator.GetTopByQualifierExpression(referenceExpression);
                    if (IsUsageSetTransformParent(fullReferenceExpression, out var stayInWorldCoords, out var transform))
                    {
                        if (!InSameBlock(fullReferenceExpression, storage))
                        {
                            return;
                        }
                        var finder = new TransformParentRelatedReferenceFinder(referenceExpression);
                        var relatedExpressions = finder.GetRelatedExpressions(scope, expression).FirstOrDefault();
                        if (relatedExpressions == null || relatedExpressions.GetTreeStartOffset() >= fullReferenceExpression.GetTreeStartOffset())
                            consumer.AddHighlighting(new InstantiateWithoutParentWarning(fullReferenceExpression, expression, transform, stayInWorldCoords));
                        return;
                    }
                }
            }
        }

        private bool IsSameReferenceUsed(ITreeNode treeNode, IReferenceExpression dest)
        {
            if (!(treeNode is IReferenceExpression referenceExpression))
                return false;

            while (true)
            {
                var firstDeclaredElement = referenceExpression.Reference.Resolve().DeclaredElement;
                if (firstDeclaredElement == null)
                    return false;
                
                var secondDeclaredElement = dest.Reference.Resolve().DeclaredElement;
                if (secondDeclaredElement == null)
                    return false;

                if (!firstDeclaredElement.Equals(secondDeclaredElement))
                    return false;

                var firstParent = referenceExpression.QualifierExpression;
                var secondParent = dest.QualifierExpression;
                
                if (firstParent == null && secondParent == null)
                    return true;
                
                if (firstParent is IThisExpression && !(secondParent is IThisExpression) ||
                    !(firstParent is IThisExpression) && secondParent is IThisExpression)
                {
                    return false;
                }
                
                referenceExpression = firstParent as IReferenceExpression;
                dest = secondParent as IReferenceExpression;
                if (referenceExpression == null || dest == null)
                    return false;
            }
        }

        private bool InSameBlock(IReferenceExpression fullReferenceExpression, ITreeNode storage)
        {
            var firstStatement = fullReferenceExpression.GetContainingStatement();
            var secondStatement = storage.GetContainingNode<IStatement>();
            if (firstStatement == null || secondStatement == null)
                return false;
            return firstStatement.Parent == secondStatement.Parent;
        }

        private bool IsUsageSetTransformParent([NotNull]IReferenceExpression referenceExpression, out bool stayInWorldCoords,[CanBeNull] out ICSharpExpression expression)
        {
            stayInWorldCoords = true;
            expression = null;
            var declaredElement = referenceExpression.Reference.Resolve().DeclaredElement as IClrDeclaredElement;

            if (declaredElement == null)
                return false;
                        
            if (declaredElement is IProperty property)
            {
                expression = AssignmentExpressionNavigator.GetByDest(referenceExpression)?.Source;
                if (!property.ShortName.Equals("parent")) 
                    return false;
            }
                        
            if (declaredElement is IMethod setParentMethod)
            {
                if (!setParentMethod.ShortName.Equals("SetParent"))
                    return false;
                
                var invocation = InvocationExpressionNavigator.GetByInvokedExpression(referenceExpression);
                if (invocation == null)
                    return false;
                expression = invocation.Arguments[0].Value;
                if (setParentMethod.Parameters.Count == 2)
                {
                    var argument = invocation.Arguments[1].Value;
                    if (argument?.ConstantValue.Value is bool constantValue)
                    {
                        stayInWorldCoords = constantValue;
                    }
                    else
                    {
                        return false;
                    }
                }
            }
                        
            var containingType = declaredElement.GetContainingType();
            if (containingType != null && containingType.GetClrName().Equals(KnownTypes.Transform))
            {
                return true;
            }

            return false;
        }
    }
}