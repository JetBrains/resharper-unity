using JetBrains.Application.UI.Controls.JetPopupMenu;
using JetBrains.Diagnostics;
using JetBrains.ProjectModel;
using JetBrains.ReSharper.Feature.Services.Occurrences;
using JetBrains.ReSharper.Feature.Services.Presentation;
using JetBrains.ReSharper.Plugins.Unity.AsmDef.Psi.Caches;
using JetBrains.ReSharper.Plugins.Unity.AsmDef.Psi.DeclaredElements;
using JetBrains.ReSharper.Psi;
using JetBrains.ReSharper.Resources.Shell;
using JetBrains.UI.Icons;
using JetBrains.UI.RichText;
using JetBrains.Util.Media;
using JetBrains.Util.NetFX.Media.Colors;

#nullable enable

namespace JetBrains.ReSharper.Plugins.Unity.AsmDef.Feature.Services.Occurrences
{
    // Used to present an occurrence. Surprisingly, used by (at least) Rider when double clicking on an element used as
    // a target in a find usage. All occurrences are added to a menu, presented but the menu will automatically click
    // a single entry. If there's no presenter, the element appears disabled, and can't be clicked.
    [OccurrencePresenter(Priority = 4.0)]
    public class AsmDefNameOccurrencePresenter : IOccurrencePresenter
    {
        public bool IsApplicable(IOccurrence occurrence) => occurrence is AsmDefNameOccurrence;

        public bool Present(IMenuItemDescriptor descriptor, IOccurrence occurrence,
                            OccurrencePresentationOptions options)
        {
            if (occurrence is not AsmDefNameOccurrence asmDefNameOccurrence)
                return false;

            var solution = occurrence.GetSolution().NotNull("occurrence.GetSolution() != null");
            var cache = solution.GetComponent<AsmDefCache>();

            descriptor.Text = asmDefNameOccurrence.Name;
            if (options.IconDisplayStyle != IconDisplayStyle.NoIcon)
                descriptor.Icon = GetIcon(solution, asmDefNameOccurrence, options);

            var fileName = cache.GetAsmDefLocationByAssemblyName(asmDefNameOccurrence.Name);
            if (fileName.IsNotEmpty)
            {
                var style = TextStyle.FromForeColor(JetSystemColors.GrayText);
                descriptor.ShortcutText = new RichText($" in {fileName.Name}", style);
                descriptor.TailGlyph = AsmDefDeclaredElementType.AsmDef.GetImage();
            }
            descriptor.Style = MenuItemStyle.Enabled;
            return true;
        }

        private static IconId? GetIcon(ISolution solution,
                                       AsmDefNameOccurrence asmDefNameOccurrence,
                                       OccurrencePresentationOptions options)
        {
            var presentationService = asmDefNameOccurrence.SourceFile.GetPsiServices()
                .GetComponent<PsiSourceFilePresentationService>();
            switch (options.IconDisplayStyle)
            {
                case IconDisplayStyle.OccurrenceKind:
                    return OccurrencePresentationUtil.GetOccurrenceKindImage(asmDefNameOccurrence, solution).Icon;

                case IconDisplayStyle.File:
                    var iconId = presentationService.GetIconId(asmDefNameOccurrence.SourceFile);
                    if (iconId == null)
                    {
                        var projectFile = asmDefNameOccurrence.SourceFile.ToProjectFile();
                        if (projectFile != null)
                        {
                            iconId = Shell.Instance.GetComponent<ProjectModelElementPresentationService>()
                                .GetIcon(projectFile);
                        }
                    }

                    if (iconId == null)
                        iconId = AsmDefDeclaredElementType.AsmDef.GetImage();

                    return iconId;

                default:
                    return AsmDefDeclaredElementType.AsmDef.GetImage();
            }
        }
    }
}