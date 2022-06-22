#nullable enable

using System;
using System.Linq;
using JetBrains.ReSharper.Feature.Services.Daemon;
using JetBrains.ReSharper.Plugins.Unity.CSharp.Daemon.Errors;
using JetBrains.ReSharper.Plugins.Unity.UnityEditorIntegration.Api;
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
            if (attribute.TypeReference?.Resolve().DeclaredElement is not ITypeElement attributeTypeElement)
                return;

            if (!Equals(attributeTypeElement.GetClrName(), KnownTypes.FormerlySerializedAsAttribute))
                return;

            var fields = AttributesOwnerDeclarationNavigator.GetByAttribute(attribute).OfType<IField>().ToList();
            if (fields.Count == 0)
            {
                // The attribute is either on an invalid target (which is already an error), or it's got the field:
                // target, in which case, we can't validate the name because we can't predict the name of the backing
                // field
                // TODO: Validate that the backing field is serialisable
                // I.e. it's a private field so requires the [field: SerializeField] attribute, and make sure the type
                // of the property is serialisable
                return;
            }

            if (fields.Count > 1)
            {
                // It doesn't make sense to apply FormerlySerializedAs to a multiple field declaration, e.g.
                // [FormerlySerializedAs('cheese')] public int cheese, grapes, wine;
                // Because this will apply the attribute to ALL fields
                consumer.AddHighlighting(new PossibleMisapplicationOfAttributeToMultipleFieldsWarning(attribute));
                return;
            }

            var field = fields[0];
            if (!Api.IsSerialisedField(field))
            {
                consumer.AddHighlighting(new RedundantFormerlySerializedAsAttributeWarning(attribute));
                return;
            }

            var attributeInstance = attribute.GetAttributeInstance();
            var nameParameter = attributeInstance.PositionParameter(0);
            if (nameParameter.IsConstant && nameParameter.ConstantValue.IsString(out var value) &&
                string.Equals(value, field.ShortName, StringComparison.Ordinal))
            {
                consumer.AddHighlighting(new RedundantFormerlySerializedAsAttributeWarning(attribute));
            }
        }
    }
}
