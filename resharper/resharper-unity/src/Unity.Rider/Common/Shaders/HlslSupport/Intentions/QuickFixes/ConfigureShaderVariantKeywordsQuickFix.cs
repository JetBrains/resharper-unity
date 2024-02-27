#nullable enable
using System;
using JetBrains.Application.Progress;
using JetBrains.ProjectModel;
using JetBrains.ReSharper.Feature.Services.QuickFixes;
using JetBrains.ReSharper.Plugins.Unity.Rider.Common.Protocol;
using JetBrains.ReSharper.Plugins.Unity.Rider.Common.Shaders.HlslSupport.Daemon.CodeInsights;
using JetBrains.ReSharper.Plugins.Unity.Rider.Resources;
using JetBrains.Rider.Model.Unity.FrontendBackend;
using JetBrains.TextControl;
using JetBrains.Util;

namespace JetBrains.ReSharper.Plugins.Unity.Rider.Common.Shaders.HlslSupport.Intentions.QuickFixes;

[QuickFix]
public class ConfigureShaderVariantKeywordsQuickFix(InactiveShaderVariantBranchHighlight inactiveShaderVariantBranchHighlight) : QuickFixBase
{
    public override string Text => Strings.ConfigureShaderVariantKeywordsQuickFix_Text;

    protected override Action<ITextControl>? ExecutePsiTransaction(ISolution solution, IProgressIndicator progress)
    {
        if (solution.TryGetComponent<IShaderVariantsHost>() is { } shaderVariantsHost)
            shaderVariantsHost.ShowShaderVariantInteraction(inactiveShaderVariantBranchHighlight.Range.StartOffset, ShaderVariantInteractionOrigin.QuickFix, inactiveShaderVariantBranchHighlight.ScopeKeywords);
        return null;
    }

    public override bool IsAvailable(IUserDataHolder cache) => inactiveShaderVariantBranchHighlight.IsValid();
}