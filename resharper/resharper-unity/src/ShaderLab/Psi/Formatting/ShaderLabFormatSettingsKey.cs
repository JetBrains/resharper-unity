using JetBrains.Application.Settings;
using JetBrains.ReSharper.Psi.CodeStyle;
using JetBrains.ReSharper.Psi.EditorConfig;
using JetBrains.ReSharper.Psi.Format;

namespace JetBrains.ReSharper.Plugins.Unity.ShaderLab.Psi.Formatting
{
  [SettingsKey(typeof(CodeFormattingSettingsKey), "Code formatting in ShaderLab")]
  [EditorConfigKey("shaderlab")]
  public class ShaderLabFormatSettingsKey : FormatSettingsKeyBase
  {
    [EditorConfigEntryAlias("brace_style", EditorConfigAliasType.ReSharperGeneralized)]
    [SettingsEntry(BraceFormatStyle.NEXT_LINE, "BraceStyle")]
    public BraceFormatStyle BraceStyle;
  }
}