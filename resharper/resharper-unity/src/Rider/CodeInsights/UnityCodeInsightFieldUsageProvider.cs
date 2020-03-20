using System;
using System.Collections.Generic;
using System.Linq;
using JetBrains.Application.DataContext;
using JetBrains.Application.Threading;
using JetBrains.Application.UI.Actions.ActionManager;
using JetBrains.Application.UI.ActionsRevised.Handlers;
using JetBrains.Application.UI.Controls;
using JetBrains.Application.UI.Controls.BulbMenu.Items;
using JetBrains.Application.UI.Controls.GotoByName;
using JetBrains.Application.UI.Controls.JetPopupMenu;
using JetBrains.Application.UI.DataContext;
using JetBrains.Application.UI.PopupLayout;
using JetBrains.Application.UI.Tooltips;
using JetBrains.Diagnostics;
using JetBrains.DocumentModel;
using JetBrains.DocumentModel.DataContext;
using JetBrains.Lifetimes;
using JetBrains.ProjectModel;
using JetBrains.ProjectModel.DataContext;
using JetBrains.ReSharper.Daemon.CodeInsights;
using JetBrains.ReSharper.Feature.Services.Daemon;
using JetBrains.ReSharper.Feature.Services.Navigation.Settings;
using JetBrains.ReSharper.Host.Features.Services;
using JetBrains.ReSharper.Plugins.Unity.CSharp.Feature.Services.Navigation.GoToUnityUsages;
using JetBrains.ReSharper.Plugins.Unity.Feature.Caches;
using JetBrains.ReSharper.Plugins.Unity.ProjectModel;
using JetBrains.ReSharper.Plugins.Unity.Yaml.Psi.Caches;
using JetBrains.ReSharper.Plugins.Unity.Yaml.Psi.DeferredCaches;
using JetBrains.ReSharper.Plugins.Unity.Yaml.Psi.DeferredCaches.AssetHierarchy.References;
using JetBrains.ReSharper.Plugins.Unity.Yaml.Psi.DeferredCaches.AssetInspectorValues;
using JetBrains.ReSharper.Plugins.Unity.Yaml.Psi.DeferredCaches.AssetInspectorValues.Values;
using JetBrains.ReSharper.Psi;
using JetBrains.ReSharper.Psi.CSharp.Tree;
using JetBrains.ReSharper.Psi.DataContext;
using JetBrains.ReSharper.Psi.Tree;
using JetBrains.ReSharper.Psi.Util;
using JetBrains.ReSharper.Resources.Shell;
using JetBrains.Rider.Model;

namespace JetBrains.ReSharper.Plugins.Unity.Rider.CodeInsights
{
    [SolutionComponent]
    public class UnityCodeInsightFieldUsageProvider : AbstractUnityCodeInsightProvider
    {
        private readonly UnityApi myUnityApi;
        private readonly DeferredCacheController myDeferredCacheController;
        private readonly AssetInspectorValuesContainer myInspectorValuesContainer;
        private readonly DataContexts myContexts;
        private readonly IActionManager myActionManager;
        public override string ProviderId => "Unity serialized field";
        public override string DisplayName => "Unity serialized field";
        public override CodeLensAnchorKind DefaultAnchor => CodeLensAnchorKind.Right;

        public override ICollection<CodeLensRelativeOrdering> RelativeOrderings =>
            new[] {new CodeLensRelativeOrderingLast()};

        public UnityCodeInsightFieldUsageProvider(UnitySolutionTracker unitySolutionTracker,
            UnityApi unityApi, UnityHost host, BulbMenuComponent bulbMenu, DeferredCacheController deferredCacheController,
            AssetInspectorValuesContainer inspectorValuesContainer)
            : base(unitySolutionTracker, host, bulbMenu)
        {
            myUnityApi = unityApi;
            myDeferredCacheController = deferredCacheController;
            myInspectorValuesContainer = inspectorValuesContainer;
            myActionManager = Shell.Instance.GetComponent<IActionManager>();
            myContexts =  Shell.Instance.GetComponent<DataContexts>();
        }
        
        private static (string guid, string[] propertyNames) GetAssetGuidAndPropertyName(ISolution solution, IField declaredElement)
        {
            Assertion.Assert(solution.Locks.IsReadAccessAllowed(), "ReadLock required");
            
            var containingType = declaredElement.GetContainingType();
            if (containingType == null)
                return (null, Array.Empty<string>());

            var guid = AssetUtils.GetGuidFor(solution.GetComponent<MetaFileGuidCache>(), containingType);
            return (guid, AssetUtils.GetAllNamesFor(declaredElement).ToArray());
        }
        
        public override void OnClick(CodeInsightsHighlighting highlighting, ISolution solution)
        {
            if (!(highlighting is UnityInspectorCodeInsightsHighlighting))
                return;
            
            var rules = new List<IDataRule>();
            rules.AddRule("Solution", ProjectModelDataConstants.SOLUTION, solution);      
      
            var declaredElement = highlighting.DeclaredElement;
            rules.AddRule("DeclaredElement", PsiDataConstants.DECLARED_ELEMENTS_FROM_ALL_CONTEXTS, new[] {  declaredElement });

            using (ReadLockCookie.Create())
            {               
                if (!declaredElement.IsValid())
                    return;
        
                rules.AddRule("DocumentEditorContext", DocumentModelDataConstants.EDITOR_CONTEXT, new DocumentEditorContext(highlighting.Range));
                rules.AddRule("PopupWindowSourceOverride", UIDataConstants.PopupWindowContextSource,
                    new PopupWindowContextSource(lt => new RiderEditorOffsetPopupWindowContext(highlighting.Range.StartOffset.Offset)));

                rules.AddRule("DontNavigateImmediatelyToSingleUsage", NavigationSettings.DONT_NAVIGATE_IMMEDIATELY_TO_SINGLE_USAGE, new object());
      
                var ctx = myContexts.CreateWithDataRules(Lifetime.Eternal, rules);
        
                var def = myActionManager.Defs.GetActionDef<GoToUnityUsagesAction>();
                def.EvaluateAndExecute(myActionManager, ctx);
            }
        }

        public void AddInspectorHighlighting(IHighlightingConsumer consumer, ICSharpDeclaration element,
            IDeclaredElement declaredElement, string baseDisplayName, string baseTooltip, string moreText, IconModel iconModel,
            IEnumerable<BulbMenuItem> items, List<CodeLensEntryExtraActionModel> extraActions)
        {
            string displayName = null;
            string tooltip = "Values from Unity Editor Inspector";

            var solution = element.GetSolution();
            Assertion.Assert(solution.Locks.IsReadAccessAllowed(), "ReadLock required");

            var field = (declaredElement as IField).NotNull();
            var type = field.Type;
            var containingType = field.GetContainingType();
            if (containingType == null)
            {
                base.AddHighlighting(consumer, element, field, baseDisplayName, baseTooltip, moreText, iconModel, items, extraActions);
                return;
            }
            
            var (guid, propertyNames) = GetAssetGuidAndPropertyName(solution, field);
            if (guid == null || propertyNames.Length == 0)
            {
                base.AddHighlighting(consumer, element, field, baseDisplayName, baseTooltip, moreText, iconModel, items, extraActions);
                return;
            }

            var presentationType = GetUnityPresentationType(type);

            if (!myDeferredCacheController.CompletedOnce.Value || ShouldShowUnknownPresentation(presentationType))
            {
                base.AddHighlighting(consumer, element, field, baseDisplayName, baseTooltip, moreText, iconModel, items, extraActions);
                return;
            }

            var initializer = (element as IFieldDeclaration).NotNull("element as IFieldDeclaration != null").Initial;
            var initValue = (initializer as IExpressionInitializer)?.Value?.ConstantValue.Value;

            var initValueUnityPresentation = GetUnitySerializedPresentation(presentationType, initValue);
            
            if (myInspectorValuesContainer.IsIndexResultEstimated(guid, containingType, propertyNames))
            {
                var count = myInspectorValuesContainer.GetAffectedFiles(guid, propertyNames) -  myInspectorValuesContainer.GetAffectedFilesWithSpecificValue(guid, propertyNames, initValueUnityPresentation);
                displayName = $"{count}+ changes";
            }
            else
            {
                var initValueCount =
                    myInspectorValuesContainer.GetValueCount(guid, propertyNames, initValueUnityPresentation);

                if (initValueCount == 0 && myInspectorValuesContainer.GetUniqueValuesCount(guid, propertyNames) == 1) // only modified value
                {
                    var value  = myInspectorValuesContainer.GetUniqueValueDifferTo(guid, propertyNames, null);
                    displayName = value.GetPresentation(solution, field, false);
                }
                else if (initValueCount > 0 && myInspectorValuesContainer.GetUniqueValuesCount(guid, propertyNames) == 2)
                {

                    // original value & only one modified value
                    var anotherValueWithLocation = myInspectorValuesContainer.GetUniqueValueDifferTo(guid, propertyNames, initValueUnityPresentation);
                    displayName = anotherValueWithLocation.GetPresentation(solution, field, false);
                }

                if (displayName == null || displayName.Equals("..."))
                {
                    var count = myInspectorValuesContainer.GetAffectedFiles(guid, propertyNames) -
                                myInspectorValuesContainer.GetAffectedFilesWithSpecificValue(guid, propertyNames,
                                    initValueUnityPresentation);
                    if (count == 0)
                    {
                        displayName = "Unchanged";
                    }
                    else
                    {
                        var word = count == 1 ? "asset" : "assets";
                        displayName = $"Changed in {count} {word}";
                    }
                }
            }

            consumer.AddHighlighting(new UnityInspectorCodeInsightsHighlighting(element.GetNameDocumentRange(),
                displayName, tooltip, "Property Inspector values", this,
                declaredElement, iconModel, presentationType));
        }
        
        private IAssetValue GetUnitySerializedPresentation(UnityPresentationType presentationType, object value)
        {
            if (presentationType == UnityPresentationType.Bool && value is bool b)
                return b ? new AssetSimpleValue("1") : new AssetSimpleValue("0");

            if (presentationType == UnityPresentationType.ScriptableObject && value == null)
                return new AssetReferenceValue(new LocalReference(0, 0));
            
            if (presentationType == UnityPresentationType.FileId && value == null)
                return new AssetReferenceValue(new LocalReference(0, 0));

            if ((presentationType == UnityPresentationType.OtherSimple  || presentationType == UnityPresentationType.Bool) && value == null)
                return new AssetSimpleValue("0");
            
            if (value == null)
                return new AssetSimpleValue(string.Empty);

            return new AssetSimpleValue(value.ToString());
        }
        
        private UnityPresentationType GetUnityPresentationType(IType type)
        {
            if (UnityApi.IsDescendantOfScriptableObject(type.GetTypeElement()))
                return UnityPresentationType.ScriptableObject;
            if (type.IsBool())
                return UnityPresentationType.Bool;
            if (type.IsEnumType())
                return UnityPresentationType.Enum;
            if (type.IsString())
                return UnityPresentationType.String;

            if (type.IsSimplePredefined())
                return UnityPresentationType.OtherSimple;
            
            if (type.IsValueType())
                return UnityPresentationType.ValueType;

            if (IsSerializedViaFileId(type))
                return UnityPresentationType.FileId;

            return UnityPresentationType.Other;
        }

        private bool IsSerializedViaFileId(IType type)
        {
            var typeElement = type.GetTypeElement();
            if (typeElement == null)
                return false;

            return UnityApi.IsDescendantOf(KnownTypes.GameObject, typeElement) ||
                   UnityApi.IsDescendantOf(KnownTypes.Component, typeElement);
        }
        
        private bool ShouldShowUnknownPresentation(UnityPresentationType presentationType)
        {
            return presentationType == UnityPresentationType.Other ||
                   presentationType == UnityPresentationType.ValueType;
        }
        
        public enum UnityPresentationType
        {
            Enum,
            Bool,
            String,
            OtherSimple,
            FileId,
            ValueType,
            Other,
            ScriptableObject
        }
    }
}