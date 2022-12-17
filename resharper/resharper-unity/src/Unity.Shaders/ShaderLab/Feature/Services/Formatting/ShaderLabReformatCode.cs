using System;
using System.Collections.Generic;
using System.Linq;
using JetBrains.Application.Progress;
using JetBrains.Application.Threading;
using JetBrains.DocumentModel;
using JetBrains.ReSharper.Feature.Services.CodeCleanup;
using JetBrains.ReSharper.Plugins.Unity.Shaders.Resources;
using JetBrains.ReSharper.Plugins.Unity.Shaders.ShaderLab.Psi;
using JetBrains.ReSharper.Plugins.Unity.Shaders.ShaderLab.Psi.Tree;
using JetBrains.ReSharper.Psi;
using JetBrains.ReSharper.Psi.CodeStyle;
using JetBrains.ReSharper.Psi.Files;
using JetBrains.Util;

namespace JetBrains.ReSharper.Plugins.Unity.Shaders.ShaderLab.Feature.Services.Formatting
{
  [CodeCleanupModule]
  public class ShaderLabReformatCode : ICodeCleanupModule
  {
    // Arbitrary order number...
    private static readonly CodeCleanupLanguage ourShaderLabCleanupLanguage = new("ShaderLab", 12);

    private static readonly CodeCleanupSingleOptionDescriptor ourDescriptor =
        new CodeCleanupOptionDescriptor<bool>("ShaderLabReformatCode", ourShaderLabCleanupLanguage,
            CodeCleanupOptionDescriptor.ReformatGroup, displayName: Strings.ReformatCode_Text);

    public ICollection<CodeCleanupOptionDescriptor> Descriptors => new[] { ourDescriptor };

    public PsiLanguageType LanguageType => ShaderLabLanguage.Instance;

    public bool IsAvailableOnSelection => true;

    public void SetDefaultSetting(CodeCleanupProfile profile, CodeCleanupService.DefaultProfileType profileType)
    {
      switch (profileType)
      {
        case CodeCleanupService.DefaultProfileType.FULL:
        case CodeCleanupService.DefaultProfileType.REFORMAT:
        case CodeCleanupService.DefaultProfileType.CODE_STYLE:
          profile.SetSetting(ourDescriptor, true);
          break;
        default:
          throw new ArgumentOutOfRangeException("profileType");
      }
    }

    public bool IsAvailable(IPsiSourceFile sourceFile)
    {
      return sourceFile.IsLanguageSupported<ShaderLabLanguage>();
    }

    public bool IsAvailable(CodeCleanupProfile profile)
    {
        return profile.GetSetting(ourDescriptor) is true;
    }

    public void Process(IPsiSourceFile sourceFile, IRangeMarker rangeMarker, CodeCleanupProfile profile,
        IProgressIndicator progressIndicator, IUserDataHolder cache)
    {
      var solution = sourceFile.GetSolution();

      if (!IsAvailable(profile)) return;

      var psiServices = sourceFile.GetPsiServices();
      IShaderLabFile[] files;
      using (new ReleaseLockCookie(psiServices.Locks, LockKind.FullWrite))
      {
        psiServices.Locks.AssertReadAccessAllowed();
        files = sourceFile.GetPsiFiles<ShaderLabLanguage>().Cast<IShaderLabFile>().ToArray();
      }
      using (progressIndicator.SafeTotal(Name, files.Length))
      {
        foreach (var file in files)
        {
          using (var indicator = progressIndicator.CreateSubProgress(1))
          {
            var service = file.Language.LanguageService();
            if (service == null) return;

            var formatter = service.CodeFormatter;

            sourceFile.GetPsiServices().Transactions.Execute("Code cleanup", delegate
            {
              if (rangeMarker != null && rangeMarker.IsValid)
                CodeFormatterHelper.Format(file.Language,
                  solution, rangeMarker.DocumentRange, CodeFormatProfile.DEFAULT, true, false, indicator);
              else
              {
                formatter.FormatFile(
                  file,
                  CodeFormatProfile.DEFAULT,
                  indicator);
              }
            });
          }
        }
      }
    }

    public string Name => "Reformat ShaderLab";
  }
}