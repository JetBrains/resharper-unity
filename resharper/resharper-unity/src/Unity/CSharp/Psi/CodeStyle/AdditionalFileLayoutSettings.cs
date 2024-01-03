using JetBrains.Application.Settings;
using JetBrains.ReSharper.Plugins.Unity.Core.Application.Settings;
using JetBrains.ReSharper.Plugins.Unity.Resources;

namespace JetBrains.ReSharper.Plugins.Unity.CSharp.Psi.CodeStyle
{
    [SettingsKey(typeof(UnitySettings), DescriptionResourceType:typeof(Strings), DescriptionResourceName:nameof(Strings.AdditionalFileLayoutSettings_s_Additional_Unity_C__file_layout_pattern))]
    public class AdditionalFileLayoutSettings
    {
        [SettingsEntry("", DescriptionResourceType:typeof(Strings), DescriptionResourceName:nameof(Strings.AdditionalFileLayoutSettings_s_File_Layout_Pattern))]
        public string Pattern;
    }
}