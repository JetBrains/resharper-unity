using System.Collections.Generic;
using JetBrains.DocumentModel;
using JetBrains.ProjectModel;
using JetBrains.ReSharper.Feature.Services.Daemon;
using JetBrains.ReSharper.Feature.Services.Intentions;
using JetBrains.ReSharper.Plugins.Unity.Feature.Services.QuickFixes;
using JetBrains.ReSharper.Psi.CSharp.Tree;
using JetBrains.ReSharper.Psi.Tree;
using JetBrains.ReSharper.Resources.Resources.Icons;
using JetBrains.TextControl;
using JetBrains.UI.BulbMenu;
using JetBrains.Util;

namespace JetBrains.ReSharper.Plugins.Unity.Daemon.Stages.Highlightings
{
    [StaticSeverityHighlighting(Severity.INFO, HighlightingGroupIds.GutterMarksGroup,
        OverlapResolve = OverlapResolveKind.NONE, AttributeId = UnityHighlightingAttributeIds.UNITY_GUTTER_ICON_ATTRIBUTE)]
    public class UnityMarkOnGutter : IHighlighting
    {
        private readonly ITreeNode myElement;
        private readonly DocumentRange myRange;

        public UnityMarkOnGutter(ITreeNode element, DocumentRange range, string tooltip)
        {
            myElement = element;
            myRange = range;
            ToolTip = tooltip;
        }

        public bool IsValid()
        {
            return myElement == null || myElement.IsValid();
        }

        public DocumentRange CalculateRange() => myRange;
        public string ToolTip { get; }
        public string ErrorStripeToolTip => ToolTip;

        public IEnumerable<BulbMenuItem> GetBulbMenuItems(ISolution solution, ITextControl textControl)
        {
            var declaration = myElement as IClassLikeDeclaration;
            if (declaration != null)
            {
                var foo = new GenerateUnityEventFunctionsFix(declaration);
                return new[]
                {
                    new BulbMenuItem(new IntentionAction.MyExecutableProxi(foo, solution, textControl), "Generate Unity event functions", PsiFeaturesUnsortedThemedIcons.FuncZoneGenerate.Id, BulbMenuAnchors.FirstClassContextItems)
                };
            }

            return EmptyList<BulbMenuItem>.Enumerable;
        }
    }
}