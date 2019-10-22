﻿using JetBrains.ProjectModel;
using JetBrains.ReSharper.Feature.Services.Daemon;
using JetBrains.ReSharper.Plugins.Unity.AsmdefNew;
using JetBrains.ReSharper.Plugins.Unity.AsmDefNew.Daemon.Errors;
using JetBrains.ReSharper.Plugins.Unity.AsmdefNew.Psi.Caches;
using JetBrains.ReSharper.Plugins.Unity.AsmdefNew.Psi.Resolve;
using JetBrains.ReSharper.Plugins.Unity.JsonNew.Psi.Tree;
using JetBrains.ReSharper.Psi.Tree;

namespace JetBrains.ReSharper.Plugins.Unity.AsmDefNew.Feature.Services.Daemon
{
    [ElementProblemAnalyzer(typeof(IJsonNewLiteralExpression))]
    public class AsmDefReferencingSelfProblemAnalyzer : AsmDefProblemAnalyzer<IJsonNewLiteralExpression>
    {
        protected override void Analyze(IJsonNewLiteralExpression element, ElementProblemAnalyzerData data, IHighlightingConsumer consumer)
        {
            if (element.IsReferenceLiteral())
            {
                var nameCache = data.Solution.GetComponent<AsmDefNameCache>();
                var nameDeclaredElement = nameCache.GetNameDeclaredElement(data.SourceFile);
                var reference = element.FindReference<AsmDefNameReference>();
                if (reference != null && nameDeclaredElement != null &&
                    Equals(reference.Resolve().DeclaredElement, nameDeclaredElement))
                {
                    consumer.AddHighlighting(new ReferencingSelfError(reference));
                }
            }
        }
    }
}