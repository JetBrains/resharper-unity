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

    public void CheckForResolveProblems(ITreeNode node, IHighlightingConsumer consumer, ReferenceCollection references)
    {
      foreach (var reference in references)
        CheckForResolveProblems(consumer, reference);
    }

    private void CheckForResolveProblems(IHighlightingConsumer consumer, IReference reference)
    {
      var error = reference.CheckResolveResult();
      if (error == null)
        throw new InvalidOperationException("ResolveErrorType is null for reference " + reference.GetType().FullName);

      if (error == ResolveErrorType.OK)
        return;

      if (myResolveHighlighterRegistrar.ContainsHandler(ShaderLabLanguage.Instance, error))
      {
        var highlighting = myResolveHighlighterRegistrar.GetResolveHighlighting(reference, error);
        if (highlighting != null)
          consumer.AddHighlighting(highlighting);
      }
      else
        consumer.AddHighlighting(new NotResolvedError(reference));
    }
  }
}