using System.Text;
using JetBrains.Application.UI.PopupLayout;
using JetBrains.IDE;
using JetBrains.ProjectModel;
using JetBrains.ReSharper.Feature.Services.Navigation.NavigationExtensions;
using JetBrains.ReSharper.Feature.Services.Occurrences;
using JetBrains.ReSharper.Plugins.Json.Psi;
using JetBrains.ReSharper.Psi;
using JetBrains.ReSharper.Psi.Files;
using JetBrains.Util;

namespace JetBrains.ReSharper.Plugins.Unity.AsmDef.Feature.Services.Occurrences
{
    public class AsmDefNameOccurrence : IOccurrence
    {
        private readonly ISolution mySolution;
        private readonly int myDeclaredElementTreeOffset;

        public readonly string Name;
        public readonly IPsiSourceFile SourceFile;
        public readonly int NavigationTreeOffset;

        public AsmDefNameOccurrence(string name,
                                    IPsiSourceFile sourceFile,
                                    int declaredElementTreeOffset,
                                    int navigationTreeOffset,
                                    ISolution solution)
        {
            Name = name;
            SourceFile = sourceFile;
            myDeclaredElementTreeOffset = declaredElementTreeOffset;
            NavigationTreeOffset = navigationTreeOffset;
            mySolution = solution;

            PresentationOptions = OccurrencePresentationOptions.DefaultOptions;
        }

        public bool Navigate(ISolution solution, PopupWindowContextSource windowContext, bool transferFocus,
                             TabOptions tabOptions = TabOptions.Default)
        {
            var psiFile = SourceFile.GetPrimaryPsiFile();
            if (psiFile == null || !psiFile.Language.Is<JsonNewLanguage>())
                return false;

            var range = TextRange.FromLength(NavigationTreeOffset, Name.Length);
            return SourceFile.Navigate(range, transferFocus, tabOptions, windowContext);
        }

        public ISolution GetSolution() => mySolution;
        public OccurrenceType OccurrenceType => OccurrenceType.TextualOccurrence;
        public bool IsValid => true;
        public OccurrencePresentationOptions PresentationOptions { get; set; }

        public string DumpToString()
        {
            // +2 for the quotes, +1 to put it after the quote
            var range = TextRange.FromLength(myDeclaredElementTreeOffset, Name.Length + 3);
            var line = RangeOccurrenceUtil.GetTrimmedLinePossibleMultiline(SourceFile,
                    range, null, out var occurrenceInLineRange)
                .Insert(occurrenceInLineRange.StartOffset, "|")
                .Insert(occurrenceInLineRange.EndOffset, "|");
            var builder = new StringBuilder();
            builder.AppendFormat("TO: [O] {0}", line);
            builder.AppendFormat(" RANGE: {0} @ {1}", range.ToInvariantString(),
                SourceFile.DisplayName);
            return builder.ToString();
        }
    }
}