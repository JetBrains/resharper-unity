using JetBrains.ReSharper.Feature.Services.Daemon;
using JetBrains.ReSharper.Plugins.Unity.CSharp.Daemon.Errors;
using JetBrains.ReSharper.Plugins.Unity.CSharp.Daemon.Stages.Dispatcher;
using JetBrains.ReSharper.Psi;
using JetBrains.ReSharper.Psi.CSharp.Tree;
using JetBrains.ReSharper.Psi.Util;

namespace JetBrains.ReSharper.Plugins.Unity.CSharp.Daemon.Stages.Analysis
{
    [ElementProblemAnalyzer(typeof(IObjectCreationExpression),
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
            var createdType = element.ExplicitType();
            var monoBehaviourType = TypeFactory.CreateTypeByCLRName(KnownTypes.MonoBehaviour, element.GetPsiModule());
            var scriptableObjectType =
                TypeFactory.CreateTypeByCLRName(KnownTypes.ScriptableObject, element.GetPsiModule());

            if (createdType.IsSubtypeOf(monoBehaviourType))
            {
                consumer.AddHighlighting(new IncorrectMonoBehaviourInstantiationWarning(element));
            }
            else if (createdType.IsSubtypeOf(scriptableObjectType))
            {
                consumer.AddHighlighting(new IncorrectScriptableObjectInstantiationWarning(element));
            }
        }
    }
}