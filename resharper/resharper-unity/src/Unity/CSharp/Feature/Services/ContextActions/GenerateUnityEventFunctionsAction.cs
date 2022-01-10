using System;
using System.Collections.Generic;
using JetBrains.Application.Settings;
using JetBrains.Application.UI.Controls.BulbMenu.Anchors;
using JetBrains.Diagnostics;
using JetBrains.ProjectModel;
using JetBrains.ReSharper.Feature.Services.ContextActions;
using JetBrains.ReSharper.Feature.Services.CSharp.ContextActions;
using JetBrains.ReSharper.Feature.Services.Intentions;
using JetBrains.ReSharper.Plugins.Unity.Core.Application.Settings;
using JetBrains.ReSharper.Plugins.Unity.CSharp.Feature.Services.QuickFixes;
using JetBrains.ReSharper.Plugins.Unity.UnityEditorIntegration.Api;
using JetBrains.ReSharper.Psi.CSharp.Tree;
using JetBrains.ReSharper.Psi.Tree;
using JetBrains.ReSharper.Resources.Resources.Icons;
using JetBrains.Util;

namespace JetBrains.ReSharper.Plugins.Unity.CSharp.Feature.Services.ContextActions
{
    [ContextAction(Group = UnityContextActions.GroupID,
        Name = "Generate Unity event functions",
        Description = "Generate Unity event functions inside Unity type")]
    public class GenerateUnityEventFunctionsAction : IContextAction
    {
        private readonly ICSharpContextActionDataProvider myDataProvider;

        public GenerateUnityEventFunctionsAction(ICSharpContextActionDataProvider dataProvider)
        {
            myDataProvider = dataProvider;
        }

        public IEnumerable<IntentionAction> CreateBulbItems()
        {
            var node = myDataProvider.GetSelectedTreeNode<ITreeNode>();
            Assertion.Assert(node != null, "node != null");
            var classDeclaration = node.GetContainingNode<IClassLikeDeclaration>();

            var fix = new GenerateUnityEventFunctionsFix(classDeclaration, node);

            //RIDER-30526
            var action = new IntentionAction(fix, PsiFeaturesUnsortedThemedIcons.FuncZoneGenerate.Id,
                new SubmenuAnchor(BulbMenuAnchors.PermanentBackgroundItems, SubmenuBehavior.Executable));

            return new[] {action};
        }

        public bool IsAvailable(IUserDataHolder cache)
        {
            var node = myDataProvider.GetSelectedTreeNode<ITreeNode>();
            var classDeclaration = node?.GetContainingNode<IClassLikeDeclaration>();
            if (classDeclaration != null)
            {
                // This context action is already visible on the method declaration when the gutter icons are visible
                if (IsShowingGutterIcons(myDataProvider.Solution) && node.GetContainingNode<IClassBody>() == null)
                    return false;

                if (node.GetContainingNode<IMethodDeclaration>() == null &&
                    node.GetContainingNode<IPropertyDeclaration>() == null)
                {
                    var unityApi = myDataProvider.Solution.GetComponent<UnityApi>();
                    return unityApi.IsUnityType(classDeclaration.DeclaredElement);
                }
            }

            return false;
        }

        private static bool IsShowingGutterIcons(ISolution solution)
        {
            var settings = solution.GetSettingsStore();
            switch (settings.GetValue((UnitySettings key) => key.GutterIconMode))
            {
                case GutterIconMode.Always:  return true;
                case GutterIconMode.None:    return false;
                case GutterIconMode.CodeInsightDisabled:

                    // TODO: Let's avoid #defines
#if RIDER
                    // TODO: Fix this magic constant!!!!
                    // var provider = solution.GetComponent<JetBrains.ReSharper.Plugins.Unity.Rider.CodeInsights.UnityCodeInsightProvider>();
                    var providerId = "Unity implicit usage"; // provider.ProviderId;
                    return settings.GetIndexedValue(
                        (JetBrains.RdBackend.Common.Platform.CodeInsights.CodeInsightsSettings key) =>
                            key.DisabledProviders, providerId);
#else
                    return true;
#endif
                default:
                    throw new ArgumentOutOfRangeException();
            }
        }
    }
}