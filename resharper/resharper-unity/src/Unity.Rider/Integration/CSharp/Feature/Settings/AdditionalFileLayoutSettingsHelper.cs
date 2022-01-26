using System;
using JetBrains.Application.Settings;
using JetBrains.DataFlow;
using JetBrains.Lifetimes;
using JetBrains.ReSharper.Plugins.Unity.CSharp.Psi.CodeStyle;
using JetBrains.Rider.Backend.Features.Dialog;
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

        private static readonly object ourLocalChangeToken = new object();

        private readonly RiderDialogHost myDialogHost;
        private readonly string myDefaultWithRegions;
        private readonly string myDefaultWithoutRegions;

        public AdditionalFileLayoutSettingsHelper(in Lifetime lifetime, IContextBoundSettingsStore settingsContext,
            RiderDialogHost dialogHost)
        {
            myDialogHost = dialogHost;

            myDefaultWithoutRegions = AdditionalFileLayoutResources.DefaultAdditionalFileLayoutPatterns.ReplaceNewLines("\n");
            myDefaultWithRegions = AdditionalFileLayoutResources.DefaultAdditionalFileLayoutPatternsWithRegions.ReplaceNewLines("\n");

            var initialText = settingsContext.GetValue((AdditionalFileLayoutSettings s) => s.Pattern);
            if (initialText.IsNullOrEmpty()) initialText = AdditionalFileLayoutResources.DefaultAdditionalFileLayoutPatterns;
            initialText = initialText.ReplaceNewLines("\n");

            Text = new Property<string>(lifetime, "AdditionalFileLayoutSettingsHelper.Text", initialText);

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
                if (!myDialogHost.ShowYesNoMessageBox(
                    "You are about to replace the set of patterns with a default one." +
                    Environment.NewLine +
                    "This will remove all changes you might have made." + Environment.NewLine +
                    "Do you want to proceed?"))
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