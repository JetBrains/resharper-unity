using JetBrains.Annotations;
using JetBrains.Application.Components;
using JetBrains.Application.Settings;
using JetBrains.Application.UI.Components;
using JetBrains.Application.UI.Options;
using JetBrains.Lifetimes;
using JetBrains.ReSharper.Feature.Services.OptionPages;
using JetBrains.ReSharper.Feature.Services.OptionPages.CodeEditing;
using JetBrains.ReSharper.Feature.Services.OptionPages.CodeStyle;
using JetBrains.ReSharper.Plugins.Unity.ShaderLab.Psi;
using JetBrains.ReSharper.Plugins.Unity.ShaderLab.Psi.Formatting;
using JetBrains.ReSharper.Psi;
using JetBrains.ReSharper.Psi.EditorConfig;
using JetBrains.ReSharper.Resources.Resources.Icons;

namespace JetBrains.ReSharper.Plugins.Unity.ShaderLab.Feature.Services.Formatting
{
    [OptionsPage(
        PID,
        "ShaderLab Formatting Style",
        typeof(PsiFeaturesUnsortedOptionsThemedIcons.Indent),
        ParentId = CodeEditingPage.PID,
        Sequence = 0,
        FilterTags = new[] {ConfigFileUtils.EditorConfigName})]
    public class ShaderLabFormattingStylePage : CodeStylePage
    {
        public const string PID = "ShaderLabFormattingStylePage";

        public override bool ShowAutoDetectAndConfigureFormattingTip => true;

        public override string Id => PID;

        public ShaderLabFormattingStylePage(Lifetime lifetime,
                                            [NotNull] OptionsSettingsSmartContext smartContext,
                                            [NotNull] IUIApplication environment,
                                            [NotNull] ShaderLabFormattingStylePageSchema schema,
                                            [NotNull] CodeStylePreview preview, IComponentContainer container)
            : base(lifetime, smartContext, environment, schema, preview, container)
        {
        }
    }

    [FormattingSettingsPresentationComponent]
    public class ShaderLabFormattingStylePageSchema : IndentStylePageSchema<ShaderLabFormatSettingsKey, ShaderLabCodeStylePreview>
    {
        public ShaderLabFormattingStylePageSchema(Lifetime lifetime,
                                                  [NotNull] IContextBoundSettingsStoreLive smartContext,
                                                  [NotNull] IValueEditorViewModelFactory itemViewModelFactory,
                                                  IComponentContainer container, ISettingsToHide settingsToHide)
            : base(lifetime, smartContext, itemViewModelFactory, container, settingsToHide)
        {
        }

        protected override void Describe(SchemaBuilder builder)
        {
            base.Describe(builder);

            builder
                .Category("Brace rules")

                .ItemFor(
                    key => key.BraceStyle,
                    "Shader \"Unlit/NewUnlitShader\"\n{\n    Properties\n    {\n        _MainTex (\"Texture\", 2D) = \"white\" {}\n    }\n    SubShader\n    {\n        Tags { \"RenderType\"=\"Opaque\" }\n        LOD 100\n\n        Pass\n        {\n\n        }\n    }\n}")
                .EndCategory();
        }

        public override KnownLanguage Language => ShaderLabLanguage.Instance;
        public override string PageName => "ShaderLab Formatting Style";
    }
}