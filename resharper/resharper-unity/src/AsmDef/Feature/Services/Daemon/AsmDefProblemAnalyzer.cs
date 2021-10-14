using System;
using System.Linq.Expressions;
using JetBrains.Application.InlayHints;
using JetBrains.Application.Settings;
using JetBrains.ReSharper.Feature.Services.Daemon;
using JetBrains.ReSharper.Plugins.Unity.Core.Application.Settings;
using JetBrains.ReSharper.Plugins.Unity.JsonNew.Psi;
using JetBrains.ReSharper.Plugins.Unity.ProjectModel;
using JetBrains.ReSharper.Plugins.Unity.Utils;
using JetBrains.ReSharper.Psi;
using JetBrains.ReSharper.Psi.Tree;

#nullable enable

namespace JetBrains.ReSharper.Plugins.Unity.AsmDef.Feature.Services.Daemon
{
    public abstract class AsmDefProblemAnalyzer<T> : ElementProblemAnalyzer<T>
        where T : ITreeNode
    {
        protected override void Run(T element, ElementProblemAnalyzerData data, IHighlightingConsumer consumer)
        {
            // Run for visible documents and SWEA. Also run for "other", which is used by scoped quick fixes
            if (data.GetDaemonProcessKind() == DaemonProcessKind.GLOBAL_WARNINGS)
                return;

            if (data.SourceFile == null || !element.Language.Is<JsonNewLanguage>() || !data.SourceFile.IsAsmDef())
                return;

            if (!element.GetSolution().HasUnityReference())
                return;

            Analyze(element, data, consumer);
        }

        protected abstract void Analyze(T element, ElementProblemAnalyzerData data, IHighlightingConsumer consumer);

        protected static InlayHintsMode GetMode(ElementProblemAnalyzerData data,
                                                Expression<Func<UnityInlayHintSettings, InlayHintsMode>> option)
        {
            if (data.RunKind != ElementProblemAnalyzerRunKind.FullDaemon)
                return InlayHintsMode.Never;

            if (data.GetDaemonProcessKind() != DaemonProcessKind.VISIBLE_DOCUMENT)
                return InlayHintsMode.Never;

            // This checks the "Enable Inlay Hints in .NET languages" option. It's a stretch to call .asmdef a .net
            // language, but it's the best overall switch we've got
            if (!data.SettingsStore.GetValue((GeneralInlayHintsOptions s) => s.EnableInlayHints))
                return InlayHintsMode.Never;

            return data.SettingsStore.GetValue(option).EnsureDefault(data.SettingsStore);
        }
    }
}
