using System.Text;
using JetBrains.Application.UI.PopupLayout;
using JetBrains.IDE;
using JetBrains.ProjectModel;
using JetBrains.ReSharper.Feature.Services.Navigation.NavigationExtensions;
using JetBrains.ReSharper.Feature.Services.Occurrences;
using JetBrains.ReSharper.Psi;
using JetBrains.ReSharper.Psi.Files;
using JetBrains.ReSharper.Psi.JavaScript.LanguageImpl.JSon;
using JetBrains.Util;

namespace JetBrains.ReSharper.Plugins.Unity.Json.Feature.Services.Occurrences
{
    public class AsmDefNameOccurrence : IOccurrence
    {
        private readonly string myName;
        private readonly IPsiSourceFile mySourceFile;
        private readonly int myDeclaredElementTreeOffset;
        private readonly int myNavigationTreeOffset;
        private readonly ISolution mySolution;

        public AsmDefNameOccurrence(string name, IPsiSourceFile sourceFile,
            int declaredElementTreeOffset, int navigationTreeOffset, ISolution solution)
        {
            myName = name;
            mySourceFile = sourceFile;
            myDeclaredElementTreeOffset = declaredElementTreeOffset;
            myNavigationTreeOffset = navigationTreeOffset;
            mySolution = solution;

            PresentationOptions = OccurrencePresentationOptions.DefaultOptions;
        }

        public bool Navigate(ISolution solution, PopupWindowContextSource windowContext, bool transferFocus,
            TabOptions tabOptions = TabOptions.Default)
        {
            var psiFile = mySourceFile.GetPrimaryPsiFile();
            if (psiFile == null || !psiFile.Language.Is<JsonLanguage>())
                return false;

            var range = TextRange.FromLength(myNavigationTreeOffset, myName.Length);
            return mySourceFile.Navigate(range, transferFocus, tabOptions, windowContext);
        }

        public ISolution GetSolution() => mySolution;
        public OccurrenceType OccurrenceType => OccurrenceType.TextualOccurrence;
        public bool IsValid => true;
        public OccurrencePresentationOptions PresentationOptions { get; set; }

        public string DumpToString()
        {
            // +2 for the quotes, +1 to put it after the quote
            var range = TextRange.FromLength(myDeclaredElementTreeOffset, myName.Length + 3);
            var line = RangeOccurrenceUtil.GetTrimmedLinePossibleMultiline(mySourceFile,
                    range, null, out var occurrenceInLineRange)
                .Insert(occurrenceInLineRange.StartOffset, "|")
                .Insert(occurrenceInLineRange.EndOffset, "|");
            var builder = new StringBuilder();
            builder.AppendFormat("TO: [O] {0}", line);
            builder.AppendFormat(" RANGE: {0} @ {1}", range.ToInvariantString(),
                mySourceFile.DisplayName);
            return builder.ToString();
        }
    }
}