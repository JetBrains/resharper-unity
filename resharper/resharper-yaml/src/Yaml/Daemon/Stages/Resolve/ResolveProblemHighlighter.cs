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
      var resolveErrorType = reference.CheckResolveResult();
      if (resolveErrorType == null)
        throw new InvalidOperationException("ResolveErrorType is null for reference " + reference.GetType().FullName);

      if (resolveErrorType.IsAcceptable)
        return;

      // ReSharper disable once AssignNullToNotNullAttribute
      if (myResolveHighlighterRegistrar.ContainsHandler(YamlLanguage.Instance, resolveErrorType))
      {
        var highlighting = myResolveHighlighterRegistrar.GetResolveHighlighting(reference, resolveErrorType);
        if (highlighting != null)
          consumer.AddHighlighting(highlighting);
      }
      else
        consumer.AddHighlighting(new NotResolvedError(reference));
    }
  }
}