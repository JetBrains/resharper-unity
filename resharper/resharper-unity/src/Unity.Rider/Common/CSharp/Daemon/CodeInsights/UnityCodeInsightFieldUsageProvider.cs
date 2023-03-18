#nullable enable

using System;
using System.Collections.Generic;
using System.Linq;
using JetBrains.Application.DataContext;
using JetBrains.Application.Threading;
using JetBrains.Application.UI.Actions.ActionManager;
using JetBrains.Application.UI.ActionsRevised.Handlers;
using JetBrains.Application.UI.Controls.BulbMenu.Items;
using JetBrains.Application.UI.Controls.GotoByName;
using JetBrains.Application.UI.DataContext;
using JetBrains.Application.UI.PopupLayout;
using JetBrains.Diagnostics;
using JetBrains.DocumentModel;
using JetBrains.DocumentModel.DataContext;
using JetBrains.Lifetimes;
using JetBrains.ProjectModel;
using JetBrains.ProjectModel.DataContext;
using JetBrains.RdBackend.Common.Features.Services;
using JetBrains.ReSharper.Daemon.CodeInsights;
using JetBrains.ReSharper.Feature.Services.Caches;
using JetBrains.ReSharper.Feature.Services.Daemon;
using JetBrains.ReSharper.Feature.Services.Navigation.Settings;
using JetBrains.ReSharper.Plugins.Unity.CSharp.Feature.Services.Navigation.GoToUnityUsages;
using JetBrains.ReSharper.Plugins.Unity.Rider.Common.Protocol;
using JetBrains.ReSharper.Plugins.Unity.Rider.Resources;
using JetBrains.ReSharper.Plugins.Unity.UnityEditorIntegration.Api;
using JetBrains.ReSharper.Plugins.Unity.Utils;
using JetBrains.ReSharper.Plugins.Unity.Yaml.Psi.Caches;
using JetBrains.ReSharper.Plugins.Unity.Yaml.Psi.DeferredCaches;
using JetBrains.ReSharper.Plugins.Unity.Yaml.Psi.DeferredCaches.AssetHierarchy.References;
using JetBrains.ReSharper.Plugins.Unity.Yaml.Psi.DeferredCaches.AssetInspectorValues;
using JetBrains.ReSharper.Plugins.Unity.Yaml.Psi.DeferredCaches.AssetInspectorValues.Values;
using JetBrains.ReSharper.Plugins.Unity.Yaml.Psi.DeferredCaches.UnityEvents;
using JetBrains.ReSharper.Psi;
using JetBrains.ReSharper.Psi.CSharp.Tree;
using JetBrains.ReSharper.Psi.DataContext;
using JetBrains.ReSharper.Psi.Tree;
using JetBrains.ReSharper.Psi.Util;
using JetBrains.ReSharper.Resources.Shell;
using JetBrains.Rider.Model;
using JetBrains.Util;

namespace JetBrains.ReSharper.Plugins.Unity.Rider.Common.CSharp.Daemon.CodeInsights
{
    [SolutionComponent]
    public class UnityCodeInsightFieldUsageProvider : AbstractUnityCodeInsightProvider
    {
        private readonly DeferredCacheController myDeferredCacheController;
        private readonly AssetInspectorValuesContainer myInspectorValuesContainer;
        private readonly UnityEventsElementContainer myUnityEventsElementContainer;
        private readonly DataContexts myContexts;
        private readonly IActionManager myActionManager;

        public override string ProviderId => "Unity serialized field";
        public override string DisplayName => Strings.UnityCodeInsightFieldUsageProvider_DisplayName_Unity_serialized_field;
        public override CodeVisionAnchorKind DefaultAnchor => CodeVisionAnchorKind.Right;

        public override ICollection<CodeVisionRelativeOrdering> RelativeOrderings =>
            new[] {new CodeVisionRelativeOrderingLast()};

        public UnityCodeInsightFieldUsageProvider(IFrontendBackendHost frontendBackendHost,
                                                  BulbMenuComponent bulbMenu,
                                                  DeferredCacheController deferredCacheController,
                                                  AssetInspectorValuesContainer inspectorValuesContainer,
                                                  UnityEventsElementContainer unityEventsElementContainer)
            : base(frontendBackendHost, bulbMenu)
        {
            myDeferredCacheController = deferredCacheController;
            myInspectorValuesContainer = inspectorValuesContainer;
            myUnityEventsElementContainer = unityEventsElementContainer;
            myActionManager = Shell.Instance.GetComponent<IActionManager>();
            myContexts = Shell.Instance.GetComponent<DataContexts>();
        }

        private static (Guid? guid, string[] propertyNames) GetAssetGuidAndPropertyName(ISolution solution, IField declaredElement)
        {
            Assertion.Assert(solution.Locks.IsReadAccessAllowed(), "ReadLock required");

            var containingType = declaredElement.ContainingType;
            if (containingType == null)
                return (null, Array.Empty<string>());

            var guid = AssetUtils.GetGuidFor(solution.GetComponent<MetaFileGuidCache>(), containingType);
            return (guid, AssetUtils.GetAllNamesFor(declaredElement).ToArray());
        }

        public override void OnClick(CodeInsightHighlightInfo highlightInfo, ISolution solution)
        {
            if (!(highlightInfo.CodeInsightsHighlighting is UnityInspectorCodeInsightsHighlighting))
                return;

            var rules = new List<IDataRule>();
            rules.AddRule("Solution", ProjectModelDataConstants.SOLUTION, solution);

            var declaredElement = highlightInfo.CodeInsightsHighlighting.DeclaredElement;
            rules.AddRule("DeclaredElement", PsiDataConstants.DECLARED_ELEMENTS_FROM_ALL_CONTEXTS, new[] {  declaredElement });

            using (ReadLockCookie.Create())
            {
                if (!declaredElement.IsValid())
                    return;

                rules.AddRule("DocumentEditorContext", DocumentModelDataConstants.EDITOR_CONTEXT, new DocumentEditorContext(highlightInfo.CodeInsightsHighlighting.Range));
                rules.AddRule("PopupWindowSourceOverride", UIDataConstants.PopupWindowContextSource,
                    new PopupWindowContextSource(_ => new RiderEditorOffsetPopupWindowContext(highlightInfo.CodeInsightsHighlighting.Range.StartOffset.Offset)));

                rules.AddRule("DontNavigateImmediatelyToSingleUsage", NavigationSettings.DONT_NAVIGATE_IMMEDIATELY_TO_SINGLE_USAGE, new object());

                var ctx = myContexts.CreateWithDataRules(Lifetime.Eternal, rules);

                var def = myActionManager.Defs.GetActionDef<GoToUnityUsagesAction>();
                def.EvaluateAndExecute(myActionManager, ctx);
            }
        }

        public void AddInspectorHighlighting(IHighlightingConsumer consumer, ICSharpDeclaration element,
            IDeclaredElement declaredElement, string baseDisplayName, string baseTooltip, string moreText, IconModel iconModel,
            IEnumerable<BulbMenuItem> items, List<CodeVisionEntryExtraActionModel> extraActions)
        {
            string? displayName = null;

            var solution = element.GetSolution();
            Assertion.Assert(solution.Locks.IsReadAccessAllowed(), "ReadLock required");

            var field = (declaredElement as IField).NotNull();
            var type = field.Type;
            var containingType = field.ContainingType;
            if (containingType == null)
            {
                base.AddHighlighting(consumer, element, field, baseDisplayName, baseTooltip, moreText, iconModel, items, extraActions);
                return;
            }

            var (guidN, propertyNames) = GetAssetGuidAndPropertyName(solution, field);
            if (guidN == null || propertyNames.Length == 0)
            {
                base.AddHighlighting(consumer, element, field, baseDisplayName, baseTooltip, moreText, iconModel, items, extraActions);
                return;
            }

            var guid = guidN.Value;

            var presentationType = GetUnityPresentationType(type);

            if (!myDeferredCacheController.CompletedOnce.Value || ShouldShowUnknownPresentation(presentationType))
            {
                base.AddHighlighting(consumer, element, field, baseDisplayName, baseTooltip, moreText, iconModel, items, extraActions);
                return;
            }

            if (presentationType == UnityPresentationType.UnityEvent)
            {
                var count = myUnityEventsElementContainer.GetUsageCountForEvent(field, out var estimated);
                var text = NounUtilEx.ToEmptyPluralOrSingularQuick(count, estimated,
                    Strings.UnityCodeInsightFieldUsageProvider_AddInspectorHighlighting_No_methods,
                    Strings.UnityCodeInsightFieldUsageProvider_AddInspectorHighlighting_method,
                    Strings.UnityCodeInsightFieldUsageProvider_AddInspectorHighlighting_methods);

                consumer.AddHighlighting(new UnityInspectorCodeInsightsHighlighting(element.GetNameDocumentRange(),
                    text, GetTooltip(count, estimated, false), Strings.UnityCodeInsightFieldUsageProvider_AddInspectorHighlighting_CapitalChar_Method, this,
                    declaredElement, iconModel, presentationType));
                return;
            }


            var initializer = (element as IFieldDeclaration).NotNull().Initial;
            var initValue = (initializer as IExpressionInitializer)?.Value?.ConstantValue;

            var initValueUnityPresentation = GetUnitySerializedPresentation(presentationType, initValue);

            int changesCount;
            bool isEstimated = false;
            bool isUniqueChange = false;
            if (myInspectorValuesContainer.IsIndexResultEstimated(guid, containingType, propertyNames))
            {
                changesCount = myInspectorValuesContainer.GetAffectedFiles(guid, propertyNames) -  myInspectorValuesContainer.GetAffectedFilesWithSpecificValue(guid, propertyNames, initValueUnityPresentation);
                displayName = string.Format(Strings.UnityCodeInsightFieldUsageProvider_AddInspectorHighlighting_Changed_in__0___assets, changesCount);
                isEstimated = true;
            }
            else
            {
                changesCount = 0;
                var initValueCount = myInspectorValuesContainer.GetValueCount(guid, propertyNames, initValueUnityPresentation);

                if (initValueCount == 0 && myInspectorValuesContainer.GetUniqueValuesCount(guid, propertyNames) == 1) // only modified value
                {
                    isUniqueChange = true;
                    var value  = myInspectorValuesContainer.GetUniqueValueDifferTo(guid, propertyNames, null);
                    displayName = value.GetPresentation(solution, field, false);
                }
                else if (initValueCount > 0 && myInspectorValuesContainer.GetUniqueValuesCount(guid, propertyNames) == 2)
                {
                    isUniqueChange = true;
                    // original value & only one modified value
                    var anotherValueWithLocation = myInspectorValuesContainer.GetUniqueValueDifferTo(guid, propertyNames, initValueUnityPresentation);
                    displayName = anotherValueWithLocation.GetPresentation(solution, field, false);
                }

                if (displayName == null || displayName.Equals("..."))
                {
                    changesCount = myInspectorValuesContainer.GetAffectedFiles(guid, propertyNames) -
                                   myInspectorValuesContainer.GetAffectedFilesWithSpecificValue(guid, propertyNames,
                                       initValueUnityPresentation);
                    if (changesCount == 0)
                    {
                        displayName = Strings.UnityCodeInsightFieldUsageProvider_AddInspectorHighlighting_Unchanged;
                    }
                    else
                    {
                        var word = NounUtil.ToPluralOrSingularQuick(changesCount, Strings.UnityCodeInsightFieldUsageProvider_AddInspectorHighlighting_asset, Strings.UnityCodeInsightFieldUsageProvider_AddInspectorHighlighting_assets);
                        displayName = string.Format(Strings.UnityCodeInsightFieldUsageProvider_AddInspectorHighlighting_Changed_in__0___1_, changesCount, word);
                    }
                }
            }

            consumer.AddHighlighting(new UnityInspectorCodeInsightsHighlighting(element.GetNameDocumentRange(),
                displayName, GetTooltip(changesCount, isEstimated, isUniqueChange), Strings.UnityCodeInsightFieldUsageProvider_AddInspectorHighlighting_Property_Inspector_values, this,
                declaredElement, iconModel, presentationType));
        }

        private string GetTooltip(int changesCount, bool isEstimated, bool isUniqueChange)
        {
            if (isUniqueChange)
                return Strings.UnityCodeInsightFieldUsageProvider_GetTooltip_Unique_change;

            if (changesCount == 0 && !isEstimated)
                return Strings.UnityCodeInsightFieldUsageProvider_GetTooltip_No_changes_in_assets;

            if (changesCount == 0 && isEstimated)
                return Strings.UnityCodeInsightFieldUsageProvider_GetTooltip_Possible_indirect_changes;

            if (changesCount == 1 && isEstimated)
                return Strings.UnityCodeInsightFieldUsageProvider_GetTooltip_Changed_in_1_asset___possible_indirect_changes;

            return isEstimated ? string.Format(Strings.UnityCodeInsightFieldUsageProvider_GetTooltip_Changed_in__0__assets___possible_indirect_changes, changesCount) : string.Format(Strings.UnityCodeInsightFieldUsageProvider_GetTooltip_Changed_in__0__assets, changesCount);
        }

        private IAssetValue GetUnitySerializedPresentation(UnityPresentationType presentationType, ConstantValue? value)
        {
            var b = false;
            if (presentationType == UnityPresentationType.Bool && (value == null || value.IsBoolean(out b)))
                return b ? new AssetSimpleValue("1") : new AssetSimpleValue("0");

            if (presentationType == UnityPresentationType.ScriptableObject && (value == null || value.IsNull()))
                return new AssetReferenceValue(new LocalReference(0, 0));

            if (presentationType == UnityPresentationType.FileId && (value == null || value.IsNull()))
                return new AssetReferenceValue(new LocalReference(0, 0));

            if (presentationType is UnityPresentationType.OtherSimple or UnityPresentationType.Bool &&
                (value == null || value.IsNull()))
            {
                return new AssetSimpleValue("0");
            }

            if (value == null || value.IsNull())
                return new AssetSimpleValue(string.Empty);

#pragma warning disable CS0618
            return new AssetSimpleValue(value.Value?.ToString());
#pragma warning restore CS0618
        }

        private UnityPresentationType GetUnityPresentationType(IType type)
        {
            if (type.GetTypeElement().DerivesFromUnityEvent())
                return UnityPresentationType.UnityEvent;

            if (type.GetTypeElement().DerivesFromScriptableObject())
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

            return typeElement.DerivesFrom(KnownTypes.GameObject)
                   || typeElement.DerivesFrom(KnownTypes.Component);
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
            ScriptableObject,
            UnityEvent
        }
    }
}
