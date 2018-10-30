using System;
using JetBrains.ReSharper.Feature.Services.Daemon;
using JetBrains.ReSharper.Plugins.Unity.ShaderLab.Daemon.Errors;
using JetBrains.ReSharper.Plugins.Unity.ShaderLab.Psi;
using JetBrains.ReSharper.Psi.Resolve;
using JetBrains.ReSharper.Psi.Tree;

namespace JetBrains.ReSharper.Plugins.Unity.ShaderLab.Daemon.Stages.Resolve
{
    internal class ResolveProblemHighlighter
    {
        private readonly ResolveHighlighterRegistrar myResolveHighlighterRegistrar;

        public ResolveProblemHighlighter(ResolveHighlighterRegistrar resolveHighlighterRegistrar)
        {
            myResolveHighlighterRegistrar = resolveHighlighterRegistrar;
        }

        public void CheckForResolveProblems(ITreeNode node, IHighlightingConsumer consumer,
            ReferenceCollection references)
        {
            foreach (var reference in references)
                CheckForResolveProblems(consumer, reference);
        }

        private void CheckForResolveProblems(IHighlightingConsumer consumer, IReference reference)
        {
            var error = reference.CheckResolveResult();
            if (error == null)
                throw new InvalidOperationException("ResolveErrorType is null for reference " +
                                                    reference.GetType().FullName);

            if (error == ResolveErrorType.OK)
                return;

            // ReSharper disable once AssignNullToNotNullAttribute
            if (myResolveHighlighterRegistrar.ContainsHandler(ShaderLabLanguage.Instance, error))
            {
                var highlighting = myResolveHighlighterRegistrar.GetResolveHighlighting(reference, error);
                if (highlighting != null)
                    consumer.AddHighlighting(highlighting);
            }
            else
            {
                // The default is unresolved. But we can't use something like NotResolvedError because:
                // a) Using an undeclared property in ShaderLab isn't a compile time error. An undeclared
                //    property just uses the default value
                // b) Properties can be set from code, without having to be declared first
                // c) There is at least one hidden property `unity_GUIZTestMode`
                //
                // So, this could be:
                // a) a typo
                // b) an external reference
                //
                // How do we tell the difference, and how do we show the difference?
                // For now, just don't give an error
                consumer.AddHighlighting(new NotResolvedPropertyWarning(reference));
            }
        }
    }
}