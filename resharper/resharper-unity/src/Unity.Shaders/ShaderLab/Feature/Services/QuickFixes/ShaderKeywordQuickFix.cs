#nullable enable
using System;
using System.Collections.Generic;
using System.Collections.Immutable;
using JetBrains.Application.I18n;
using JetBrains.Application.Progress;
using JetBrains.ProjectModel;
using JetBrains.ReSharper.Feature.Services.Bulbs;
using JetBrains.ReSharper.Feature.Services.Intentions;
using JetBrains.ReSharper.Feature.Services.QuickFixes;
using JetBrains.ReSharper.Plugins.Unity.Shaders.HlslSupport.Daemon.Highlightings;
using JetBrains.ReSharper.Plugins.Unity.Shaders.HlslSupport.ShaderVariants;
using JetBrains.ReSharper.Plugins.Unity.Shaders.Resources;
using JetBrains.TextControl;
using JetBrains.Util;
using JetBrains.Util.DataStructures.Collections;

namespace JetBrains.ReSharper.Plugins.Unity.Shaders.ShaderLab.Feature.Services.QuickFixes;

[QuickFix]
public class ShaderKeywordQuickFix : IQuickFix
{
    private readonly ShaderKeywordHighlight myHighlight;
    
    public ShaderKeywordQuickFix(ShaderKeywordHighlight highlight)
    {
        myHighlight = highlight;
    }

    public IEnumerable<IntentionAction> CreateBulbItems() =>
        myHighlight switch
        {
            EnabledShaderKeywordHighlight => new DisableShaderKeyword(myHighlight.Keyword).ToContextActionIntentions(),
            ImplicitlyEnabledShaderKeywordHighlight when ShaderDefineSymbolsRecognizer.Recognize(myHighlight.Keyword) is null => 
                new EnableShaderKeyword(myHighlight.Keyword).ToContextActionIntentions(),
            DisabledShaderKeywordHighlight => new EnableShaderKeyword(myHighlight.Keyword).ToContextActionIntentions(),
            SuppressedShaderKeywordHighlight shaderKeywordHighlight =>
                new DisableShaderKeyword(myHighlight.Keyword).ToContextActionIntention() is var disableShaderKeyword &&
                shaderKeywordHighlight.SuppressorsString is { } suppressorsString
                    ? FixedList.Of(new UnSuppressShaderKeyword(shaderKeywordHighlight.Suppressors, suppressorsString).ToContextActionIntention(), disableShaderKeyword)
                    : FixedList.Of(disableShaderKeyword),
            _ => EmptyList<IntentionAction>.Enumerable
        };

    public bool IsAvailable(IUserDataHolder cache) => myHighlight.IsValid();
    
    private class DisableShaderKeyword : BulbActionBase
    {
        private readonly string myKeyword;

        public DisableShaderKeyword(string keyword) => myKeyword = keyword;

        public override string Text => Strings.DisableShaderKeyword_Disable_shader_keyword;
        protected override Action<ITextControl> ExecutePsiTransaction(ISolution solution, IProgressIndicator progress) => 
            _ => solution.GetComponent<ShaderVariantsManager>().SetDefineSymbolEnabled(myKeyword, false);
    }
    
    private class EnableShaderKeyword : BulbActionBase
    {
        private readonly string myKeyword;

        public EnableShaderKeyword(string keyword) => myKeyword = keyword;

        public override string Text => Strings.EnableShaderKeyword_Enable_shader_keyword;
        protected override Action<ITextControl> ExecutePsiTransaction(ISolution solution, IProgressIndicator progress) =>
            _ => solution.GetComponent<ShaderVariantsManager>().SetDefineSymbolEnabled(myKeyword, true);
    }
    
    private class UnSuppressShaderKeyword : BulbActionBase
    {
        private readonly IReadOnlyList<string> mySuppressors;
        private readonly string mySuppressorsString;

        public UnSuppressShaderKeyword(ImmutableArray<string> suppressors, string suppressorsString)
        {
            mySuppressors = suppressors;
            mySuppressorsString = suppressorsString;
        }

        public override string Text => Strings.UnSuppressShaderKeyword_Disable_conflicting_shader_keyword.Format(mySuppressorsString);

        protected override Action<ITextControl> ExecutePsiTransaction(ISolution solution, IProgressIndicator progress) =>
            _ =>
            {
                var manager = solution.GetComponent<ShaderVariantsManager>();
                foreach (var suppressor in mySuppressors)
                    manager.SetKeywordEnabled(suppressor, false);
            };
    }
}