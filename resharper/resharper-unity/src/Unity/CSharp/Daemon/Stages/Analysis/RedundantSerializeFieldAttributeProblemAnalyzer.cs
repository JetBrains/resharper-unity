﻿#nullable enable

using JetBrains.ReSharper.Feature.Services.Daemon;
using JetBrains.ReSharper.Plugins.Unity.CSharp.Daemon.Errors;
using JetBrains.ReSharper.Plugins.Unity.UnityEditorIntegration.Api;
using JetBrains.ReSharper.Psi;
using JetBrains.ReSharper.Psi.CSharp.Tree;

namespace JetBrains.ReSharper.Plugins.Unity.CSharp.Daemon.Stages.Analysis
{
    [ElementProblemAnalyzer(typeof(IAttribute),
        HighlightingTypes = new[] { typeof(RedundantSerializeFieldAttributeWarning) })]
    public class RedundantSerializeFieldAttributeProblemAnalyzer : UnityElementProblemAnalyzer<IAttribute>
    {
        public RedundantSerializeFieldAttributeProblemAnalyzer(UnityApi unityApi)
            : base(unityApi)
        {
        }

        protected override void Analyze(IAttribute attribute, ElementProblemAnalyzerData data, IHighlightingConsumer consumer)
        {
            if (!(attribute.TypeReference?.Resolve().DeclaredElement is ITypeElement attributeTypeElement))
                return;

            if (!Equals(attributeTypeElement.GetClrName(), KnownTypes.SerializeField))
                return;

            foreach (var declaration in AttributesOwnerDeclarationNavigator.GetByAttribute(attribute))
            {
                if (declaration.DeclaredElement is IField field && !Api.IsSerialisedField(field)
                    || (declaration.DeclaredElement is IProperty property && attribute.Target == AttributeTarget.Field
                                                                          && !Api.IsSerialisedAutoProperty(property, attribute)))
                {
                    consumer.AddHighlighting(new RedundantSerializeFieldAttributeWarning(attribute));
                    return;
                }
            }
        }
    }
}
