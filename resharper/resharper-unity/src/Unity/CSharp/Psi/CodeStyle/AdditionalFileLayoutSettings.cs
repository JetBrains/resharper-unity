using JetBrains.Application.Settings;
using JetBrains.ReSharper.Plugins.Unity.Core.Application.Settings;

namespace JetBrains.ReSharper.Plugins.Unity.CSharp.Psi.CodeStyle
{
    [SettingsKey(typeof(UnitySettings), "Additional Unity C# file layout pattern")]
    public class AdditionalFileLayoutSettings
    {
        [SettingsEntry("", "File Layout Pattern")]
        public string Pattern;
    }
}