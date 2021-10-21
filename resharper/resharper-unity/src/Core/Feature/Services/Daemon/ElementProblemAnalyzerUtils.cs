using System;
using System.Linq.Expressions;
using JetBrains.Application.InlayHints;
using JetBrains.Application.Settings;
using JetBrains.ReSharper.Feature.Services.Daemon;
using JetBrains.ReSharper.Plugins.Unity.Core.Application.Settings;

#nullable enable

namespace JetBrains.ReSharper.Plugins.Unity.Core.Feature.Services.Daemon
{
    public static class ElementProblemAnalyzerUtils
    {
        public static InlayHintsMode GetInlayHintsMode(ElementProblemAnalyzerData data,
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