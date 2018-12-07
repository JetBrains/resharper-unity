using JetBrains.ReSharper.Feature.Services.Daemon;
using JetBrains.ReSharper.Plugins.Unity.CSharp.Daemon.Errors;
using JetBrains.ReSharper.Plugins.Unity.CSharp.Daemon.Stages.Dispatcher;
using JetBrains.ReSharper.Psi;
using JetBrains.ReSharper.Psi.CSharp.Tree;
using JetBrains.ReSharper.Psi.Resolve;

namespace JetBrains.ReSharper.Plugins.Unity.CSharp.Daemon.Stages.Analysis
{
    [ElementProblemAnalyzer(typeof(IInvocationExpression), HighlightingTypes =
        new[] {typeof(PropertyDrawerOnGUIBaseWarning)})]
    public class PropertyDrawerOnGUIBaseProblemAnalyzer : UnityElementProblemAnalyzer<IInvocationExpression>
    {
        public PropertyDrawerOnGUIBaseProblemAnalyzer(UnityApi unityApi)
            : base(unityApi)
        {
        }

        protected override void Analyze(IInvocationExpression expression, ElementProblemAnalyzerData data,
            IHighlightingConsumer consumer)
        {
            if (IsOnGUIBaseCall(expression) &&
                IsInsidePropertyDrawer(expression) &&
                IsInsideOnGUI(expression))
            {
                var invocation = (expression.InvokedExpression as IReferenceExpression)?.Reference;
                consumer.AddHighlighting(new PropertyDrawerOnGUIBaseWarning(invocation));
            }
        }

        private static bool IsOnGUIBaseCall(IInvocationExpression expression)
        {
            var reference = expression.Reference;
            if (reference == null)
                return false;

            if (!IsBaseCallReference(expression))
                return false;

            var info = reference.Resolve();

            if (info.ResolveErrorType != ResolveErrorType.OK)
                return false;

            var method = info.DeclaredElement as IMethod;
            if (method == null)
                return false;

            var doesNameMatch = method.ShortName == "OnGUI";
            if (!doesNameMatch)
                return false;

            return true;
        }
        
        private static bool IsBaseCallReference(IInvocationExpression expression)
        {
            var referenceExpression = expression.InvokedExpression as IReferenceExpression;
            return referenceExpression?.QualifierExpression is IBaseExpression;
        }

        private static bool IsInsideOnGUI(IInvocationExpression expression)
        {
            // Make sure we are inside the OnGUI() method to only catch accidental usages
            // If someone intentionally wants to do this, they probably know what they are doing
            var methodDeclaration = expression.GetContainingNode<IMethodDeclaration>();
            if (methodDeclaration == null)
                return false;

            var name = methodDeclaration.NameIdentifier?.Name;
            if (name != "OnGUI")
                return false;

            var isOverride = methodDeclaration.IsOverride;
            if (!isOverride)
                return false;

            return true;
        }

        private static bool IsInsidePropertyDrawer(IInvocationExpression expression)
        {
            var containingType = expression.GetContainingNode<IClassLikeDeclaration>()?.DeclaredElement;
            var propertyDrawer = TypeFactory.CreateTypeByCLRName(KnownTypes.PropertyDrawer, expression.PsiModule);
            return containingType?.IsDescendantOf(propertyDrawer.GetTypeElement()) != false;
        }
    }
}