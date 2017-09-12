#if RIDER

using System.Collections.Generic;
using JetBrains.Application.Settings;
using JetBrains.ReSharper.Daemon.CSharp.Stages;
using JetBrains.ReSharper.Feature.Services.Daemon;
using JetBrains.ReSharper.Plugins.Unity.ShaderLab.Host.Features.SyntaxHighlighting;
using JetBrains.ReSharper.Plugins.Unity.ShaderLab.Psi;
using JetBrains.ReSharper.Psi;
using JetBrains.ReSharper.Psi.Files;
using JetBrains.ReSharper.Psi.Tree;

// TODO: Delete this file once we have a Rider SDK and can use the real RiderSyntaxHighlightingManager

// ReSharper disable once CheckNamespace
namespace JetBrains.ReSharper.Host.Features.SyntaxHighlighting
{
    internal abstract class RiderSyntaxHighlightingManager
    {
        public abstract RiderSyntaxHighlightingProcessBase CreateProcess(IDaemonProcess process,
            IContextBoundSettingsStore settings, IFile getPrimaryPsiFile);
    }

    [DaemonStage(StagesBefore = new[] {typeof(SmartResolverStage)})]
    internal class TempRiderSyntaxHighlightingStage : IDaemonStage
    {
        public IEnumerable<IDaemonStageProcess> CreateProcess(IDaemonProcess process, IContextBoundSettingsStore settings,
            DaemonProcessKind processKind)
        {
            var services = process.SourceFile.GetPsiServices();
            services.Files.AssertAllDocumentAreCommitted();
            var languageManager = services.LanguageManager;
            var files = process.SourceFile.GetPsiFiles<ShaderLabLanguage>();
            var processes = new List<IDaemonStageProcess>();
            foreach (var file in files)
            {
                var manager = languageManager.TryGetService<ShaderLabSyntaxHighlightingManager>(file.Language);
                if (manager != null)
                    processes.Add(manager.CreateProcess(process, settings, file));
            }
            return processes;
        }

        public ErrorStripeRequest NeedsErrorStripe(IPsiSourceFile sourceFile, IContextBoundSettingsStore settingsStore)
        {
            return ErrorStripeRequest.NONE;
        }
    }
}

#endif