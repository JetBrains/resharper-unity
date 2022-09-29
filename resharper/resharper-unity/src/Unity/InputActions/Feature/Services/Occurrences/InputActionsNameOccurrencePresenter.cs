using JetBrains.Application.UI.Controls.JetPopupMenu;
using JetBrains.Diagnostics;
using JetBrains.ProjectModel;
using JetBrains.ReSharper.Feature.Services.Occurrences;
using JetBrains.ReSharper.Feature.Services.Presentation;
using JetBrains.ReSharper.Plugins.Unity.InputActions.Psi.Caches;
using JetBrains.ReSharper.Plugins.Unity.InputActions.Psi.DeclaredElements;
using JetBrains.ReSharper.Psi;
using JetBrains.ReSharper.Resources.Shell;
using JetBrains.UI.Icons;
using JetBrains.UI.RichText;
using JetBrains.Util.Media;

#nullable enable

namespace JetBrains.ReSharper.Plugins.Unity.InputActions.Feature.Services.Occurrences
{
    // Used to present an occurrence. Surprisingly, used by (at least) Rider when double clicking on an element used as
    // a target in a find usage. All occurrences are added to a menu, presented but the menu will automatically click
    // a single entry. If there's no presenter, the element appears disabled, and can't be clicked.
    [OccurrencePresenter(Priority = 4.0)]
    public class InputActionsNameOccurrencePresenter : IOccurrencePresenter
    {
        public bool IsApplicable(IOccurrence occurrence) => occurrence is InputActionsNameOccurrence;

        public bool Present(IMenuItemDescriptor descriptor, IOccurrence occurrence,
                            OccurrencePresentationOptions options)
        {
            if (occurrence is not InputActionsNameOccurrence inputActionsNameOccurrence)
                return false;

            var solution = occurrence.GetSolution().NotNull("occurrence.GetSolution() != null");
            var cache = solution.GetComponent<InputActionsCache>();

            descriptor.Text = inputActionsNameOccurrence.Name;
            if (options.IconDisplayStyle != IconDisplayStyle.NoIcon)
                descriptor.Icon = GetIcon(solution, inputActionsNameOccurrence, options);

            // var fileName = cache.GetInputActionsLocationByAssemblyName(InputActionsNameOccurrence.Name);
            // if (fileName.IsNotEmpty)
            // {
            //     var style = TextStyle.FromForeColor(JetSystemColors.GrayText);
            //     descriptor.ShortcutText = new RichText($" in {fileName.Name}", style);
            //     descriptor.TailGlyph = InputActionsDeclaredElementType.InputActions.GetImage();
            // }
            descriptor.Style = MenuItemStyle.Enabled;
            return true;
        }

        private static IconId? GetIcon(ISolution solution,
                                       InputActionsNameOccurrence InputActionsNameOccurrence,
                                       OccurrencePresentationOptions options)
        {
            var presentationService = InputActionsNameOccurrence.SourceFile.GetPsiServices()
                .GetComponent<PsiSourceFilePresentationService>();
            switch (options.IconDisplayStyle)
            {
                case IconDisplayStyle.OccurrenceKind:
                    return OccurrencePresentationUtil.GetOccurrenceKindImage(InputActionsNameOccurrence, solution).Icon;

                case IconDisplayStyle.File:
                    var iconId = presentationService.GetIconId(InputActionsNameOccurrence.SourceFile);
                    if (iconId == null)
                    {
                        var projectFile = InputActionsNameOccurrence.SourceFile.ToProjectFile();
                        if (projectFile != null)
                        {
                            iconId = Shell.Instance.GetComponent<ProjectModelElementPresentationService>()
                                .GetIcon(projectFile);
                        }
                    }

                    if (iconId == null)
                        iconId = InputActionsDeclaredElementType.InputActions.GetImage();

                    return iconId;

                default:
                    return InputActionsDeclaredElementType.InputActions.GetImage();
            }
        }
    }
}