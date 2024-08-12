#nullable enable
using System.Collections.Generic;
using JetBrains.Application.Parts;
using JetBrains.Application.UI.Controls.BulbMenu.Items;
using JetBrains.Application.UI.Controls.Utils;
using JetBrains.Application.UI.PopupLayout;
using JetBrains.ProjectModel;
using JetBrains.ReSharper.Feature.Services.Daemon;
using JetBrains.ReSharper.Feature.Services.Navigation;
using JetBrains.ReSharper.Psi;
using JetBrains.ReSharper.Psi.CSharp.Tree;
using JetBrains.TextControl.DocumentMarkup;
using JetBrains.TextControl.DocumentMarkup.Adornments;
using JetBrains.Util;

namespace JetBrains.ReSharper.Plugins.Unity.CSharp.Daemon.Stages.Highlightings;

[SolutionComponent(Instantiation.DemandAnyThreadSafe)]
public class UnityObjectLifetimeCheckViaNullEqualityHintAdornmentProvider : IHighlighterAdornmentProvider
{
    public bool IsValid(IHighlighter highlighter) => highlighter.GetHighlighting() is UnityObjectNullComparisonHintHighlighting hint && hint.IsValid();

    public IAdornmentDataModel? CreateDataModel(IHighlighter highlighter)
    {
        if (highlighter.GetHighlighting() is UnityObjectNullComparisonHintHighlighting hint && hint.IsValid())
        {
            var data = new AdornmentData(hint.Text, hint.Icon, AdornmentFlags.IsNavigable, new AdornmentPlacement(UnityObjectNullComparisonHintHighlighting.DefaultOrder), PushToHintMode.Always);
            return new DataModel(data, hint.Expression);
        }

        return null;
    }

    class DataModel(AdornmentData data, IEqualityExpression expression) : IAdornmentDataModel
    {
        public AdornmentData Data => data;
        public IPresentableItem? ContextMenuTitle => null;
        public IEnumerable<BulbMenuItem> ContextMenuItems => EmptyList<BulbMenuItem>.Enumerable;

        public void ExecuteNavigation(PopupWindowContextSource? popupWindowContextSource)
        {
            if (expression.OperatorReference?.Resolve().DeclaredElement is not {} declaredElement)
                return;
            
            var solution = declaredElement.GetSolution();
            popupWindowContextSource ??= solution.GetComponent<IMainWindowPopupWindowContext>().Source;
            var navigationService = solution.GetComponent<IDeclaredElementNavigationService>();
            using (CompilationContextCookie.GetExplicitUniversalContextIfNotSet())
            {
                navigationService.Navigate(declaredElement, popupWindowContextSource, true);
            }
        }

        public TextRange? SelectionRange => null;
    }
}
