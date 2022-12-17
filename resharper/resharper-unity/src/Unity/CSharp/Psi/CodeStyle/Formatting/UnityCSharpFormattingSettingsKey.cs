using JetBrains.Application.Settings;
using JetBrains.ReSharper.Psi.CodeStyle;

namespace JetBrains.ReSharper.Plugins.Unity.CSharp.Psi.CodeStyle.Formatting
{
    [SettingsKey(typeof(CodeFormattingSettingsKey), "Settings for Unity-specific C# formatting")]
    public class UnityCSharpFormattingSettingsKey
    {
        [SettingsEntry(DefaultValue: false, "Force a line break after any [Header] annotations")]
        public bool ENFORCE_CUSTOM_HEADER_FORMATTING;
        
        [SettingsEntry(DefaultValue: 0, "Blank lines after the [Header] annotations")]
        public int BLANK_LINES_AFTER_HEADER;
    }
}