#nullable enable
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
using JetBrains.ReSharper.Psi.Tree;
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

        public PsiLanguageType LanguageType => ShaderLabLanguage.Instance!;

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

        public void Process(IPsiSourceFile sourceFile, IRangeMarker? rangeMarker, CodeCleanupProfile profile,
            IProgressIndicator progressIndicator, IUserDataHolder cache)
        {
            var solution = sourceFile.GetSolution();

            if (!IsAvailable(profile)) return;

            var psiServices = sourceFile.GetPsiServices();
            IReadOnlyList<IFile> files;
            using (new ReleaseLockCookie(psiServices.Locks, LockKind.FullWrite))
            {
                psiServices.Locks.AssertReadAccessAllowed();
                if (rangeMarker is {DocumentRange: var documentRange} && documentRange.IsValid())
                    files = sourceFile.GetPsiServices().GetPsiFiles<KnownLanguage>(documentRange).ToIReadOnlyList();
                else
                    // we get all known languages instead of just ShaderLabLanguage to warm up injected HLSL (see https://youtrack.jetbrains.com/issue/DEXP-804035/Parsing-C-under-write-lock-leads-to-deadlock)
                    files = sourceFile.GetPsiFiles<KnownLanguage>().Where(x => x.Language.Is<ShaderLabLanguage>()).ToList();
            }

            using (progressIndicator.SafeTotal(Name, files.Count))
            {
                foreach (var file in files)
                {
                    using var indicator = progressIndicator.CreateSubProgress(1);
                    var service = file.Language.LanguageService();
                    if (service == null) return;

                    var formatter = service.CodeFormatter;
                    sourceFile.GetPsiServices().Transactions.Execute("Code cleanup", delegate
                    {
                        if (rangeMarker is { DocumentRange: var documentRange } && documentRange.IsValid())
                            CodeFormatterHelper.Format(file.Language, solution, documentRange, CodeFormatProfile.DEFAULT, true, false, indicator);
                        else
                            formatter?.FormatFile(file, CodeFormatProfile.DEFAULT, indicator);
                    });
                }
            }
        }

        public string Name => "Reformat ShaderLab";
    }
}