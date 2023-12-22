using JetBrains.Application.Settings;
using JetBrains.ReSharper.Plugins.Unity.Shaders.Resources;
using JetBrains.ReSharper.Psi.CodeStyle;
using JetBrains.ReSharper.Psi.EditorConfig;
using JetBrains.ReSharper.Psi.Format;

namespace JetBrains.ReSharper.Plugins.Unity.Shaders.ShaderLab.Psi.Formatting
{
  [SettingsKey(typeof(CodeFormattingSettingsKey), DescriptionResourceType: typeof(Strings), DescriptionResourceName:nameof(Strings.ShaderLabFormatSettingsKey_s_Code_formatting_in_ShaderLab))]
  [EditorConfigKey("shaderlab")]
  public class ShaderLabFormatSettingsKey : FormatSettingsKeyBase
  {
    [EditorConfigEntryAlias("brace_style", EditorConfigAliasType.ReSharperGeneralized)]
    [SettingsEntry(BraceFormatStyle.NEXT_LINE, DescriptionResourceType:typeof(Strings), DescriptionResourceName:nameof(Strings.ShaderLabFormatSettingsKey_s_BraceStyle))]
    public BraceFormatStyle BraceStyle;
  }
}