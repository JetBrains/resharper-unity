using System;
using System.Collections.Generic;
using JetBrains.Application.Settings;
using JetBrains.ReSharper.Feature.Services.Daemon;
using JetBrains.ReSharper.Plugins.Unity.Psi.ShaderLab;
using JetBrains.ReSharper.Plugins.Unity.Psi.ShaderLab.Tree;
using JetBrains.ReSharper.Psi;
using JetBrains.ReSharper.Psi.Files;
using JetBrains.Util;

namespace JetBrains.ReSharper.Plugins.Unity.Daemon.Stages
{
    public abstract class ShaderLabStageBase : IDaemonStage
    {
        public IEnumerable<IDaemonStageProcess> CreateProcess(IDaemonProcess process, IContextBoundSettingsStore settings, DaemonProcessKind processKind)
        {
            if (!IsSupported(process.SourceFile))
                return EmptyList<IDaemonStageProcess>.Instance;

            process.SourceFile.GetPsiServices().Files.AssertAllDocumentAreCommitted();
            return process.SourceFile.GetPsiFiles<ShaderLabLanguage>()
                .SelectNotNull(file => CreateProcess(process, settings, processKind, (IShaderLabFile) file));
        }

        protected abstract IDaemonStageProcess CreateProcess(IDaemonProcess process, IContextBoundSettingsStore settings, DaemonProcessKind processKind, IShaderLabFile file);

        public ErrorStripeRequest NeedsErrorStripe(IPsiSourceFile sourceFile, IContextBoundSettingsStore settingsStore)
        {
            throw new InvalidOperationException("This API is obsolete");
        }

        protected virtual bool IsSupported(IPsiSourceFile sourceFile)
        {
            if (sourceFile == null || !sourceFile.IsValid())
                return false;

            var properties = sourceFile.Properties;
            if (properties.IsNonUserFile || !properties.ProvidesCodeModel)
                return false;

            return sourceFile.IsLanguageSupported<ShaderLabLanguage>();
        }
    }
}