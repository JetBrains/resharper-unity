using JetBrains.ReSharper.Feature.Services.Daemon;
using JetBrains.ReSharper.Plugins.Unity.CSharp.Daemon.Errors;
using JetBrains.ReSharper.Plugins.Unity.UnityEditorIntegration.Api;
using JetBrains.ReSharper.Psi;
using JetBrains.ReSharper.Psi.CSharp.Tree;

namespace JetBrains.ReSharper.Plugins.Unity.CSharp.Daemon.Stages.Analysis
{
    [ElementProblemAnalyzer(typeof(IAttribute), HighlightingTypes = new[] { typeof(InvalidStaticModifierWarning) })]
    public class NonStaticMenuItemProblemAnalyzer : UnityElementProblemAnalyzer<IAttribute>
    {
        public NonStaticMenuItemProblemAnalyzer(UnityApi unityApi)
            : base(unityApi)
        {
        }

        protected override void Analyze(IAttribute element, ElementProblemAnalyzerData data, IHighlightingConsumer consumer)
        {
            if (element.TypeReference?.Resolve().DeclaredElement is ITypeElement type &&
                Equals(type.GetClrName(), KnownTypes.MenuItemAttribute))
            {
                var methodDeclaration = element.GetContainingNode<IMethodDeclaration>();
                var declaredElement = methodDeclaration?.DeclaredElement;
                if (declaredElement is { IsStatic: false })
                {
                    consumer.AddHighlighting(new InvalidStaticModifierWarning(methodDeclaration, new MethodSignature(declaredElement.ReturnType, true)));
                }
            }
        }
    }
}