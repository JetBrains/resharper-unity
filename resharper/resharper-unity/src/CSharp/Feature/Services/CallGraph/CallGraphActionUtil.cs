using System;
using System.Collections.Generic;
using System.Linq;
using JetBrains.Application.Threading;
using JetBrains.Application.UI.Controls.BulbMenu.Items;
using JetBrains.ProjectModel;
using JetBrains.ReSharper.Feature.Services.CallHierarchy.FindResults;
using JetBrains.ReSharper.Plugins.Unity.CSharp.Daemon.CallGraph;
using JetBrains.ReSharper.Plugins.Unity.CSharp.Daemon.Stages.ContextSystem;
using JetBrains.ReSharper.Psi.CSharp.Tree;
using JetBrains.TextControl;

namespace JetBrains.ReSharper.Plugins.Unity.CSharp.Feature.Services.CallGraph
{
    public static class CallGraphActionUtil
    {
        public static IEnumerable<BulbMenuItem> ToMenuItems(this IEnumerable<ShowCallsBulbActionBase> bulbs, ITextControl textControl,
            ISolution solution)
        {
            return bulbs.Select(bulb => UnityCallGraphUtil.BulbActionToMenuItem(bulb, textControl, solution, bulb.Icon));
        }

        public static Func<CallHierarchyFindResult, bool> GetSimpleFilter(
            ISolution solution,
            ICallGraphContextProvider provider,
            ShowCallsType type)
        {
            switch (type)
            {
                case ShowCallsType.INCOMING:
                {
                    return result =>
                    {
                        solution.Locks.AssertReadAccessAllowed();

                        var referenceElement = result.ReferenceElement;
                        var csharpTreeNode = referenceElement as ICSharpTreeNode;
                        var containing = csharpTreeNode?.GetContainingFunctionLikeDeclarationOrClosure();
                        var declaredElement = containing?.DeclaredElement;

                        return provider.IsMarkedGlobal(declaredElement);
                    };
                }
                case ShowCallsType.OUTGOING:
                {
                    return result =>
                    {
                        solution.Locks.AssertReadAccessAllowed();

                        var referenceElement = result.ReferenceElement;
                        var identifier = referenceElement as ICSharpIdentifier;
                        var referenceExpression = ReferenceExpressionNavigator.GetByNameIdentifier(identifier);
                        var declaredElement = referenceExpression?.Reference.Resolve().DeclaredElement;

                        return provider.IsMarkedGlobal(declaredElement);
                    };
                }
                default:
                    throw new ArgumentOutOfRangeException(nameof(type), type, null);
            }
        }
    }
}