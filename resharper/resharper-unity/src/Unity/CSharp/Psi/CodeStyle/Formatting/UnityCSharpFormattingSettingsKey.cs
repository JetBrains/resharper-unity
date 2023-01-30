using JetBrains.Application.Settings;
using JetBrains.ReSharper.Plugins.Unity.Resources;
using JetBrains.ReSharper.Psi.CodeStyle;

namespace JetBrains.ReSharper.Plugins.Unity.CSharp.Psi.CodeStyle.Formatting
{
    [SettingsKey(typeof(CodeFormattingSettingsKey), DescriptionResourceType:typeof(Strings), DescriptionResourceName: nameof(Strings.UnityCSharpFormattingSettingsKey_s_Settings_for_Unity_specific_C__formatting))]
    public class UnityCSharpFormattingSettingsKey
    {
        [SettingsEntry(DefaultValue: false, DescriptionResourceType: typeof(Strings), DescriptionResourceName: nameof(Strings.UnityCSharpFormattingSettingsKey_s_Force_a_line_break_after_any__Header__annotations))]
        public bool ENFORCE_CUSTOM_HEADER_FORMATTING;
        
        [SettingsEntry(DefaultValue: 0, DescriptionResourceType: typeof(Strings), DescriptionResourceName: nameof(Strings.UnityCSharpFormattingSettingsKey_s_Blank_lines_after_the__Header__annotations))]
        public int BLANK_LINES_AFTER_HEADER;
    }
}