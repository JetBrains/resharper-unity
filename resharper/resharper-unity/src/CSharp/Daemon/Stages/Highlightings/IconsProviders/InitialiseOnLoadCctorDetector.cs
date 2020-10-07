using System.Collections.Generic;
using JetBrains.Application.UI.Controls.BulbMenu.Items;
using JetBrains.ProjectModel;
using JetBrains.ReSharper.Feature.Services.Daemon;
using JetBrains.ReSharper.Plugins.Unity.CSharp.Daemon.Stages.ContextSystem;
using JetBrains.ReSharper.Psi;
using JetBrains.ReSharper.Psi.CSharp.Tree;
using JetBrains.ReSharper.Psi.Tree;
using JetBrains.ReSharper.Psi.Util;
using JetBrains.Util.Collections;

namespace JetBrains.ReSharper.Plugins.Unity.CSharp.Daemon.Stages.Highlightings.IconsProviders
{
    [SolutionComponent]
    public class InitialiseOnLoadCctorDetector : UnityDeclarationHighlightingProviderBase
    {
        public InitialiseOnLoadCctorDetector(ISolution solution,
                                             IApplicationWideContextBoundSettingStore settingsStore,
                                             UnityProblemAnalyzerContextSystem contextSystem)
            : base(solution, settingsStore, contextSystem)
        {
        }

        public override bool AddDeclarationHighlighting(IDeclaration node, IHighlightingConsumer consumer, DaemonProcessKind kind)
        {
            if (!(node is IConstructorDeclaration element))
                return false;

            if (!element.IsStatic)
                return false;

            var containingType = element.GetContainingTypeDeclaration()?.DeclaredElement;
            if (containingType != null &&
                containingType.HasAttributeInstance(KnownTypes.InitializeOnLoadAttribute, false))
            {
                AddHighlighting(consumer, element, "Used implicitly",
                    "Called when Unity first launches the editor, the player, or recompiles scripts", kind);
                return true;
            }

            return false;
        }

        protected override IEnumerable<BulbMenuItem> GetActions(ICSharpDeclaration declaration)
        {
            return EnumerableCollection<BulbMenuItem>.Empty;
        }
    }
}