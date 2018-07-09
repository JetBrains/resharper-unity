using System.Linq;
using JetBrains.ReSharper.Feature.Services.Daemon;
using JetBrains.ReSharper.Plugins.Unity.CSharp.Daemon.Errors;
using JetBrains.ReSharper.Plugins.Unity.CSharp.Daemon.Stages.Dispatcher;
using JetBrains.ReSharper.Psi;
using JetBrains.ReSharper.Psi.CSharp.Impl;
using JetBrains.ReSharper.Psi.CSharp.Tree;

namespace JetBrains.ReSharper.Plugins.Unity.CSharp.Daemon.Stages.Analysis
{
    [ElementProblemAnalyzer(typeof(IAttribute),
        HighlightingTypes = new[]
        {
            typeof(RedundantFormerlySerializedAsAttributeWarning),
            typeof(PossibleMisapplicationOfAttributeToMultipleFieldsWarning)
        })]
    public class FormerlySerializedAsAttributeProblemAnalyzer : UnityElementProblemAnalyzer<IAttribute>
    {
        public FormerlySerializedAsAttributeProblemAnalyzer(UnityApi unityApi)
            : base(unityApi)
        {
        }

        protected override void Analyze(IAttribute attribute, ElementProblemAnalyzerData data, IHighlightingConsumer consumer)
        {
            if (!(attribute.TypeReference?.Resolve().DeclaredElement is ITypeElement attributeTypeElement))
                return;

            if (!Equals(attributeTypeElement.GetClrName(), KnownTypes.FormerlySerializedAsAttribute))
                return;

            var fields = attribute.GetFieldsByAttribute();
            if (fields.Count > 1)
            {
                // It doesn't make sense to apply FormerlySerializedAs to a multiple field declaration, e.g.
                // [FormerlySerializedAs('cheese')] public int cheese, grapes, wine;
                // Because this will apply the attribute to ALL fields
                consumer.AddHighlighting(new PossibleMisapplicationOfAttributeToMultipleFieldsWarning(attribute));
                return;
            }

            var field = fields.First();
            if (!Api.IsSerialisedField(field))
            {
                consumer.AddHighlighting(new RedundantFormerlySerializedAsAttributeWarning(attribute));
                return;
            }

            var attributeInstance = attribute.GetAttributeInstance();
            var nameParameter = attributeInstance.PositionParameter(0);
            if (nameParameter.IsConstant && nameParameter.ConstantValue.IsString() &&
                (string) nameParameter.ConstantValue.Value == field.ShortName)
            {
                consumer.AddHighlighting(new RedundantFormerlySerializedAsAttributeWarning(attribute));
            }
        }
    }
}