using JetBrains.ReSharper.Feature.Services.Daemon;
using JetBrains.ReSharper.Plugins.Unity.CSharp.Daemon.Errors;
using JetBrains.ReSharper.Plugins.Unity.CSharp.Daemon.Stages.Dispatcher;
using JetBrains.ReSharper.Psi;
using JetBrains.ReSharper.Psi.CSharp.Tree;
using JetBrains.ReSharper.Psi.Resolve;

namespace JetBrains.ReSharper.Plugins.Unity.CSharp.Daemon.Stages.Analysis
{
    [ElementProblemAnalyzer(typeof(IReferenceExpression),
        HighlightingTypes = new[] {typeof(InefficientCameraMainUsageWarning)})]
    public class InefficientCameraMainUsageProblemAnalyzer : UnityElementProblemAnalyzer<IReferenceExpression>
    {
        public InefficientCameraMainUsageProblemAnalyzer(UnityApi unityApi)
            : base(unityApi)
        {
        }

        protected override void Analyze(IReferenceExpression expression, ElementProblemAnalyzerData data,
            IHighlightingConsumer consumer)
        {
            if (IsCameraMainReference(expression) && IsInsideFrequentlyCalledMethod(expression))
                consumer.AddHighlighting(new InefficientCameraMainUsageWarning(expression));
        }

        private static bool IsCameraMainReference(IReferenceExpression expression)
        {
            if (expression.NameIdentifier?.Name == "main")
            {
                var info = expression.Reference.Resolve();
                if (info.ResolveErrorType == ResolveErrorType.OK)
                {
                    var property = info.DeclaredElement as IProperty;
                    var containingType = property?.GetContainingType();
                    if (containingType != null)
                        return KnownTypes.Camera.Equals(containingType.GetClrName());
                }
            }

            return false;
        }

        // These methods are called every frame
        // See https://docs.unity3d.com/Manual/ExecutionOrder.html
        // TODO: Should we also look at the rendering methods?
        private bool IsInsideFrequentlyCalledMethod(IReferenceExpression expression)
        {
            var methodDeclaration = expression.GetContainingNode<IMethodDeclaration>();
            if (methodDeclaration == null)
                return false;

            var name = methodDeclaration.NameIdentifier?.Name;
            if (!(name == "Update" || name == "FixedUpdate" || name == "LateUpdate"))
                return false;

            var method = methodDeclaration.DeclaredElement;
            if (method == null || !Api.IsEventFunction(method))
                return false;

            return true;
        }
    }
}