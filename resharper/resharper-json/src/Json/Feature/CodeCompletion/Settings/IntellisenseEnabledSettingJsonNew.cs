using JetBrains.Application.Settings;
using JetBrains.ReSharper.Feature.Services.CodeCompletion.Settings;

namespace JetBrains.ReSharper.Plugins.Unity.JsonNew.Feature.CodeCompletion.Settings
{
    [SettingsKey(typeof(IntellisenseEnabledSettingsKey), "Override VS IntelliSense for Json")]
    public class IntellisenseEnabledSettingJsonNew
    {
        [SettingsEntry(false, "Json (.json files)")]
        public bool IntellisenseEnabled;
    }
}