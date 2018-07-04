using JetBrains.ReSharper.Feature.Services.Daemon;
using JetBrains.ReSharper.Plugins.Unity.CSharp.Daemon.Errors;
using JetBrains.ReSharper.Plugins.Unity.CSharp.Daemon.Stages.Dispatcher;
using JetBrains.ReSharper.Psi;
using JetBrains.ReSharper.Psi.CSharp.Tree;

namespace JetBrains.ReSharper.Plugins.Unity.CSharp.Daemon.Stages.Analysis
{
    [ElementProblemAnalyzer(typeof(IAttribute),
        HighlightingTypes = new[] { typeof(RedundantInitializeOnLoadAttributeWarning) })]
    public class RedundantInitializeOnLoadAttributeProblemAnalyzer : UnityElementProblemAnalyzer<IAttribute>
    {
        public RedundantInitializeOnLoadAttributeProblemAnalyzer(UnityApi unityApi)
            : base(unityApi)
        {
        }

        protected override void Analyze(IAttribute element, ElementProblemAnalyzerData data, IHighlightingConsumer consumer)
        {
            if (!(element.TypeReference?.Resolve().DeclaredElement is ITypeElement attributeTypeElement))
                return;

            if (Equals(attributeTypeElement.GetClrName(), KnownTypes.InitializeOnLoadAttribute))
            {
                var classLikeDeclaration = ClassLikeDeclarationNavigator.GetByAttribute(element);
                if (classLikeDeclaration != null && classLikeDeclaration.ConstructorDeclarations.All(c => !c.IsStatic))
                {
                    // Unity doesn't report a warning if there isn't a static constructor, so just highlight
                    // the attribute as dead code.
                    consumer.AddHighlighting(new RedundantInitializeOnLoadAttributeWarning(element));
                }
            }
        }
    }
}