using System.Collections.Generic;
using JetBrains.Application.Settings;
using JetBrains.ReSharper.Feature.Services.Daemon;
using JetBrains.ReSharper.Plugins.Unity.Shaders.ShaderLab.Psi;
using JetBrains.ReSharper.Plugins.Unity.Shaders.ShaderLab.Psi.Tree;
using JetBrains.ReSharper.Psi;
using JetBrains.ReSharper.Psi.Files;
using JetBrains.Util;

namespace JetBrains.ReSharper.Plugins.Unity.Shaders.ShaderLab.Daemon.Stages
{
    public abstract class ShaderLabStageBase : IModernDaemonStage
    {
        public IEnumerable<IDaemonStageProcess> CreateProcess(
            IDaemonProcess process, IContextBoundSettingsStore settings, DaemonProcessKind processKind)
        {
            process.SourceFile.GetPsiServices().Files.AssertAllDocumentAreCommitted();

            return process.SourceFile.GetPsiFiles<ShaderLabLanguage>()
                .SelectNotNull(file => CreateProcess(process, settings, processKind, (IShaderLabFile) file));
        }

        protected abstract IDaemonStageProcess CreateProcess(
            IDaemonProcess process, IContextBoundSettingsStore settings, DaemonProcessKind processKind, IShaderLabFile file);

        bool IModernDaemonStage.IsApplicable(IPsiSourceFile sourceFile, DaemonProcessKind processKind)
        {
            return IsSupported(sourceFile);
        }

        protected virtual bool IsSupported(IPsiSourceFile sourceFile)
        {
            var properties = sourceFile.Properties;
            if (properties.IsNonUserFile || !properties.ProvidesCodeModel)
                return false;

            return sourceFile.IsLanguageSupported<ShaderLabLanguage>();
        }
    }
}