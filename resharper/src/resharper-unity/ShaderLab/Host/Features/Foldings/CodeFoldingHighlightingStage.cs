#if RIDER

using System;
using JetBrains.Application.Settings;
using JetBrains.ReSharper.Daemon.Stages;
using JetBrains.ReSharper.Daemon.UsageChecking;
using JetBrains.ReSharper.Feature.Services.Daemon;
using JetBrains.ReSharper.Plugins.Unity.ShaderLab.Daemon.Stages;
using JetBrains.ReSharper.Plugins.Unity.ShaderLab.Psi.Tree;
using JetBrains.ReSharper.Psi;
using JetBrains.ReSharper.Psi.Tree;

namespace JetBrains.ReSharper.Plugins.Unity.ShaderLab.Host.Features.Foldings
{
    // TODO: Get rid of this once we have an SDK and can simply implement ICodeFoldingProcessor
    [DaemonStage(StagesBefore = new[] { typeof(GlobalFileStructureCollectorStage) },
        StagesAfter = new [] { typeof(CollectUsagesStage)} )]
    public class CodeFoldingHighlightingStage : ShaderLabStageBase
    {
        protected override IDaemonStageProcess CreateProcess(IDaemonProcess process, IContextBoundSettingsStore settings,
            DaemonProcessKind processKind, IShaderLabFile file)
        {
            return new CodeFoldingProcess(process, settings, file);
        }

        private class CodeFoldingProcess : IDaemonStageProcess
        {
            private readonly IContextBoundSettingsStore mySettings;
            private readonly IFile myFile;

            public CodeFoldingProcess(IDaemonProcess daemonProcess, IContextBoundSettingsStore settings, IFile file)
            {
                mySettings = settings;
                myFile = file;
                DaemonProcess = daemonProcess;
            }

            public void Execute(Action<DaemonStageResult> committer)
            {
                var services = DaemonProcess.SourceFile.GetPsiServices();
                services.Files.AssertAllDocumentAreCommitted();

                var factory = new ShaderLabCodeFoldingProcessFactory();
                var consumer = new DefaultHighlightingConsumer(this, mySettings);
                myFile.ProcessDescendants(factory.CreateProcessor(), consumer);

                var foldings = CodeFoldingUtil.AppendRangeWithOverlappingResolve(consumer.Highlightings);
                committer(new DaemonStageResult(foldings));
            }

            public IDaemonProcess DaemonProcess { get; }
        }
    }
}

#endif