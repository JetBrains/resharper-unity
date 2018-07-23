using JetBrains.ReSharper.Feature.Services.Daemon;
using JetBrains.ReSharper.Plugins.Unity.CSharp.Daemon.Errors;
using JetBrains.ReSharper.Plugins.Unity.CSharp.Daemon.Stages.Dispatcher;
using JetBrains.ReSharper.Psi;
using JetBrains.ReSharper.Psi.CSharp.Tree;

namespace JetBrains.ReSharper.Plugins.Unity.CSharp.Daemon.Stages.Analysis
{
    [ElementProblemAnalyzer(typeof(IAttribute),
        HighlightingTypes = new[] { typeof(RedundantHideInInspectorAttributeWarning) })]
    public class RedundantHideInInspectorAttributeProblemAnalyzer : UnityElementProblemAnalyzer<IAttribute>
    {
        public RedundantHideInInspectorAttributeProblemAnalyzer(UnityApi unityApi)
            : base(unityApi)
        {
        }

        protected override void Analyze(IAttribute attribute, ElementProblemAnalyzerData data, IHighlightingConsumer consumer)
        {
            if (!(attribute.TypeReference?.Resolve().DeclaredElement is ITypeElement attributeTypeElement))
                return;

            if (!Equals(attributeTypeElement.GetClrName(), KnownTypes.HideInInspector))
                return;

            var fields = attribute.GetFieldsByAttribute();
            foreach (var field in fields)
            {
                if (!Api.IsSerialisedField(field))
                {
                    consumer.AddHighlighting(new RedundantHideInInspectorAttributeWarning(attribute));
                    return;
                }
            }
        }
    }
}