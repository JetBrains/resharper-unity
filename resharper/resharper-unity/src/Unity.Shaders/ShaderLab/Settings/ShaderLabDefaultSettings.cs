using JetBrains.Application;
using JetBrains.Application.Parts;
using JetBrains.Application.Settings;
using JetBrains.Application.Settings.Implementation;
using JetBrains.ReSharper.Plugins.Unity.Shaders.ShaderLab.Psi;
using JetBrains.ReSharper.Psi.PerformanceThreshold.Settings;
using JetBrains.Util;

#nullable enable

namespace JetBrains.ReSharper.Plugins.Unity.Shaders.ShaderLab.Settings
{
    [ShellComponent(Instantiation.DemandAnyThreadSafe)]
    public class UnityDefaultPerformanceThresholdSettingsProvider : HaveDefaultSettings<PerformanceThresholdSettings>
    {
        public UnityDefaultPerformanceThresholdSettingsProvider(ILogger logger, ISettingsSchema settingsSchema)
            : base(settingsSchema, logger)
        {
        }

        public override void InitDefaultSettings(ISettingsStorageMountPoint mountPoint)
        {
            SetIndexedValue(mountPoint, PerformanceThresholdSettingsAccessor.AnalysisFileSizeThreshold, ShaderLabLanguage.Name, 300000);
            SetIndexedValue(mountPoint, PerformanceThresholdSettingsAccessor.BuildPsiFileSizeThreshold, ShaderLabLanguage.Name, 4096000);
        }

        public override string Name => "UnityDefaultPerformanceThresholdSettingsProvider";
    }
}
