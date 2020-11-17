using System;
using JetBrains.Annotations;
using JetBrains.Application.UI.PopupLayout;
using JetBrains.Diagnostics;
using JetBrains.IDE;
using JetBrains.ProjectModel;
using JetBrains.ReSharper.Feature.Services.Presentation;
using JetBrains.ReSharper.Plugins.Unity.Yaml.Psi.DeferredCaches.AnimationEventsUsages;
using JetBrains.ReSharper.Psi;
using JetBrains.ReSharper.Psi.Pointers;
using JetBrains.ReSharper.Resources.Shell;
using JetBrains.UI.RichText;
using JetBrains.Util.Extension;

namespace JetBrains.ReSharper.Plugins.Unity.Yaml.Feature.Services.Navigation
{
    public class UnityAnimationEventOccurence : UnityAssetOccurrence
    {
        [NotNull] private AnimationUsage Usage { get; }

        public UnityAnimationEventOccurence([NotNull] IPsiSourceFile sourceFile,
                                            [NotNull] IDeclaredElementPointer<IDeclaredElement> declaredElement,
                                            [NotNull] AnimationUsage usage)
            : base(sourceFile, declaredElement, usage.Location, false)
        {
            Usage = usage;
        }

        public override RichText GetDisplayText()
        {
            var time = Convert.ToInt32(Usage.Time * Usage.SampleRate);
            var digitsCount = Math.Floor(Math.Log10(Usage.SampleRate) + 1);
            var formattedMod = (time % Usage.SampleRate).ToString("D" + digitsCount);
            return new RichText($"{Usage.AnimationName} at {time / Usage.SampleRate}:{formattedMod}");
        }

        public override string GetRelatedFilePresentation()
        {
            return SourceFile.NotNull().Name.SubstringBeforeLast(".") + " animation";
        }
        
        public override bool Navigate([NotNull] ISolution solution,
                                      [NotNull] PopupWindowContextSource windowContext,
                                      bool transferFocus,
                                      TabOptions tabOptions = TabOptions.Default)
        {
            if (DeclaredElementPointer is null) return true;
            var navigator = solution.GetComponent<UnityAssetOccurrenceNavigator>();
            return navigator.Navigate(solution, DeclaredElementPointer, OwningElementLocation);
        }

        public override string ToString()
        {
            var pointer = DeclaredElementPointer;
            if (pointer is null) return "Invalid";
            using (ReadLockCookie.Create())
            {
                using (CompilationContextCookie.GetExplicitUniversalContextIfNotSet())
                {
                    var element = pointer.FindDeclaredElement();
                    if (element == null) return "Invalid";
                    var language = element.PresentationLanguage;
                    return DeclaredElementMenuItemFormatter.FormatText(element, language, out _)?.Text ?? "Invalid";
                }
            }
        }
    }
}