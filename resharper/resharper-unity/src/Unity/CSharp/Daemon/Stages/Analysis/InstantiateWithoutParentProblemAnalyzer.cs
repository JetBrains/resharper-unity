#nullable enable

using System.Collections.Generic;
using System.Linq;
using JetBrains.Diagnostics;
using JetBrains.ReSharper.Daemon.CSharp.Stages;
using JetBrains.ReSharper.Feature.Services.Daemon;
using JetBrains.ReSharper.Plugins.Unity.CSharp.Daemon.Errors;
using JetBrains.ReSharper.Plugins.Unity.UnityEditorIntegration.Api;
using JetBrains.ReSharper.Plugins.Unity.Utils;
using JetBrains.ReSharper.Psi;
using JetBrains.ReSharper.Psi.CSharp.Tree;
using JetBrains.ReSharper.Psi.CSharp.Util;
using JetBrains.ReSharper.Psi.Resolve;
using JetBrains.ReSharper.Psi.Tree;

namespace JetBrains.ReSharper.Plugins.Unity.CSharp.Daemon.Stages.Analysis
{
    [ElementProblemAnalyzer(typeof(IInvocationExpression), HighlightingTypes = new[] { typeof(InstantiateWithoutParentWarning) })]
    public class InstantiateWithoutParentProblemAnalyzer : UnityElementProblemAnalyzer<IInvocationExpression>
    {
        private const string KnownMethod = "Instantiate";

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
            if (method is not { ShortName: KnownMethod })
                return;

            if (method.ContainingType?.GetClrName().Equals(KnownTypes.Object) != true)
                return;

            var parameters = method.Parameters;
            if (parameters.Count != 1)
                return;

            var scope = expression.GetContainingFunctionLikeDeclarationOrClosure().GetCodeBody().GetAnyTreeNode();
            if (scope == null)
                return;

            IEnumerable<ITreeNode>? usages;
            ITreeNode? storage;
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
                if (dest != null && destInfo != null && destInfo.ResolveErrorType == ResolveErrorType.OK)
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
                            return;
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
            if (treeNode is not IReferenceExpression referenceExpression)
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

                if ((firstParent is IThisExpression && secondParent is not IThisExpression) ||
                    (firstParent is not IThisExpression && secondParent is IThisExpression))
                {
                    return false;
                }

                if (firstParent is not IReferenceExpression firstParentExpression
                    || secondParent is not IReferenceExpression secondParentExpression)
                {
                    return false;
                }

                referenceExpression = firstParentExpression;
                dest = secondParentExpression;
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

        private bool IsUsageSetTransformParent(IReferenceExpression referenceExpression, out bool stayInWorldCoords,out ICSharpExpression? expression)
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
                    // TODO: Use conditional access when the monorepo build uses a more modern C# compiler
                    // Currently (as of 01/2023) the monorepo build for Unity uses C#9 compiler, which will complain
                    // that the out variable is uninitialised when we use conditional access
                    // See also https://youtrack.jetbrains.com/issue/RSRP-489147
                    var argument = invocation.Arguments[1].Value;
                    if (argument != null && argument.ConstantValue.IsBoolean(out var constantValue))
                        stayInWorldCoords = constantValue;
                    else
                        return false;
                }
            }

            var containingType = declaredElement.GetContainingType();
            return containingType != null && containingType.GetClrName().Equals(KnownTypes.Transform);
        }
    }
}
