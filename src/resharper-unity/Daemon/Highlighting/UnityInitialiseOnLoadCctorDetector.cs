using JetBrains.ReSharper.Daemon.Stages.Dispatcher;
using JetBrains.ReSharper.Feature.Services.Daemon;
using JetBrains.ReSharper.Plugins.Unity.Feature.Services.Daemon;
using JetBrains.ReSharper.Psi.CSharp.Tree;
using JetBrains.ReSharper.Psi.Tree;

namespace JetBrains.ReSharper.Plugins.Unity.Daemon.Highlighting
{
    [ElementProblemAnalyzer(typeof(IConstructorDeclaration), HighlightingTypes = new[] {typeof(UnityMarkOnGutter)})]
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
                AddGutterMark(element, element.GetNameDocumentRange(), "Called when Unity first launches the editor, the player, or recompiles scripts", consumer);
            }
        }
    }
}