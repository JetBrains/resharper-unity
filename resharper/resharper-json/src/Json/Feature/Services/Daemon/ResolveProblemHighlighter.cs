using System;
using JetBrains.ReSharper.Feature.Services.Daemon;
using JetBrains.ReSharper.Plugins.Json.Psi;
using JetBrains.ReSharper.Psi.Resolve;
using JetBrains.ReSharper.Psi.Tree;

namespace JetBrains.ReSharper.Plugins.Json.Feature.Services.Daemon
{
    public class ResolveProblemHighlighter
    {
        private readonly ResolveHighlighterRegistrar myResolveHighlighterRegistrar;

        public ResolveProblemHighlighter(ResolveHighlighterRegistrar resolveHighlighterRegistrar)
        {
            myResolveHighlighterRegistrar = resolveHighlighterRegistrar;
        }

        public void CheckForResolveProblems(ReferenceCollection references, IHighlightingConsumer consumer)
        {
            foreach (var reference in references)
                CheckForResolveProblems(consumer, reference);
        }

        private void CheckForResolveProblems(IHighlightingConsumer consumer, IReference reference)
        {
            var error = reference.CheckResolveResult();
            if (error == null)
            {
                throw new InvalidOperationException("ResolveErrorType is null for reference " +
                                                    reference.GetType().FullName);
            }

            if (error.IsAcceptable)
                return;

            // ReSharper disable once AssignNullToNotNullAttribute
            if (myResolveHighlighterRegistrar.ContainsHandler(JsonNewLanguage.Instance, error))
            {
                var highlighting = myResolveHighlighterRegistrar.GetResolveHighlighting(reference, error);
                if (highlighting != null) consumer.AddHighlighting(highlighting);
            }
        }
    }
}