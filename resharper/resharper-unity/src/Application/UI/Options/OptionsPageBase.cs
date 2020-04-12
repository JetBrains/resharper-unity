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

            return AddBoolOption(setting, caption, tooltip ?? settingsScalarEntry.Description);
        }

        private int myCurrentIndent;

        protected IOptionEntity WithIndent(IOptionEntity entity)
        {
            SetIndent(entity, myCurrentIndent);
            return entity;
        }

        protected void Header([NotNull] string text)
        {
            WithIndent(AddHeader(text));
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