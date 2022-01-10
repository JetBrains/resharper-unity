﻿using JetBrains.Application;
using JetBrains.Application.Settings;
using JetBrains.ProjectModel;
using JetBrains.ReSharper.Feature.Services.Daemon;
using JetBrains.ReSharper.Plugins.Unity.ShaderLab.Daemon.ContextHighlighters;
using JetBrains.ReSharper.Psi;
using JetBrains.ReSharper.Resources.Shell;

namespace JetBrains.ReSharper.Plugins.Unity.Rider.ShaderLab.Daemon.ContextHighlighters
{
    [ShellComponent]
    public class RiderShaderLabUsageContextHighlighterAvailability : ShaderLabUsageContextHighlighterAvailability
    {
        public override bool IsAvailable(IPsiSourceFile psiSourceFile)
        {
            return Shell.Instance.IsTestShell ||
                   psiSourceFile.GetSolution().GetSettingsStore().GetValue(HighlightingSettingsAccessor.HighlightUsages);
        }
    }
}