using System;
using System.Linq.Expressions;
using JetBrains.Annotations;
using JetBrains.Application.Settings;
using JetBrains.Application.UI.Options;
using JetBrains.Application.UI.Options.OptionsDialog;
using JetBrains.Application.UI.Options.OptionsDialog.SimpleOptions;
using JetBrains.DataFlow;
using JetBrains.Lifetimes;
using JetBrains.UI.RichText;

namespace JetBrains.ReSharper.Plugins.Unity.Application.UI.Options
{
    public class OptionsPageBase : CustomSimpleOptionsPage
    {
        protected OptionsPageBase(
            Lifetime lifetime,
            [NotNull] OptionsSettingsSmartContext settingsStore)
            : base(lifetime, settingsStore)
        {
        }

        [NotNull]
        protected IOptionEntity CheckBox<TKeyClass, TEntryMemberType>(
            [NotNull] Expression<Func<TKeyClass, TEntryMemberType>> setting,
            [NotNull] RichText caption,
            [CanBeNull] string tooltip = null)
        {
            var settingsScalarEntry = OptionsSettingsSmartContext.Schema.GetScalarEntry(setting);
            var clrType = settingsScalarEntry.ValueClrType.Bind();
            var valueProperty = new Property<TEntryMemberType>("Bool value property for type {0}".FormatEx(clrType.FullName));

            OptionsSettingsSmartContext.SetBinding(myLifetime, settingsScalarEntry, valueProperty);

            var option = AddBoolOption(setting, caption, tooltip ?? settingsScalarEntry.Description);

            SetIndent(option, myCurrentIndent);

            return option;
        }

        private int myCurrentIndent;

        protected void WithIndent(IOptionEntity entity)
        {
            SetIndent(entity, myCurrentIndent);
        }

        protected void Header([NotNull] RichText text)
        {
            SetIndent(AddHeader(text), myCurrentIndent);
        }

        protected void BeginSection()
        {
            myCurrentIndent++;
        }

        protected void EndSection()
        {
            myCurrentIndent--;
        }
    }
}