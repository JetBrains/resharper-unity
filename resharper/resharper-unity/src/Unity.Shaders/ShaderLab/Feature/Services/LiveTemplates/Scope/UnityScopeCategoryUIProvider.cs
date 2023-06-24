#nullable enable
using System.Collections.Generic;
using System.Linq;
using JetBrains.Application.I18n;
using JetBrains.DataFlow;
using JetBrains.IDE.UI.Extensions;
using JetBrains.Lifetimes;
using JetBrains.ReSharper.Feature.Services.LiveTemplates.Scope;
using JetBrains.ReSharper.Feature.Services.LiveTemplates.Templates;
using JetBrains.ReSharper.Plugins.Unity.Resources.Icons;
using JetBrains.ReSharper.Plugins.Unity.Shaders.ShaderLab.Psi.Parsing;
using JetBrains.ReSharper.Psi.Resources;
using JetBrains.Rider.Model.UIAutomation;
using Strings = JetBrains.ReSharper.Plugins.Unity.Shaders.Resources.Strings;

namespace JetBrains.ReSharper.Plugins.Unity.Shaders.ShaderLab.Feature.Services.LiveTemplates.Scope
{
    [ScopeCategoryUIProvider(Priority = Priority)]
    public class UnityScopeCategoryUIProvider : ScopeCategoryUIProvider
    {
        // Needs to be less than other priorities in R#'s built in ScopeCategoryUIProvider
        // to push it to the end of the list
        private const int Priority = -200;

        static UnityScopeCategoryUIProvider()
        {
            TemplateImage.Register("UnityShaderLab", PsiSymbolsThemedIcons.FileShader.Id);
        }

        public UnityScopeCategoryUIProvider()
            : base(LogoIcons.Unity.Id)
        {
            MainPoint = new InUnityShaderLabFile();
        }

        public override IEnumerable<ITemplateScopePoint> BuildAllPoints()
        {
            yield return new InUnityShaderLabFile();
            yield return new MustBeInShaderLabBlock(ShaderLabTokenType.SHADER_KEYWORD);
        }

        public override string CategoryCaption => Strings.InUnityShaderLabFile_PresentableShortName_ShaderLab__Unity_;

        public override string Present(ITemplateScopePoint point)
        {
            return point switch
            {
                MustBeInShaderLabBlock inShaderLabBlock => Strings.InUnityShaderLabBlock_Presentation.Format(inShaderLabBlock.CommandKeyword.TokenRepresentation),
                InUnityShaderLabFile => Strings.InUnityShaderLabFile_Presentation,
                _ => base.Present(point)
            };
        }

        public override bool HaveOptionsUIFor(ITemplateScopePoint point) => point is MustBeInShaderLabBlock;

        public override IScopeOptionsUIBase? CreateUI(ITemplateScopePoint point) =>
            point switch
            {
                MustBeInShaderLabBlock inShaderLabBlock => new InShaderBlockOptionsScopeBe(inShaderLabBlock),
                _ => null
            };
        
        private class InShaderBlockOptionsScopeBe : ScopeOptionsBeBase
        {
            private readonly MustBeInShaderLabBlock myInShaderLabBlock;
            private string? myLastResult;

            public InShaderBlockOptionsScopeBe(MustBeInShaderLabBlock inShaderLabBlock)
            {
                myInShaderLabBlock = inShaderLabBlock;
            }

            public override void Done()
            {
                if (myLastResult != null && MustBeInShaderLabBlock.TryCreateFromCommandKeyword(myLastResult) is {} scopePoint)
                    InvokeNewScopePoint(scopePoint);
            }

            public override BeControl GetBeControl(Lifetime lifetime)
            {
                var commandKeywords = MustBeInShaderLabBlock.KnownKeywords.ToArray();
                var minLevel = new Property<string>(MustBeInShaderLabBlock.BlockKeywordAttributeName, myInShaderLabBlock.CommandKeyword.TokenRepresentation);
                minLevel.Change.Advise_NewNotNull(lifetime, args =>
                {
                    myLastResult = args.New;
                });

                return BeControls.GetAutoGrid(GridOrientation.Vertical, Strings.BlockCommand_Text.GetBeLabel(), minLevel.GetBeComboBox(lifetime, commandKeywords));
            }
        }
    }
}