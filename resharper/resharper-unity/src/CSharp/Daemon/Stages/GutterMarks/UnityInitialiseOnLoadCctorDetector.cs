using JetBrains.ReSharper.Feature.Services.Daemon;
using JetBrains.ReSharper.Plugins.Unity.CSharp.Daemon.Errors;
using JetBrains.ReSharper.Plugins.Unity.CSharp.Daemon.Stages.Dispatcher;
using JetBrains.ReSharper.Psi.CSharp.Tree;

namespace JetBrains.ReSharper.Plugins.Unity.CSharp.Daemon.Stages.GutterMarks
{
    [ElementProblemAnalyzer(typeof(IConstructorDeclaration), HighlightingTypes = new[] {typeof(UnityGutterMarkInfo)})]
    public class UnityInitialiseOnLoadCctorDetector : UnityElementProblemAnalyzer<IConstructorDeclaration>
    {
        public UnityInitialiseOnLoadCctorDetector(UnityApi unityApi)
            : base(unityApi)
        {
        }

        protected override void Analyze(IConstructorDeclaration element, ElementProblemAnalyzerData data, IHighlightingConsumer consumer)
        {
            if (!element.IsStatic)
                return;

            var containingType = element.GetContainingTypeDeclaration()?.DeclaredElement;
            if (containingType != null && containingType.HasAttributeInstance(KnownTypes.InitializeOnLoadAttribute, false))
            {
                var highlighting = new UnityGutterMarkInfo(element, "Called when Unity first launches the editor, the player, or recompiles scripts");
                consumer.AddHighlighting(highlighting);
            }
        }
    }
}