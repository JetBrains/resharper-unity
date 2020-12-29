using System;
using System.Collections.Generic;
using JetBrains.Application.Progress;
using JetBrains.Application.Settings;
using JetBrains.Application.UI.Controls.BulbMenu.Items;
using JetBrains.Application.UI.Help;
using JetBrains.Application.UI.Icons.CommonThemedIcons;
using JetBrains.ProjectModel;
using JetBrains.ReSharper.Feature.Services.Bulbs;
using JetBrains.ReSharper.Feature.Services.Daemon;
using JetBrains.ReSharper.Feature.Services.Resources;
using JetBrains.ReSharper.Plugins.Unity.Application.UI.Help;
using JetBrains.ReSharper.Plugins.Unity.CSharp.Daemon.CallGraph;
using JetBrains.ReSharper.Plugins.Unity.CSharp.Daemon.Stages.ContextSystem;
using JetBrains.ReSharper.Plugins.Unity.CSharp.Daemon.Stages.PerformanceCriticalCodeAnalysis.ContextSystem;
using JetBrains.ReSharper.Plugins.Unity.CSharp.Feature.Services.Bulbs;
using JetBrains.ReSharper.Plugins.Unity.Settings;
using JetBrains.ReSharper.Psi;
using JetBrains.ReSharper.Psi.CSharp.Tree;
using JetBrains.ReSharper.Psi.Util;
using JetBrains.TextControl;
using JetBrains.Util;

namespace JetBrains.ReSharper.Plugins.Unity.CSharp.Daemon.Stages.Highlightings.IconsProviders
{
    [SolutionComponent]
    public class UnityCommonIconProvider
    {
        protected readonly IApplicationWideContextBoundSettingStore SettingsStore;
        protected readonly UnityApi UnityApi;
        protected readonly PerformanceCriticalContextProvider PerformanceContextProvider;
        private readonly ISolution mySolution;
        private readonly ITextControlManager myTextControlManager;
        private readonly IEnumerable<IPerformanceAnalysisCodeInsightMenuItemProvider> myMenuItemProviders;
       
        public UnityCommonIconProvider(ISolution solution, UnityApi unityApi,
                                       IApplicationWideContextBoundSettingStore settingsStore,
                                       PerformanceCriticalContextProvider performanceContextProvider,
                                       IEnumerable<IPerformanceAnalysisCodeInsightMenuItemProvider> menuItemProviders)
        {
            mySolution = solution;
            myTextControlManager = solution.GetComponent<ITextControlManager>();
            UnityApi = unityApi;
            SettingsStore = settingsStore;
            PerformanceContextProvider = performanceContextProvider;
            myMenuItemProviders = menuItemProviders;
        }

        public virtual void AddEventFunctionHighlighting(IHighlightingConsumer consumer, IMethod method,
            UnityEventFunction eventFunction, string text, IReadOnlyCallGraphContext context)
        {
            var tooltip = GetEventFunctionTooltip(eventFunction);
            foreach (var declaration in method.GetDeclarations())
            {
                if (declaration is ICSharpDeclaration cSharpDeclaration)
                {
                    consumer.AddImplicitConfigurableHighlighting(cSharpDeclaration);
                    consumer.AddHotHighlighting(PerformanceContextProvider, cSharpDeclaration,
                        SettingsStore.BoundSettingsStore, text, tooltip, context, GetEventFunctionActions(cSharpDeclaration, context));
                }
            }
        }

        public virtual void AddFrequentlyCalledMethodHighlighting(IHighlightingConsumer consumer,
            ICSharpDeclaration cSharpDeclaration,
            string text, string tooltip, IReadOnlyCallGraphContext context)
        {
            // gutter mark
            var actions = GetActions(cSharpDeclaration, context);
            
            consumer.AddHotHighlighting(PerformanceContextProvider, cSharpDeclaration,
                SettingsStore.BoundSettingsStore, text, tooltip, context, actions, true);
        }

        protected IReadOnlyList<BulbMenuItem> GetEventFunctionActions(ICSharpDeclaration declaration, IReadOnlyCallGraphContext context)
        {
            if (!(declaration is IMethodDeclaration methodDeclaration)) 
                return EmptyList<BulbMenuItem>.Instance;
            
            var declaredElement = methodDeclaration.DeclaredElement;
            var textControl = mySolution.GetComponent<ITextControlManager>().LastFocusedTextControl.Value;

            if (textControl == null || declaredElement == null) 
                return EmptyList<BulbMenuItem>.Instance;
            
            var isCoroutine = IsCoroutine(methodDeclaration, UnityApi);
            var result = GetBulbMenuItems(declaration, context) as List<BulbMenuItem> ?? new List<BulbMenuItem>();

            if (isCoroutine.HasValue)
            {
                var bulbAction = isCoroutine.Value
                    ? (IBulbAction) new ConvertFromCoroutineBulbAction(methodDeclaration)
                    : new ConvertToCoroutineBulbAction(methodDeclaration);

                var menuITem = UnityCallGraphUtil.BulbActionToMenuItem(bulbAction, textControl, mySolution, BulbThemedIcons.ContextAction.Id);
                        
                result.Add(menuITem);
            }

            if (UnityApi.IsEventFunction(declaredElement))
            {
                var showUnityHelp = mySolution.GetComponent<ShowUnityHelp>();
                var documentationNavigationAction = new DocumentationNavigationAction(showUnityHelp, declaredElement, UnityApi);
                var menuItem = UnityCallGraphUtil.BulbActionToMenuItem(documentationNavigationAction, textControl, mySolution, CommonThemedIcons.Question.Id);
                
                result.Add(menuItem);
            }

            return result;
        }

        protected virtual string GetEventFunctionTooltip(UnityEventFunction eventFunction)
        {
            var tooltip = "Unity event function";
            if (!string.IsNullOrEmpty(eventFunction.Description))
                tooltip += Environment.NewLine + Environment.NewLine + eventFunction.Description;
            if (eventFunction.CanBeCoroutine)
                tooltip += Environment.NewLine + "This function can be a coroutine.";

            return tooltip;
        }

        private class DocumentationNavigationAction : BulbActionBase
        {
            private readonly ShowUnityHelp myShowUnityHelp;
            private readonly IMethod myMethod;
            private readonly UnityApi myUnityApi;

            public DocumentationNavigationAction(ShowUnityHelp showUnityHelp, IMethod method, UnityApi unityApi)
            {
                myShowUnityHelp = showUnityHelp;
                myMethod = method;
                myUnityApi = unityApi;
            }

            protected override Action<ITextControl> ExecutePsiTransaction(ISolution solution,
                IProgressIndicator progress)
            {
                myShowUnityHelp.ShowHelp(myMethod.GetUnityEventFunctionName(myUnityApi), HelpSystem.HelpKind.Msdn);
                return null;
            }

            public override string Text => "View documentation";
        }

        protected static bool? IsCoroutine(IMethodDeclaration methodDeclaration, UnityApi unityApi)
        {
            if (methodDeclaration == null) return null;
            if (!methodDeclaration.IsFromUnityProject()) return null;

            var method = methodDeclaration.DeclaredElement;
            if (method == null) return null;

            var function = unityApi.GetUnityEventFunction(method);
            if (function == null || !function.CanBeCoroutine) return null;

            var type = method.ReturnType.GetScalarType();
            if (type == null) return false;

            return Equals(type.GetClrName(), PredefinedType.IENUMERATOR_FQN);
        }
        
        protected IReadOnlyList<BulbMenuItem> GetActions(ICSharpDeclaration declaration, IReadOnlyCallGraphContext context)
        {
            if (declaration.DeclaredElement is IMethod method && UnityApi.IsEventFunction(method))
                return GetEventFunctionActions(declaration, context);

            return GetBulbMenuItems(declaration, context);
        }

        protected IReadOnlyList<BulbMenuItem> GetBulbMenuItems(ICSharpDeclaration declaration, IReadOnlyCallGraphContext context)
        {
            if (!(declaration is IMethodDeclaration methodDeclaration))
                return EmptyList<BulbMenuItem>.Instance;

            var iconsEnabled = context.DaemonProcess.ContextBoundSettingsStore.GetValue((UnitySettings key) => key.EnableIconsForPerformanceCriticalCode);
                    
            if (!iconsEnabled)
                return EmptyList<BulbMenuItem>.Instance;
            
            var textControl = myTextControlManager.LastFocusedTextControl.Value;
            var result = new List<BulbMenuItem>();

            foreach (var provider in myMenuItemProviders)
            {
                var menuItems = provider.GetMenuItems(methodDeclaration, textControl, context);

                foreach (var item in menuItems)
                    result.Add(item);
            }

            return result;
        }
    }
}