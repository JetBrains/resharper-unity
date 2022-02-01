using System.Collections.Generic;
using JetBrains.Application.Settings;
using JetBrains.ReSharper.Feature.Services.Daemon;
using JetBrains.ReSharper.Plugins.Unity.Shaders.Cg.Psi;
using JetBrains.ReSharper.Plugins.Unity.Shaders.Cg.Psi.Tree;
using JetBrains.ReSharper.Psi;
using JetBrains.ReSharper.Psi.Files;
using JetBrains.Util;

namespace JetBrains.ReSharper.Plugins.Unity.Shaders.Cg.Daemon.Stages
{
    public abstract class CgDaemonStageBase : IDaemonStage
    {
        public IEnumerable<IDaemonStageProcess> CreateProcess(IDaemonProcess process, IContextBoundSettingsStore settings, DaemonProcessKind processKind)
        {
            if (!IsSupported(process.SourceFile))
                return EmptyList<IDaemonStageProcess>.InstanceList;

            process.SourceFile.GetPsiServices().Files.AssertAllDocumentAreCommitted();
            return process.SourceFile.GetPsiFiles<CgLanguage>()
                .SelectNotNull(file => CreateProcess(process, settings, processKind, (ICgFile) file));
        }

        private bool IsSupported(IPsiSourceFile sourceFile)
        {
            // just as C#
            if (sourceFile == null || !sourceFile.IsValid())
                return false;

            var properties = sourceFile.Properties;
            if (properties.IsNonUserFile || !properties.ProvidesCodeModel)
                return false;

            return sourceFile.IsLanguageSupported<CgLanguage>();
        }

        protected abstract IDaemonStageProcess CreateProcess(IDaemonProcess process,
            IContextBoundSettingsStore settings, DaemonProcessKind processKind, ICgFile file);
    }
}