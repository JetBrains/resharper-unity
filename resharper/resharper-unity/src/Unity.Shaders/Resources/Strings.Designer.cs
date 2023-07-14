﻿namespace JetBrains.ReSharper.Plugins.Unity.Shaders.Resources
{
  using System;
  using JetBrains.Application.I18n;
  using JetBrains.DataFlow;
  using JetBrains.Diagnostics;
  using JetBrains.Lifetimes;
  using JetBrains.Util;
  using JetBrains.Util.Logging;
  
  [global::System.Diagnostics.DebuggerNonUserCodeAttribute()]
  [global::System.Runtime.CompilerServices.CompilerGeneratedAttribute()]
  public static class Strings
  {
    private static readonly ILogger ourLog = Logger.GetLogger("JetBrains.ReSharper.Plugins.Unity.Shaders.Resources.Strings");

    static Strings()
    {
      CultureContextComponent.Instance.WhenNotNull(Lifetime.Eternal, (lifetime, instance) =>
      {
        lifetime.Bracket(() =>
          {
            ourResourceManager = new Lazy<JetResourceManager>(
              () =>
              {
                return instance
                  .CreateResourceManager("JetBrains.ReSharper.Plugins.Unity.Shaders.Resources.Strings", typeof(Strings).Assembly);
              });
          },
          () =>
          {
            ourResourceManager = null;
          });
      });
    }
    
    private static Lazy<JetResourceManager> ourResourceManager = null;
    
    [global::System.ComponentModel.EditorBrowsableAttribute(global::System.ComponentModel.EditorBrowsableState.Advanced)]
    public static JetResourceManager ResourceManager
    {
      get
      {
        var resourceManager = ourResourceManager;
        if (resourceManager == null)
        {
          return ErrorJetResourceManager.Instance;
        }
        return resourceManager.Value;
      }
    }

    public static string AmbiguousReferencematchMessage => ResourceManager.GetString("AmbiguousReferencematchMessage");
    public static string CannotResolveSymbolMessage => ResourceManager.GetString("CannotResolveSymbolMessage");
    public static string CgBuiltInTypeTokenNodeType_TokenRepresentation_built_in_type => ResourceManager.GetString("CgBuiltInTypeTokenNodeType_TokenRepresentation_built_in_type");
    public static string CgLanguageSpecificDaemonBehaviour_InitialErrorStripe_File_s_primary_language_in_not_Cg => ResourceManager.GetString("CgLanguageSpecificDaemonBehaviour_InitialErrorStripe_File_s_primary_language_in_not_Cg");
    public static string CgSyntaxHighlightingProcess_VisitSemanticNode_Semantic__packoffset_or_register_expected => ResourceManager.GetString("CgSyntaxHighlightingProcess_VisitSemanticNode_Semantic__packoffset_or_register_expected");
    public static string CommentsBlockComment_RiderPresentableName => ResourceManager.GetString("CommentsBlockComment_RiderPresentableName");
    public static string CommentsLineComment_RiderPresentableName => ResourceManager.GetString("CommentsLineComment_RiderPresentableName");
    public static string ConflictingPropertyIsDefinedBelowMessage => ResourceManager.GetString("ConflictingPropertyIsDefinedBelowMessage");
    public static string IgnoredCharacterConsiderInsertingNewLineForClarityMessage => ResourceManager.GetString("IgnoredCharacterConsiderInsertingNewLineForClarityMessage");
    public static string InUnityShaderLabFile_PresentableShortName_ShaderLab__Unity_ => ResourceManager.GetString("InUnityShaderLabFile_PresentableShortName_ShaderLab__Unity_");
    public static string InUnityShaderLabFile_QuickListTitle_Unity_files => ResourceManager.GetString("InUnityShaderLabFile_QuickListTitle_Unity_files");
    public static string Message => ResourceManager.GetString("Message");
    public static string ParametersAreNotValidInThisLocationMessage => ResourceManager.GetString("ParametersAreNotValidInThisLocationMessage");
    public static string PossibleUnintendedUseOfUndeclaredPropertyPropertyMayBeSetFromCodeMessage => ResourceManager.GetString("PossibleUnintendedUseOfUndeclaredPropertyPropertyMayBeSetFromCodeMessage");
    public static string ReformatCode_Text => ResourceManager.GetString("ReformatCode_Text");
    public static string RemoveSwallowedToken_Text_Insert_new_line => ResourceManager.GetString("RemoveSwallowedToken_Text_Insert_new_line");
    public static string RemoveToken_Text_Remove_invalid_parameters => ResourceManager.GetString("RemoveToken_Text_Remove_invalid_parameters");
    public static string ThereIsAlreadyAPropertyNamedDeclaredMessage => ResourceManager.GetString("ThereIsAlreadyAPropertyNamedDeclaredMessage");
    public static string IntellisenseEnabledSettingShaderLab_s_Override_VS_IntelliSense_for_ShaderLab => ResourceManager.GetString("IntellisenseEnabledSettingShaderLab_s_Override_VS_IntelliSense_for_ShaderLab");
    public static string IntellisenseEnabledSettingShaderLab_s_ShaderLab__Unity__shader_files_ => ResourceManager.GetString("IntellisenseEnabledSettingShaderLab_s_ShaderLab__Unity__shader_files_");
    public static string ShaderLabAutopopupEnabledSettingsKey_s_In_variable_references => ResourceManager.GetString("ShaderLabAutopopupEnabledSettingsKey_s_In_variable_references");
    public static string ShaderLabAutopopupEnabledSettingsKey_s_In_keywords => ResourceManager.GetString("ShaderLabAutopopupEnabledSettingsKey_s_In_keywords");
    public static string ShaderLabAutopopupEnabledSettingsKey_s_ShaderLab => ResourceManager.GetString("ShaderLabAutopopupEnabledSettingsKey_s_ShaderLab");
    public static string ShaderLabFormatSettingsKey_s_Code_formatting_in_ShaderLab => ResourceManager.GetString("ShaderLabFormatSettingsKey_s_Code_formatting_in_ShaderLab");
    public static string ShaderLabFormatSettingsKey_s_BraceStyle => ResourceManager.GetString("ShaderLabFormatSettingsKey_s_BraceStyle");
    public static string ShaderLabFormattingStylePageSchema_Describe_Brace_rules => ResourceManager.GetString("ShaderLabFormattingStylePageSchema_Describe_Brace_rules");
    public static string ShaderLabFormattingStylePageSchema_PageName_ShaderLab_Formatting_Style => ResourceManager.GetString("ShaderLabFormattingStylePageSchema_PageName_ShaderLab_Formatting_Style");
    public static string InUnityShaderLabBlock_PresentableShortName => ResourceManager.GetString("InUnityShaderLabBlock_PresentableShortName");
    public static string InUnityShaderLabRoot_PresentableShortName => ResourceManager.GetString("InUnityShaderLabRoot_PresentableShortName");
    public static string InUnityShaderLabFile_Presentation => ResourceManager.GetString("InUnityShaderLabFile_Presentation");
    public static string InUnityShaderLabRoot_Presentation => ResourceManager.GetString("InUnityShaderLabRoot_Presentation");
    public static string InUnityShaderLabBlock_Presentation => ResourceManager.GetString("InUnityShaderLabBlock_Presentation");
    public static string BlockCommand_Text => ResourceManager.GetString("BlockCommand_Text");
    public static string Keyword_RiderPresentableName => ResourceManager.GetString("Keyword_RiderPresentableName");
    public static string Number_RiderPresentableName => ResourceManager.GetString("Number_RiderPresentableName");
    public static string FieldIdentifier_RiderPresentableName => ResourceManager.GetString("FieldIdentifier_RiderPresentableName");
    public static string FunctionIdentifier_RiderPresentableName => ResourceManager.GetString("FunctionIdentifier_RiderPresentableName");
    public static string TypeIdentifier_RiderPresentableName => ResourceManager.GetString("TypeIdentifier_RiderPresentableName");
    public static string VariableIdentifier_RiderPresentableName => ResourceManager.GetString("VariableIdentifier_RiderPresentableName");
    public static string PreprocessorLineContent_RiderPresentableName => ResourceManager.GetString("PreprocessorLineContent_RiderPresentableName");
    public static string InjectedLanguageFragment_RiderPresentableName => ResourceManager.GetString("InjectedLanguageFragment_RiderPresentableName");
    public static string String_RiderPresentableName => ResourceManager.GetString("String_RiderPresentableName");
  }
}