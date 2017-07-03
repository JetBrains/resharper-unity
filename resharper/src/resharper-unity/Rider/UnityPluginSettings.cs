#if RIDER
using JetBrains.Application.Settings;
using JetBrains.ReSharper.Resources.Settings;

namespace JetBrains.ReSharper.Plugins.Unity.Rider
{
    [SettingsKey(typeof(CodeEditingSettings), "Unity plugin settings")]
    public class UnityPluginSettings
    {
        [SettingsEntry(true, "If this option is enabled, Rider plugin will be automatically installed for Unity projects.")]
        public bool InstallUnity3DRiderPlugin;
    }
}
#endif