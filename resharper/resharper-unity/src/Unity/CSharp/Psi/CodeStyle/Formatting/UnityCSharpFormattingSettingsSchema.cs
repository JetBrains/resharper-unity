using JetBrains.Annotations;
using JetBrains.Application.Components;
using JetBrains.Application.Parts;
using JetBrains.Application.Settings;
using JetBrains.Application.Settings.Calculated.Interface;
using JetBrains.Lifetimes;
using JetBrains.ReSharper.Feature.Services.OptionPages;
using JetBrains.ReSharper.Feature.Services.OptionPages.CodeStyle;
using JetBrains.ReSharper.Features.Altering.CodeFormatter.CSharp;

namespace JetBrains.ReSharper.Plugins.Unity.CSharp.Psi.CodeStyle.Formatting
{
    [FormattingSettingsPresentationComponent(Instantiation.DemandAnyThreadUnsafe)]
    public class UnityCSharpFormattingSettingsSchema : OthersPageSchemaPart
    {
        public UnityCSharpFormattingSettingsSchema(
            Lifetime lifetime, 
            [NotNull] IContextBoundSettingsStoreLive smartContext, 
            [NotNull] IValueEditorViewModelFactory itemViewModelFactory, 
            [NotNull] IComponentContainer container, 
            [NotNull] ISettingsToHide settingsToHide,
            [NotNull] ICalculatedSettingsSchema calculatedSettingsSchema
        ) : base(lifetime, smartContext, itemViewModelFactory, container, settingsToHide, calculatedSettingsSchema)
        {
        }

        protected override string PagePartName => "Unity";
        
        protected override void DescribePart(SchemaBuilder builder)
        {
            builder.ItemFor((UnityCSharpFormattingSettingsKey x) => x.ENFORCE_CUSTOM_HEADER_FORMATTING, "", PreviewType.None);
            builder.ItemFor((UnityCSharpFormattingSettingsKey x) => x.BLANK_LINES_AFTER_HEADER, "", PreviewType.None);
        }
    }
}