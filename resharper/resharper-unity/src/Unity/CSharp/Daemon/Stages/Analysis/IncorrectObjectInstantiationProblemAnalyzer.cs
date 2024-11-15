using JetBrains.Application.Parts;
using JetBrains.ReSharper.Feature.Services.Daemon;
using JetBrains.ReSharper.Plugins.Unity.CSharp.Daemon.Errors;
using JetBrains.ReSharper.Plugins.Unity.UnityEditorIntegration.Api;
using JetBrains.ReSharper.Plugins.Unity.Utils;
using JetBrains.ReSharper.Psi.CSharp.Tree;
using JetBrains.ReSharper.Psi.Util;

namespace JetBrains.ReSharper.Plugins.Unity.CSharp.Daemon.Stages.Analysis
{
    [ElementProblemAnalyzer(Instantiation.DemandAnyThreadSafe, typeof(IObjectCreationExpression),
        HighlightingTypes = new[]
        {
            typeof(IncorrectMonoBehaviourInstantiationWarning),
            typeof(IncorrectScriptableObjectInstantiationWarning)
        })]
    public class IncorrectObjectInstantiationProblemAnalyzer : UnityElementProblemAnalyzer<IObjectCreationExpression>
    {
        public IncorrectObjectInstantiationProblemAnalyzer(UnityApi unityApi)
            : base(unityApi)
        {
        }

        protected override void Analyze(IObjectCreationExpression element, ElementProblemAnalyzerData data, IHighlightingConsumer consumer)
        {
            var createdType = element.ExplicitType().GetTypeElement();

            if (createdType.DerivesFromMonoBehaviour())
            {
                consumer.AddHighlighting(new IncorrectMonoBehaviourInstantiationWarning(element));
            }
            else if (createdType.DerivesFromScriptableObject())
            {
                consumer.AddHighlighting(new IncorrectScriptableObjectInstantiationWarning(element));
            }
        }
    }
}