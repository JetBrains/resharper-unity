using JetBrains.Application;
using JetBrains.Application.Parts;
using JetBrains.Application.Settings;
using JetBrains.ProjectModel;
using JetBrains.ReSharper.Feature.Services.Daemon;
using JetBrains.ReSharper.Plugins.Unity.Shaders.ShaderLab.Daemon.ContextHighlighters;
using JetBrains.ReSharper.Psi;

#nullable enable

namespace JetBrains.ReSharper.Plugins.Unity.Rider.Integration.Shaders.ShaderLab.Daemon.ContextHighlighters
{
    [ShellComponent(Instantiation.DemandAnyThreadSafe)]
    public class RiderShaderLabUsageContextHighlighterAvailability : ShaderLabUsageContextHighlighterAvailability
    {
        public override bool IsAvailable(IPsiSourceFile psiSourceFile)
        {
            return psiSourceFile.GetSolution().GetSettingsStore()
                .GetValue(HighlightingSettingsAccessor.HighlightUsages);
        }
    }
}