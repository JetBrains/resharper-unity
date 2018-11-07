using System;
using JetBrains.ReSharper.Feature.Services.Daemon;
using JetBrains.ReSharper.Plugins.Yaml.Psi.Tree;
using JetBrains.ReSharper.Psi;
using JetBrains.ReSharper.Psi.Tree;

namespace JetBrains.ReSharper.Plugins.Yaml.Daemon.Stages
{
  public abstract class YamlDaemonStageProcessBase : TreeNodeVisitor<IHighlightingConsumer>,
    IRecursiveElementProcessor<IHighlightingConsumer>, IDaemonStageProcess
  {
    private readonly IYamlFile myFile;

    protected YamlDaemonStageProcessBase(IDaemonProcess process, IYamlFile file)
    {
      DaemonProcess = process;
      myFile = file;
    }

    public bool InteriorShouldBeProcessed(ITreeNode element, IHighlightingConsumer consumer)
    {
      return !IsProcessingFinished(consumer);
    }

    public bool IsProcessingFinished(IHighlightingConsumer consumer)
    {
      if (DaemonProcess.InterruptFlag)
        throw new OperationCanceledException();
      return false;
    }

    public virtual void ProcessBeforeInterior(ITreeNode element, IHighlightingConsumer consumer)
    {
    }

    public virtual void ProcessAfterInterior(ITreeNode element, IHighlightingConsumer consumer)
    {
      if (element is IYamlTreeNode yamlTreeNode && !yamlTreeNode.IsWhitespaceToken())
        yamlTreeNode.Accept(this, consumer);
      else
        VisitNode(element, consumer);
    }

    public void Execute(Action<DaemonStageResult> committer)
    {
      HighlightInFile((file, consumer) => file.ProcessDescendants(this, consumer), committer);
    }

    public IDaemonProcess DaemonProcess { get; }

    protected void HighlightInFile(Action<IYamlFile, IHighlightingConsumer> fileHighlighter,
      Action<DaemonStageResult> committer)
    {
      var consumer = new FilteringHighlightingConsumer(DaemonProcess.SourceFile, myFile, DaemonProcess.ContextBoundSettingsStore);
      fileHighlighter(myFile, consumer);
      committer(new DaemonStageResult(consumer.Highlightings));
    }
  }
}