using System;
using JetBrains.ReSharper.Feature.Services.Daemon;
using JetBrains.ReSharper.Plugins.Yaml.Daemon.Errors;
using JetBrains.ReSharper.Plugins.Yaml.Psi;
using JetBrains.ReSharper.Psi.Resolve;
using JetBrains.ReSharper.Psi.Tree;

namespace JetBrains.ReSharper.Plugins.Yaml.Daemon.Stages
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

      if (myResolveHighlighterRegistrar.ContainsHandler(YamlLanguage.Instance, error))
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