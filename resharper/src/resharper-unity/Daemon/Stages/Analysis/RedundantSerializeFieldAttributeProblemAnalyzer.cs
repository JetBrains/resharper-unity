﻿using JetBrains.ReSharper.Daemon.Stages.Dispatcher;
using JetBrains.ReSharper.Feature.Services.Daemon;
using JetBrains.ReSharper.Plugins.Unity.Daemon.Stages.Dispatcher;
using JetBrains.ReSharper.Plugins.Unity.Daemon.Stages.Highlightings;
using JetBrains.ReSharper.Psi;
using JetBrains.ReSharper.Psi.CSharp.Tree;

namespace JetBrains.ReSharper.Plugins.Unity.Daemon.Stages.Analysis
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

            var fieldDeclarations = FieldDeclarationNavigator.GetByAttribute(attribute);
            foreach (var fieldDeclaration in fieldDeclarations)
            {
                if (fieldDeclaration.DeclaredElement.HasAttributeInstance(PredefinedType.NONSERIALIZED_ATTRIBUTE_CLASS,
                    false))
                {
                    consumer.AddHighlighting(new RedundantSerializeFieldAttributeWarning(attribute));
                    return;
                }
            }
        }
    }
}