namespace JetBrains.ReSharper.Plugins.Unity.Shaders.Resources
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

    public static string CgLanguageSpecificDaemonBehaviour_InitialErrorStripe_File_s_primary_language_in_not_Cg => ResourceManager.GetString("CgLanguageSpecificDaemonBehaviour_InitialErrorStripe_File_s_primary_language_in_not_Cg");
    public static string CgSyntaxHighlightingProcess_VisitSemanticNode_Semantic__packoffset_or_register_expected => ResourceManager.GetString("CgSyntaxHighlightingProcess_VisitSemanticNode_Semantic__packoffset_or_register_expected");
    public static string CgBuiltInTypeTokenNodeType_TokenRepresentation_built_in_type => ResourceManager.GetString("CgBuiltInTypeTokenNodeType_TokenRepresentation_built_in_type");
  }
}