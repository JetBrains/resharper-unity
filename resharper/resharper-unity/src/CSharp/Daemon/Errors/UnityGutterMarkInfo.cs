using System.Collections.Generic;
using JetBrains.Application.UI.Controls.BulbMenu.Anchors;
using JetBrains.Application.UI.Controls.BulbMenu.Items;
using JetBrains.ProjectModel;
using JetBrains.ReSharper.Feature.Services.Bulbs;
using JetBrains.ReSharper.Feature.Services.Daemon;
using JetBrains.ReSharper.Feature.Services.Intentions;
using JetBrains.ReSharper.Feature.Services.Resources;
using JetBrains.ReSharper.Plugins.Unity.CSharp.Daemon.Stages.Highlightings;
using JetBrains.ReSharper.Plugins.Unity.CSharp.Feature.Services.Bulbs;
using JetBrains.ReSharper.Plugins.Unity.CSharp.Feature.Services.QuickFixes;
using JetBrains.ReSharper.Psi;
using JetBrains.ReSharper.Psi.CSharp.Tree;
using JetBrains.ReSharper.Resources.Resources.Icons;
using JetBrains.TextControl;
using JetBrains.Util;

namespace JetBrains.ReSharper.Plugins.Unity.CSharp.Daemon.Errors
{
    public partial class UnityGutterMarkInfo : ICustomAttributeIdHighlighting
    {
        public string AttributeId => UnityHighlightingAttributeIds.UNITY_GUTTER_ICON_ATTRIBUTE;

        // TODO: Move this somewhere better
        public IEnumerable<BulbMenuItem> GetBulbMenuItems(ISolution solution, ITextControl textControl)
        {
            var unityApi = solution.GetComponent<UnityApi>();

            if (Declaration is IClassLikeDeclaration classDeclaration)
            {
                var fix = new GenerateUnityEventFunctionsFix(classDeclaration);
                return new[]
                {
                    new BulbMenuItem(new IntentionAction.MyExecutableProxi(fix, solution, textControl),
                        "Generate Unity event functions", PsiFeaturesUnsortedThemedIcons.FuncZoneGenerate.Id,
                        BulbMenuAnchors.FirstClassContextItems)
                };
            }

            if (Declaration is IMethodDeclaration methodDeclaration)
            {
                var isCoroutine = IsCoroutine(methodDeclaration, unityApi);
                if (isCoroutine.HasValue)
                {
                    IBulbAction bulbAction;
                    if (isCoroutine.Value)
                        bulbAction = new ConvertFromCoroutineBulbAction(methodDeclaration);
                    else
                        bulbAction = new ConvertToCoroutineBulbAction(methodDeclaration);
                    return new[]
                    {
                        new BulbMenuItem(new IntentionAction.MyExecutableProxi(bulbAction, solution, textControl),
                            bulbAction.Text, BulbThemedIcons.ContextAction.Id, BulbMenuAnchors.FirstClassContextItems)
                    };
                }
            }

            return EmptyList<BulbMenuItem>.Enumerable;
        }

        private static bool? IsCoroutine(IMethodDeclaration methodDeclaration, UnityApi unityApi)
        {
            if (methodDeclaration == null) return null;
            if (!methodDeclaration.IsFromUnityProject()) return null;

            var method = methodDeclaration.DeclaredElement;
            if (method == null) return null;

            var function = unityApi.GetUnityEventFunction(method);
            if (function == null || !function.Coroutine) return null;

            var type = method.ReturnType.GetScalarType();
            if (type == null) return null;

            return Equals(type.GetClrName(), PredefinedType.IENUMERATOR_FQN);
        }
    }
}