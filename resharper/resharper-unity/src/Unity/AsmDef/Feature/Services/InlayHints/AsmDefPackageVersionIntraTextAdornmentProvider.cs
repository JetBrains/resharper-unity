using JetBrains.Application.Settings;
using JetBrains.ProjectModel;
using JetBrains.TextControl.DocumentMarkup;

#nullable enable

namespace JetBrains.ReSharper.Plugins.Unity.AsmDef.Feature.Services.InlayHints
{
    [SolutionComponent]
    public class AsmDefPackageVersionIntraTextAdornmentProvider : IHighlighterIntraTextAdornmentProvider
    {
        private readonly ISolution mySolution;
        private readonly ISettingsStore mySettingsStore;

        public AsmDefPackageVersionIntraTextAdornmentProvider(ISolution solution, ISettingsStore settingsStore)
        {
            mySolution = solution;
            mySettingsStore = settingsStore;
        }

        public bool IsValid(IHighlighter highlighter)
        {
            return highlighter.UserData is AsmDefPackageVersionInlayHintHighlighting highlighting &&
                   highlighting.IsValid();
        }

        public IIntraTextAdornmentDataModel? CreateDataModel(IHighlighter highlighter)
        {
            if (highlighter.UserData is AsmDefPackageVersionInlayHintHighlighting highlighting &&
                highlighting.IsValid())
            {
                return new AsmDefIntraTextAdornmentModel(highlighting, s => s.ShowAsmDefVersionDefinePackageVersions,
                    mySolution, mySettingsStore);
            }

            return null;
        }
    }
}