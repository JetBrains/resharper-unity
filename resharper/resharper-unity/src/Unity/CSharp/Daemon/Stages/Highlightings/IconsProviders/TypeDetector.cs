using System.Collections.Generic;
using JetBrains.Application.BuildScript.Application.Zones;
using JetBrains.Application.Parts;
using JetBrains.Application.UI.Controls.BulbMenu.Anchors;
using JetBrains.Application.UI.Controls.BulbMenu.Items;
using JetBrains.ProjectModel;
using JetBrains.ReSharper.Feature.Services;
using JetBrains.ReSharper.Feature.Services.Bulbs;
using JetBrains.ReSharper.Feature.Services.Daemon;
using JetBrains.ReSharper.Feature.Services.Intentions;
using JetBrains.ReSharper.Plugins.Unity.CSharp.Daemon.Errors;
using JetBrains.ReSharper.Plugins.Unity.CSharp.Daemon.Stages.ContextSystem;
using JetBrains.ReSharper.Plugins.Unity.CSharp.Daemon.Stages.Dots;
using JetBrains.ReSharper.Plugins.Unity.CSharp.Daemon.Stages.PerformanceCriticalCodeAnalysis.ContextSystem;
using JetBrains.ReSharper.Plugins.Unity.CSharp.Feature.Services.Generate.Dots;
using JetBrains.ReSharper.Plugins.Unity.CSharp.Feature.Services.QuickFixes;
using JetBrains.ReSharper.Plugins.Unity.Resources;
using JetBrains.ReSharper.Plugins.Unity.UnityEditorIntegration.Api;
using JetBrains.ReSharper.Plugins.Unity.UnityEditorIntegration.Packages;
using JetBrains.ReSharper.Plugins.Unity.Utils;
using JetBrains.ReSharper.Psi;
using JetBrains.ReSharper.Psi.CSharp.Tree;
using JetBrains.ReSharper.Psi.Tree;
using JetBrains.ReSharper.Psi.Util;
using JetBrains.ReSharper.Resources.Resources.Icons;
using JetBrains.TextControl;
using JetBrains.TextControl.CodeWithMe;

#nullable enable

namespace JetBrains.ReSharper.Plugins.Unity.CSharp.Daemon.Stages.Highlightings.IconsProviders
{
    [SolutionComponent(Instantiation.DemandAnyThread)]
    [ZoneMarker(typeof(ICodeEditingZone))]
    public class TypeDetector : UnityDeclarationHighlightingProviderBase
    {
        private readonly UnityApi myUnityApi;

        public TypeDetector(ISolution solution, IApplicationWideContextBoundSettingStore settingsStore, UnityApi unityApi, PerformanceCriticalContextProvider contextProvider)
            : base(solution, settingsStore, contextProvider)
        {
            myUnityApi = unityApi;
        }

        public override bool AddDeclarationHighlighting(IDeclaration node, IHighlightingConsumer consumer, IReadOnlyCallGraphContext context)
        {
            if (!(node is IClassLikeDeclaration element))
                return false;

            var typeElement = element.DeclaredElement;
            if (typeElement != null)
            {
                if (typeElement.DerivesFromMonoBehaviour())
                {
                    AddMonoBehaviourHighlighting(consumer, element, Strings.TypeDetector_AddDeclarationHighlighting_Script, Strings.TypeDetector_AddDeclarationHighlighting_Unity_script, context);
                }
                else if (typeElement.DerivesFrom(KnownTypes.Editor) || typeElement.DerivesFrom(KnownTypes.EditorWindow))
                {
                    AddEditorHighlighting(consumer, element, Strings.TypeDetector_AddDeclarationHighlighting_Editor, Strings.TypeDetector_AddDeclarationHighlighting_Custom_Unity_editor, context);
                }
                else if (typeElement.DerivesFromScriptableObject())
                {
                    AddMonoBehaviourHighlighting(consumer, element, Strings.TypeDetector_AddDeclarationHighlighting_Scriptable_object, Strings.TypeDetector_AddDeclarationHighlighting_Unity_scriptable_object, context);
                }
                else if (myUnityApi.IsUnityType(typeElement))
                {
                    AddUnityTypeHighlighting(consumer, element, Strings.TypeDetector_AddDeclarationHighlighting_Unity_type, Strings.TypeDetector_AddDeclarationHighlighting_Custom_Unity_type, context);
                }
                else if (typeElement.IsDotsImplicitlyUsedType())
                {
                    var tooltip = string.Format(Strings.TypeDetector_AddDeclarationHighlighting_Unity_entities,
                        typeElement.GetDotsCLRBaseTypeName()!.ShortName);
                    AddUnityDOTSHighlighting(consumer, element, 
                        Strings.TypeDetector_AddDeclarationHighlighting_DOTS,
                        tooltip,
                        context);
                }

                return true;
            }

            return false;
        }

        protected virtual void AddMonoBehaviourHighlighting(IHighlightingConsumer consumer, IClassLikeDeclaration declaration, string text, string tooltip, IReadOnlyCallGraphContext context)
        {
            AddHighlighting(consumer, declaration, text, tooltip, context);
        }

        protected virtual void AddEditorHighlighting(IHighlightingConsumer consumer, IClassLikeDeclaration declaration, string text, string tooltip, IReadOnlyCallGraphContext context)
        {
            AddHighlighting(consumer, declaration, text, tooltip, context);
        }

        protected virtual void AddUnityTypeHighlighting(IHighlightingConsumer consumer, IClassLikeDeclaration declaration, string text, string tooltip, IReadOnlyCallGraphContext context)
        {
            AddHighlighting(consumer, declaration, text, tooltip, context);
        }

        protected virtual void AddUnityDOTSHighlighting(IHighlightingConsumer consumer, IClassLikeDeclaration declaration, string text, string tooltip, IReadOnlyCallGraphContext context)
        {
            AddHighlighting(consumer, declaration, text, tooltip, context);
        }


        protected override void AddHighlighting(IHighlightingConsumer consumer, ICSharpDeclaration declaration, string text, string tooltip, IReadOnlyCallGraphContext context)
        {
            consumer.AddImplicitConfigurableHighlighting(declaration);
            
            if (!IconProviderUtil.ShouldShowGutterMarkIcon(SettingsStore.BoundSettingsStore))
                return;
            
            consumer.AddHighlighting(new UnityGutterMarkInfo(GetActions(declaration), declaration, tooltip));
        }

        protected override IEnumerable<BulbMenuItem> GetActions(ICSharpDeclaration declaration)
        {
            var result = new List<BulbMenuItem>();
            var textControl = Solution.GetComponent<ITextControlManager>().LastFocusedTextControlPerClient
                .ForCurrentClient();
            if (declaration is IClassLikeDeclaration classLikeDeclaration && textControl != null)
            {
                var declaredElement = classLikeDeclaration.DeclaredElement;
                if (myUnityApi.IsUnityType(declaredElement))
                {
                    var fix = new GenerateUnityEventFunctionsFix(classLikeDeclaration);
                    result.Add(new IntentionAction(fix, Strings.TypeDetector_GetActions_Generate_Unity_event_functions,
                            PsiFeaturesUnsortedThemedIcons.FuncZoneGenerate.Id, BulbMenuAnchors.FirstClassContextItems)
                        .ToBulbMenuItem(Solution, textControl));
                }

                if (classLikeDeclaration.GetContainingNode<IMethodDeclaration>() == null &&
                    classLikeDeclaration.GetContainingNode<IPropertyDeclaration>() == null
                   )
                {
                    IBulbAction? fix = null;
                    var title = string.Empty;

                    if (declaredElement.DerivesFrom(KnownTypes.IComponentData))
                    {
                        fix = new GenerateBakerAndAuthoringActionFix(classLikeDeclaration);
                        title = Strings.UnityDots_GenerateBakerAndAuthoring_Unity_Component_Fields_WindowTitle;
                    }
                    else if (declaredElement.DerivesFrom(KnownTypes.Component))
                    {
                        if (Solution.HasEntitiesPackage())
                        {
                            fix = new GenerateBakerAndComponentActionFix(classLikeDeclaration);
                            title = Strings.UnityDots_GenerateBakerAndComponent_Unity_MonoBehaviour_Fields_WindowTitle;
                        }
                    }
                    
                    if(fix != null)
                        result.Add(new IntentionAction(fix, title, PsiFeaturesUnsortedThemedIcons.FuncZoneGenerate.Id, BulbMenuAnchors.FirstClassContextItems).ToBulbMenuItem(Solution, textControl));
                }
                
                if (classLikeDeclaration.IsPartial
                    && declaredElement.IsDotsImplicitlyUsedType()
                    && !classLikeDeclaration.GetSourceFile().IsSourceGeneratedFile()
                    && declaredElement.GetDeclarations().Count > 1)
                {
                    var bulbAction = new OpenDotsSourceGeneratedFileBulbAction(Strings.UnityDots_PartialClassesGeneratedCode_ShowGeneratedCode, classLikeDeclaration);
                    result.Add(new IntentionAction(bulbAction,
                        Strings.UnityDots_PartialClassesGeneratedCode_ShowGeneratedCode, PsiFeaturesUnsortedThemedIcons.Navigate.Id,
                        BulbMenuAnchors.FirstClassContextItems).ToBulbMenuItem(Solution, textControl));
                }
            }

            return result;
        }
    }
}
