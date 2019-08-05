using JetBrains.Application.Settings;
using JetBrains.ReSharper.Daemon.Stages;
using JetBrains.ReSharper.Daemon.UsageChecking;
using JetBrains.ReSharper.Feature.Services.Daemon;
using JetBrains.ReSharper.Plugins.Yaml.Daemon.Errors;
using JetBrains.ReSharper.Plugins.Yaml.Psi;
using JetBrains.ReSharper.Plugins.Yaml.Psi.Tree;
using JetBrains.ReSharper.Psi;
using JetBrains.ReSharper.Psi.ExtensionsAPI.Tree;
using JetBrains.ReSharper.Psi.Resolve;
using JetBrains.ReSharper.Psi.Tree;

namespace JetBrains.ReSharper.Plugins.Yaml.Daemon.Stages
{
  [DaemonStage(StagesBefore = new[] {typeof(GlobalFileStructureCollectorStage)},
    StagesAfter = new[] {typeof(CollectUsagesStage)})]
  public class IdentifierHighlightingStage : YamlDaemonStageBase
  {
    private readonly ResolveHighlighterRegistrar myRegistrar;

    public IdentifierHighlightingStage(ResolveHighlighterRegistrar registrar)
    {
      myRegistrar = registrar;
    }

    protected override IDaemonStageProcess CreateProcess(IDaemonProcess process, IContextBoundSettingsStore settings,
                                                         DaemonProcessKind processKind, IYamlFile file)
    {
      return new IdentifierHighlightingProcess(process, file, myRegistrar,
        ShouldAllowOpeningChameleons(file, processKind));
    }

    protected override bool IsSupported(IPsiSourceFile sourceFile)
    {
      // Don't check PSI properties - a syntax error is a syntax error
      if (sourceFile == null || !sourceFile.IsValid())
        return false;
      
      #if !JET_MODE_ASSERT
        return false;
      #else
        return sourceFile.IsLanguageSupported<YamlLanguage>();
      #endif
    }

    private class IdentifierHighlightingProcess : YamlDaemonStageProcessBase
    {
      private readonly ResolveProblemHighlighter myResolveProblemHighlighter;
      private readonly IReferenceProvider myReferenceProvider;

      public IdentifierHighlightingProcess(IDaemonProcess process, IYamlFile file,
                                           ResolveHighlighterRegistrar resolveHighlighterRegistrar,
                                           bool allowOpeningChameleons)
        : base(process, file, allowOpeningChameleons)
      {
        myResolveProblemHighlighter = new ResolveProblemHighlighter(resolveHighlighterRegistrar);
        myReferenceProvider = ((IFileImpl) file).ReferenceProvider;
      }

      public override void VisitNode(ITreeNode node, IHighlightingConsumer consumer)
      {
        var references = node.GetReferences(myReferenceProvider);
        myResolveProblemHighlighter.CheckForResolveProblems(node, consumer, references);

        if (node is IErrorElement errorElement)
        {
          var range = errorElement.GetDocumentRange();
          if (!range.IsValid())
            range = node.Parent.GetDocumentRange();
          if (range.TextRange.IsEmpty)
          {
            if (range.TextRange.EndOffset < range.Document.GetTextLength())
              range = range.ExtendRight(1);
            else if (range.TextRange.StartOffset > 0)
              range = range.ExtendLeft(1);
          }

          consumer.AddHighlighting(new YamlSyntaxError(errorElement.ErrorDescription, range));
        }
      }
    }
  }
}
