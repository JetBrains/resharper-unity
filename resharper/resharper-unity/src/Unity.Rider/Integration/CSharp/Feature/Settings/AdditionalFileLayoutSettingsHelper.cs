#nullable enable

using System;
using JetBrains.Application.Settings;
using JetBrains.DataFlow;
using JetBrains.Lifetimes;
using JetBrains.ReSharper.Plugins.Unity.CSharp.Psi.CodeStyle;
using JetBrains.ReSharper.Plugins.Unity.Rider.Resources;
using JetBrains.Rider.Backend.Features.Settings.OptionsPage.CSharpFileLayout;
using JetBrains.Util;

namespace JetBrains.ReSharper.Plugins.Unity.Rider.Integration.CSharp.Feature.Settings
{
    public class AdditionalFileLayoutSettingsHelper
    {
        private const string EmptyPattern = "<?xml version=\"1.0\" encoding=\"utf-8\"?>\n" +
                                            "<Patterns xmlns=\"urn:schemas-jetbrains-com:member-reordering-patterns\">\n" +
                                            "    \n" +
                                            "</Patterns>";

        private static readonly object ourLocalChangeToken = new();

        private readonly string myDefaultWithRegions;
        private readonly string myDefaultWithoutRegions;

        public AdditionalFileLayoutSettingsHelper(in Lifetime lifetime, IContextBoundSettingsStore settingsContext)
        {
            myDefaultWithoutRegions = AdditionalFileLayoutResources.DefaultAdditionalFileLayoutPatterns.ReplaceNewLines("\n");
            myDefaultWithRegions = AdditionalFileLayoutResources.DefaultAdditionalFileLayoutPatternsWithRegions.ReplaceNewLines("\n");

            var initialText = settingsContext.GetValue((AdditionalFileLayoutSettings s) => s.Pattern);
            if (initialText.IsNullOrEmpty()) initialText = AdditionalFileLayoutResources.DefaultAdditionalFileLayoutPatterns;
            initialText = initialText.ReplaceNewLines("\n");

            Text = new Property<string>("AdditionalFileLayoutSettingsHelper.Text", initialText);

            Text.Change.Advise_NoAcknowledgement(lifetime, args =>
            {
                if (!args.HasNew) return;
                var text = args.New;
                settingsContext.SetValue((AdditionalFileLayoutSettings s) => s.Pattern, text);
            });
        }

        public readonly IProperty<string> Text;

        public void LoadDefaultPattern(DefaultPatternKind kind)
        {
            var previousText = Text.Value;

            if (previousText != myDefaultWithRegions && previousText != myDefaultWithoutRegions &&
                previousText != EmptyPattern)
            {
                if (!MessageBox.ShowYesNo(
                    string.Format(Strings.AdditionalFileLayoutSettingsHelper_LoadDefaultPattern_You_are_about_to_replace_the_set_of_patterns_with_a_default_one___0_This_will_remove_all_changes_you_might_have_made__1_Do_you_want_to_proceed_, Environment.NewLine)))
                {
                    return;
                }
            }

            switch (kind)
            {
                case DefaultPatternKind.Empty:
                    Text.SetValue(EmptyPattern, ourLocalChangeToken);
                    break;
                case DefaultPatternKind.WithRegions:
                    Text.SetValue(myDefaultWithRegions, ourLocalChangeToken);
                    break;
                case DefaultPatternKind.WithoutRegions:
                    Text.SetValue(myDefaultWithoutRegions, ourLocalChangeToken);
                    break;
                default:
                    throw new ArgumentOutOfRangeException(nameof(kind), kind, null);
            }
        }
    }
}
