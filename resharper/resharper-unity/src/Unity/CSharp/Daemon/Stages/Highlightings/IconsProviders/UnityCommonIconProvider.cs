using System;
using System.Collections.Generic;
using JetBrains.Application.Components;
using JetBrains.Application.Parts;
using JetBrains.Application.Progress;
using JetBrains.Application.UI.Controls.BulbMenu.Items;
using JetBrains.Application.UI.Icons.CommonThemedIcons;
using JetBrains.ProjectModel;
using JetBrains.ReSharper.Feature.Services.Bulbs;
using JetBrains.ReSharper.Feature.Services.Daemon;
using JetBrains.ReSharper.Feature.Services.Resources;
using JetBrains.ReSharper.Plugins.Unity.Core.Application.Settings;
using JetBrains.ReSharper.Plugins.Unity.Core.Application.UI.Help;
using JetBrains.ReSharper.Plugins.Unity.CSharp.Daemon.CallGraph;
using JetBrains.ReSharper.Plugins.Unity.CSharp.Daemon.Stages.ContextSystem;
using JetBrains.ReSharper.Plugins.Unity.CSharp.Daemon.Stages.PerformanceCriticalCodeAnalysis.ContextSystem;
using JetBrains.ReSharper.Plugins.Unity.CSharp.Feature.Services.Bulbs;
using JetBrains.ReSharper.Plugins.Unity.UnityEditorIntegration.Api;
using JetBrains.ReSharper.Plugins.Unity.Utils;
using JetBrains.ReSharper.Psi;
using JetBrains.ReSharper.Psi.CSharp.Tree;
using JetBrains.ReSharper.Psi.Util;
using JetBrains.TextControl;
using JetBrains.TextControl.CodeWithMe;
using JetBrains.Util;
using Strings = JetBrains.ReSharper.Plugins.Unity.Resources.Strings;

#nullable enable

namespace JetBrains.ReSharper.Plugins.Unity.CSharp.Daemon.Stages.Highlightings.IconsProviders
{
    [SolutionComponent(Instantiation.DemandAnyThreadSafe)]
    public class UnityCommonIconProvider
    {
        protected readonly IApplicationWideContextBoundSettingStore SettingsStore;
        protected readonly PerformanceCriticalContextProvider PerformanceContextProvider;
        private readonly UnityApi myUnityApi;
        private readonly ISolution mySolution;
        private readonly ITextControlManager myTextControlManager;
        private readonly IImmutableEnumerable<IPerformanceAnalysisBulbItemsProvider> myMenuItemProviders;

        public UnityCommonIconProvider(ISolution solution, UnityApi unityApi,
                                       IApplicationWideContextBoundSettingStore settingsStore,
                                       PerformanceCriticalContextProvider performanceContextProvider,
                                       IImmutableEnumerable<IPerformanceAnalysisBulbItemsProvider> menuItemProviders,
                                       ITextControlManager textControlManager)
        {
            mySolution = solution;
            myTextControlManager = textControlManager;
            myUnityApi = unityApi;
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
                    
                    if (!IconProviderUtil.ShouldShowGutterMarkIcon(SettingsStore.BoundSettingsStore))
                        continue;

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

            if (!IconProviderUtil.ShouldShowGutterMarkIcon(SettingsStore.BoundSettingsStore))
                return;
            
            consumer.AddHotHighlighting(PerformanceContextProvider, cSharpDeclaration,
                SettingsStore.BoundSettingsStore, text, tooltip, context, actions, true);
        }

        protected IReadOnlyList<BulbMenuItem> GetEventFunctionActions(ICSharpDeclaration declaration, IReadOnlyCallGraphContext context)
        {
            if (!(declaration is IMethodDeclaration methodDeclaration))
                return EmptyList<BulbMenuItem>.Instance;

            var declaredElement = methodDeclaration.DeclaredElement;
            var textControl = mySolution.GetComponent<ITextControlManager>().LastFocusedTextControlPerClient.ForCurrentClient();
            if (textControl == null || declaredElement == null)
                return EmptyList<BulbMenuItem>.Instance;

            var isCoroutine = IsCoroutine(methodDeclaration, myUnityApi);
            var result = GetBulbMenuItems(declaration, context) as List<BulbMenuItem> ?? new List<BulbMenuItem>();

            if (isCoroutine.HasValue)
            {
                var bulbAction = isCoroutine.Value
                    ? (IBulbAction) new ConvertFromCoroutineBulbAction(methodDeclaration)
                    : new ConvertToCoroutineBulbAction(methodDeclaration);

                var menuITem = UnityCallGraphUtil.BulbActionToMenuItem(bulbAction, textControl, mySolution, BulbThemedIcons.ContextAction.Id);

                result.Add(menuITem);
            }

            if (myUnityApi.IsEventFunction(declaredElement))
            {
                var showUnityHelp = mySolution.GetComponent<ShowUnityHelp>();
                var documentationNavigationAction = new DocumentationNavigationAction(showUnityHelp, declaredElement, myUnityApi);
                var menuItem = UnityCallGraphUtil.BulbActionToMenuItem(documentationNavigationAction, textControl, mySolution, CommonThemedIcons.Question.Id);

                result.Add(menuItem);
            }

            return result;
        }

        protected virtual string GetEventFunctionTooltip(UnityEventFunction eventFunction)
        {
            var tooltip = Strings.UnityCommonIconProvider_GetEventFunctionTooltip_Unity_event_function;
            if (!string.IsNullOrEmpty(eventFunction.Description))
                tooltip += Environment.NewLine + Environment.NewLine + eventFunction.Description;
            if (eventFunction.CanBeCoroutine)
                tooltip += Environment.NewLine + Strings.UnityCommonIconProvider_GetEventFunctionTooltip_This_function_can_be_a_coroutine_;

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

            protected override Action<ITextControl>? ExecutePsiTransaction(ISolution solution,
                                                                           IProgressIndicator progress)
            {
                myShowUnityHelp.ShowMsdnHelp(myMethod.GetUnityEventFunctionName(myUnityApi));
                return null;
            }

            public override string Text => Strings.DocumentationNavigationAction_Text_View_documentation;
        }

        private static bool? IsCoroutine(IMethodDeclaration methodDeclaration, UnityApi unityApi)
        {
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
            if (declaration.DeclaredElement is IMethod method && myUnityApi.IsEventFunction(method))
                return GetEventFunctionActions(declaration, context);

            return GetBulbMenuItems(declaration, context);
        }

        private IReadOnlyList<BulbMenuItem> GetBulbMenuItems(ICSharpDeclaration declaration, IReadOnlyCallGraphContext context)
        {
            if (!(declaration is IMethodDeclaration methodDeclaration))
                return EmptyList<BulbMenuItem>.Instance;

            var iconsEnabled = context.DaemonProcess.ContextBoundSettingsStore.GetValue((UnitySettings key) => key.EnableIconsForPerformanceCriticalCode);
            if (!iconsEnabled)
                return EmptyList<BulbMenuItem>.Instance;

            var textControl = myTextControlManager.LastFocusedTextControlPerClient.ForCurrentClient();
            if (textControl == null)
                return EmptyList<BulbMenuItem>.Instance;

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
