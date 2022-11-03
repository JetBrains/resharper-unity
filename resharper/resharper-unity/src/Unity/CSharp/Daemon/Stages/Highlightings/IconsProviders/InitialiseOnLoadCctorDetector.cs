using System.Collections.Generic;
using JetBrains.Application.UI.Controls.BulbMenu.Items;
using JetBrains.ProjectModel;
using JetBrains.ReSharper.Feature.Services.Daemon;
using JetBrains.ReSharper.Plugins.Unity.CSharp.Daemon.Stages.ContextSystem;
using JetBrains.ReSharper.Plugins.Unity.CSharp.Daemon.Stages.PerformanceCriticalCodeAnalysis.ContextSystem;
using JetBrains.ReSharper.Plugins.Unity.Resources;
using JetBrains.ReSharper.Plugins.Unity.UnityEditorIntegration.Api;
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
                                             PerformanceCriticalContextProvider contextProvider)
            : base(solution, settingsStore, contextProvider)
        {
        }

        public override bool AddDeclarationHighlighting(IDeclaration node, IHighlightingConsumer consumer, IReadOnlyCallGraphContext context)
        {
            if (!(node is IConstructorDeclaration element))
                return false;

            if (!element.IsStatic)
                return false;

            var containingType = element.GetContainingTypeDeclaration()?.DeclaredElement;
            if (containingType != null &&
                containingType.HasAttributeInstance(KnownTypes.InitializeOnLoadAttribute, false))
            {
                AddHighlighting(consumer, element, Strings.InitialiseOnLoadCctorDetector_AddDeclarationHighlighting_Text,
                    Strings.InitialiseOnLoadCctorDetector_AddDeclarationHighlighting_Tooltip, context);
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