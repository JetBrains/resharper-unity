using JetBrains.Application.Settings;

namespace JetBrains.ReSharper.Plugins.Unity.CSharp.Daemon.Stages.ContextSystem
{
    /// <summary>
    /// Checks if current context is enabled.
    /// Each context must have exactly 1 setting provider.
    /// </summary>
    public interface IUnityProblemAnalyzerContextSettingProvider : IUnityProblemAnalyzerContextClassification
    {
        UnityProblemAnalyzerContextSetting CheckSettings(IContextBoundSettingsStore settingsStore);
    }
}