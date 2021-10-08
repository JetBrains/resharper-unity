using System.Collections.Generic;
using System.Linq;
using JetBrains.Application.Settings;
using JetBrains.ReSharper.Feature.Services.Daemon;
using JetBrains.ReSharper.Plugins.Unity.JsonNew.Psi;
using JetBrains.ReSharper.Plugins.Unity.JsonNew.Psi.Tree;
using JetBrains.ReSharper.Psi;
using JetBrains.ReSharper.Psi.Files;
using JetBrains.Util;

namespace JetBrains.ReSharper.Plugins.Unity.JsonNew.Feature.Services.Daemon
{
    public abstract class JsonNewDaemonStageBase : IDaemonStage
    {
        public IEnumerable<IDaemonStageProcess> CreateProcess(IDaemonProcess process,
                                                              IContextBoundSettingsStore settings,
                                                              DaemonProcessKind processKind)
        {
            if (!IsSupported(process.SourceFile))
                return EmptyList<IDaemonStageProcess>.Instance;

            if (!ShouldRunOnGenerated && process.SourceFile.Properties.IsGeneratedFile)
                return EmptyList<IDaemonStageProcess>.Instance;

            process.SourceFile.GetPsiServices().Files.AssertAllDocumentAreCommitted();

            var files = process.SourceFile.GetPsiFiles<JsonNewLanguage>();
            return files.SelectNotNull(file => CreateProcess(process, settings, processKind, (IJsonNewFile)file));
        }

        protected virtual bool ShouldRunOnGenerated => false;

        protected abstract IDaemonStageProcess CreateProcess(IDaemonProcess process,
                                                             IContextBoundSettingsStore settings,
                                                             DaemonProcessKind processKind, IJsonNewFile file);

        protected virtual bool IsSupported(IPsiSourceFile sourceFile)
        {
            if (sourceFile == null || !sourceFile.IsValid())
                return false;

            return sourceFile.GetLanguages().Any(x => x.Is<JsonNewLanguage>());
        }
    }
}