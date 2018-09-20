using System.Collections.Generic;
using System.Linq;
using JetBrains.ReSharper.Daemon.CSharp.Stages;
using JetBrains.ReSharper.Feature.Services.Daemon;
using JetBrains.ReSharper.Plugins.Unity.CSharp.Daemon.Errors;
using JetBrains.ReSharper.Plugins.Unity.CSharp.Daemon.Stages.Dispatcher;
using JetBrains.ReSharper.Psi;
using JetBrains.ReSharper.Psi.CSharp.Tree;
using JetBrains.ReSharper.Psi.CSharp.Util;
using JetBrains.ReSharper.Psi.Resolve;
using JetBrains.ReSharper.Psi.Tree;
using JetBrains.Util;

namespace JetBrains.ReSharper.Plugins.Unity.CSharp.Daemon.Stages.Analysis
{
    [ElementProblemAnalyzer(typeof(IInvocationExpression), HighlightingTypes = new[] { typeof(InstantiateWithoutParentWarning) })]
    public class InstantiateWithoutParentProblemAnalyzer : UnityElementProblemAnalyzer<IInvocationExpression>
    {
        private static readonly string ourKnownMethod = "Instantiate";
        
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

            var containingMethodDeclaration = expression.GetContainingNode<IMethodDeclaration>();
            if (containingMethodDeclaration == null)
                return;
            
            IEnumerable<ITreeNode> usages = null;
            var containingParenthesizedExpression = expression.GetContainingParenthesizedExpression();
            var castExpression = CastExpressionNavigator.GetByOp(containingParenthesizedExpression);
            var asExpression = AsExpressionNavigator.GetByOperand(containingParenthesizedExpression);
            var initializer = ExpressionInitializerNavigator.GetByValue(castExpression ?? asExpression ?? containingParenthesizedExpression);
            var declaration = LocalVariableDeclarationNavigator.GetByInitial(initializer);


            var usageProvider = data.GetUsagesProvider();
            if (declaration != null)
            {
                usages = usageProvider.GetUsages(declaration.DeclaredElement, containingMethodDeclaration.Body);
            }
            else
            {
                var assignment = AssignmentExpressionNavigator.GetBySource(castExpression ?? asExpression ?? containingParenthesizedExpression);
                var dest = assignment?.Dest as IReferenceExpression;
                var destInfo = dest?.Reference.Resolve();
                if (destInfo != null && destInfo.ResolveErrorType == ResolveErrorType.OK)
                {
                    usages = usageProvider.GetUsages(destInfo.DeclaredElement.NotNull(), containingMethodDeclaration.Body);
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
                    if (fullReferenceExpression == referenceExpression)
                    {
                        var argument = CSharpArgumentNavigator.GetByValue(fullReferenceExpression.GetContainingParenthesizedExpression());
                        var invocation = InvocationExpressionNavigator.GetByArgument(argument);
                        var invocationMethodDeclaration = invocation?.Reference?.Resolve().DeclaredElement?
                            .GetDeclarations().FirstOrDefault() as IMethodDeclaration;
                        
                        if (invocationMethodDeclaration != null)
                        {
                            var element = argument.MatchingParameter?.Element;
                            if (element == null)
                                continue;
                            if (IsMethodSetTransformParent(invocationMethodDeclaration, usageProvider, element.ShortName))
                            {
                                consumer.AddHighlighting(new InstantiateWithoutParentWarning(reference, expression, null, false));
                                return;
                            }
                        }
                    }
                    else
                    {
                        if (IsUsageSetTransformParent(fullReferenceExpression, out var stayInWorldCoords, out var transform))
                        {
                            consumer.AddHighlighting(new InstantiateWithoutParentWarning(reference, expression, transform, stayInWorldCoords));
                            return;
                        }
                    }
                }
            }
        }

        private bool IsMethodSetTransformParent(IMethodDeclaration methodDeclaration, IUsagesProvider usageService, string elementShortName)
        {
            var body = methodDeclaration.Body;
            
            if (body == null)
                return false;
            
            var de = methodDeclaration.Params.ParameterDeclarations.Single(t => t.NameIdentifier?.Name == elementShortName).DeclaredElement;
            
            if (de == null)
                return false;
            
            var usages = usageService.GetUsages(de, body);

            foreach (var usage in usages)
            {
                if (usage is IReferenceExpression reference)
                {
                    var fullReferenceExpression = ReferenceExpressionNavigator.GetTopByQualifierExpression(reference);
                    if (IsUsageSetTransformParent(fullReferenceExpression, out var _, out var _))
                        return true;
                }
            }

            return false;
        }

        private bool IsUsageSetTransformParent(IReferenceExpression referenceExpression, out bool stayInWorldCoords, out ICSharpExpression expression)
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