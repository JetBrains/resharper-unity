using JetBrains.Application.Settings;
using JetBrains.ReSharper.Daemon.Stages.Dispatcher;
using JetBrains.ReSharper.Feature.Services.Daemon;
using JetBrains.ReSharper.Plugins.Yaml.Psi.Tree;
using JetBrains.ReSharper.Psi.Tree;

namespace JetBrains.ReSharper.Plugins.Yaml.Daemon.Stages
{
  // TODO: How to handle element problem analysers and huge files?
  // Unregister this for now. We don't have any YAML or Unity YAML element problem analysers, and running it would cause
  // all chameleons to be opened, which is very bad for Unity YAML files

//  [DaemonStage(StagesBefore = new[] {typeof(CollectUsagesStage), typeof(GlobalFileStructureCollectorStage)},
//    StagesAfter = new[] {typeof(LanguageSpecificDaemonStage)})]
  public class YamlErrorStage : YamlDaemonStageBase
  {
    private readonly ElementProblemAnalyzerRegistrar myElementProblemAnalyzerRegistrar;

    public YamlErrorStage(ElementProblemAnalyzerRegistrar elementProblemAnalyzerRegistrar)
    {
      myElementProblemAnalyzerRegistrar = elementProblemAnalyzerRegistrar;
    }

    protected override IDaemonStageProcess CreateProcess(IDaemonProcess process, IContextBoundSettingsStore settings,
      DaemonProcessKind processKind, IYamlFile file)
    {
      return new YamlErrorStageProcess(process, processKind, myElementProblemAnalyzerRegistrar, settings, file,
        ShouldAllowOpeningChameleons(file, processKind));
    }

    private class YamlErrorStageProcess : YamlDaemonStageProcessBase
    {
      private readonly IElementAnalyzerDispatcher myElementAnalyzerDispatcher;

      public YamlErrorStageProcess(IDaemonProcess process, DaemonProcessKind processKind,
                                   ElementProblemAnalyzerRegistrar elementProblemAnalyzerRegistrar,
                                   IContextBoundSettingsStore settings, IYamlFile file, bool allowOpeningChameleons)
        : base(process, file, allowOpeningChameleons)
      {
        var elementProblemAnalyzerData = new ElementProblemAnalyzerData(file, settings, ElementProblemAnalyzerRunKind.FullDaemon);
        elementProblemAnalyzerData.SetDaemonProcess(process, processKind);
        myElementAnalyzerDispatcher = elementProblemAnalyzerRegistrar.CreateDispatcher(elementProblemAnalyzerData);
      }

      public override void VisitNode(ITreeNode node, IHighlightingConsumer consumer)
      {
        myElementAnalyzerDispatcher.Run(node, consumer);
      }
    }
  }
}
