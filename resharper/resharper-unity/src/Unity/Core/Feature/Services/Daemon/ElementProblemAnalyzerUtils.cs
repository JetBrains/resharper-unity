#nullable enable

using System;
using System.Linq.Expressions;

using JetBrains.Application.Settings;
using JetBrains.ReSharper.Feature.Services.Daemon;
using JetBrains.ReSharper.Plugins.Unity.Core.Application.Settings;
using JetBrains.TextControl.DocumentMarkup.Adornments;
using JetBrains.TextControl.DocumentMarkup.Adornments.IntraTextAdornments;

namespace JetBrains.ReSharper.Plugins.Unity.Core.Feature.Services.Daemon
{
    public static class ElementProblemAnalyzerUtils
    {
        public static PushToHintMode GetInlayHintsMode(ElementProblemAnalyzerData data,
                                                       Expression<Func<UnityInlayHintSettings, PushToHintMode>> option)
        {
            if (data.RunKind != ElementProblemAnalyzerRunKind.FullDaemon)
                return PushToHintMode.Never;

            if (data.GetDaemonProcessKind() != DaemonProcessKind.VISIBLE_DOCUMENT)
                return PushToHintMode.Never;

            // This checks the "Enable Inlay Hints in .NET languages" option. It's a stretch to call .asmdef a .net
            // language, but it's the best overall switch we've got
            if (!data.SettingsStore.GetValue((GeneralInlayHintsOptions s) => s.EnableInlayHints))
                return PushToHintMode.Never;

            return data.SettingsStore.GetValue(option).EnsureInlayHintsDefault(data.SettingsStore);
        }
    }
}