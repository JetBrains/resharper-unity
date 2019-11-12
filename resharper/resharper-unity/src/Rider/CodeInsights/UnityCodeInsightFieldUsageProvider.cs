using System;
using System.Collections.Generic;
using System.Linq;
using JetBrains.Application.Threading;
using JetBrains.Application.UI.Controls;
using JetBrains.Application.UI.Controls.BulbMenu.Items;
using JetBrains.Application.UI.Controls.GotoByName;
using JetBrains.Application.UI.Controls.JetPopupMenu;
using JetBrains.Application.UI.Tooltips;
using JetBrains.Diagnostics;
using JetBrains.Lifetimes;
using JetBrains.ProjectModel;
using JetBrains.ReSharper.Daemon.CodeInsights;
using JetBrains.ReSharper.Feature.Services.Daemon;
using JetBrains.ReSharper.Feature.Services.Occurrences;
using JetBrains.ReSharper.Plugins.Unity.ProjectModel;
using JetBrains.ReSharper.Plugins.Unity.Resources.Icons;
using JetBrains.ReSharper.Plugins.Unity.Yaml.Psi;
using JetBrains.ReSharper.Plugins.Unity.Yaml.Psi.Caches;
using JetBrains.ReSharper.Plugins.Unity.Yaml.Psi.Caches.UnityEditorPropertyValues;
using JetBrains.ReSharper.Psi;
using JetBrains.ReSharper.Psi.CSharp.Tree;
using JetBrains.ReSharper.Psi.CSharp.Util;
using JetBrains.ReSharper.Psi.Files;
using JetBrains.ReSharper.Psi.Modules;
using JetBrains.ReSharper.Psi.Tree;
using JetBrains.ReSharper.Psi.Util;
using JetBrains.ReSharper.Resources.Shell;
using JetBrains.Rider.Model;
using JetBrains.TextControl.TextControlsManagement;

namespace JetBrains.ReSharper.Plugins.Unity.Rider.CodeInsights
{
    [SolutionComponent]
    public class UnityCodeInsightFieldUsageProvider : AbstractUnityCodeInsightProvider
    {
        private const int POP_UP_MAX_COUNT = 1000;
        public enum UnityPresentationType
        {
            Enum,
            Bool,
            String,
            OtherSimple,
            FileId,
            ValueType,
            Other,
        }

        private readonly Lifetime myLifetime;
        private readonly ConnectionTracker myConnectionTracker;
        private readonly UnityApi myUnityApi;
        private readonly IPsiFiles myFiles;
        private readonly UnityHost myUnityHost;
        private readonly UnitySceneDataLocalCache myUnitySceneDataLocalCache;
        private readonly ITooltipManager myTooltipManager;
        private readonly TextControlManager myTextControlManager;
        private readonly UnityEditorProtocol myProtocol;
        public override string ProviderId => "Unity serialized field";
        public override string DisplayName => "Unity serialized field";
        public override CodeLensAnchorKind DefaultAnchor => CodeLensAnchorKind.Right;

        public override ICollection<CodeLensRelativeOrdering> RelativeOrderings =>
            new[] {new CodeLensRelativeOrderingLast()};

        public UnityCodeInsightFieldUsageProvider(Lifetime lifetime, UnitySolutionTracker unitySolutionTracker, ConnectionTracker connectionTracker,
            UnityApi unityApi, UnityHost host, BulbMenuComponent bulbMenu, IPsiFiles files, UnityHost unityHost, UnitySceneDataLocalCache sceneDataCache,
            ITooltipManager tooltipManager, TextControlManager textControlManager, UnityEditorProtocol protocol)
            : base(unitySolutionTracker, host, bulbMenu)
        {
            myLifetime = lifetime;
            myConnectionTracker = connectionTracker;
            myUnityApi = unityApi;
            myFiles = files;
            myUnityHost = unityHost;
            myUnitySceneDataLocalCache = sceneDataCache;
            myTooltipManager = tooltipManager;
            myTextControlManager = textControlManager;
            myProtocol = protocol;
        }
        
        private static (string guid, string propertyName)? GetAssetGuidAndPropertyName(ISolution solution, IDeclaredElement declaredElement)
        {
            Assertion.Assert(solution.Locks.IsReadAccessAllowed(), "ReadLock required");
            
            var containingType = (declaredElement as IClrDeclaredElement)?.GetContainingType();
            if (containingType == null)
                return null;

            var sourceFile = declaredElement.GetSourceFiles().FirstOrDefault();
            if (sourceFile == null)
                return null;

            if (!sourceFile.IsValid())
                return null;

            var guid = solution.GetComponent<MetaFileGuidCache>().GetAssetGuid(sourceFile);
            if (guid == null)
                return null;

            // TODO [19.3] support several names for field 
//            var formerlySerializedAs = AttributeUtil.GetAttribute(fieldDeclaration, KnownTypes.FormerlySerializedAsAttribute);
//            var oldName = formerlySerializedAs?.Arguments.FirstOrDefault()?.Value?.ConstantValue?.Value as string;

            return (guid, declaredElement.ShortName);
        }
        
        public override void OnClick(CodeInsightsHighlighting highlighting, ISolution solution)
        {
            if (highlighting is UnityInspectorCodeInsightsHighlighting unityInspectorCodeInsightsHighlighting)
            {
                Shell.Instance.GetComponent<JetPopupMenus>().Show(solution.GetLifetime(),
                    JetPopupMenu.ShowWhen.NoItemsBannerIfNoItems, (lifetime, menu) =>
                    {
                        var presentationType = unityInspectorCodeInsightsHighlighting.UnityPresentationType;
                        var initValue = unityInspectorCodeInsightsHighlighting.ConstantValue;
                        var declaredElement = (highlighting.DeclaredElement as IClrDeclaredElement).NotNull("declaredElement != null");

                        if (!declaredElement.IsValid())
                            return;
                        
                        var result = GetAssetGuidAndPropertyName(solution, declaredElement);
                        if (!result.HasValue)
                            return;

                        
                        var valuesCache = solution.GetComponent<UnitySceneDataLocalCache>();
                        var values = valuesCache.GetPropertyValues(result.Value.guid, result.Value.propertyName);

                        menu.Caption.Value = WindowlessControlAutomation.Create("Inspector values");
                        menu.KeyboardAcceleration.Value = KeyboardAccelerationFlags.QuickSearch;

                        var valuesToShow = values.Where(t => !IsSameWithInitializer(t.Value, presentationType, initValue)).Take(POP_UP_MAX_COUNT);
                        foreach (var valueWithLocation in valuesToShow)
                        {
                            var value = valueWithLocation.Value;
                            if (ShouldShowUnknownPresentation(presentationType))
                                menu.ItemKeys.Add(valueWithLocation);
                            else
                                menu.ItemKeys.Add(valueWithLocation);

                        }

                        menu.DescribeItem.Advise(lifetime, e =>
                        {
                            var value = (e.Key as MonoBehaviourPropertyValueWithLocation).NotNull("value != null");

                            string shortPresentation;
                            if (ShouldShowUnknownPresentation(presentationType))
                            {
                                shortPresentation = "...";
                            }
                            else
                            {
                                if (!declaredElement.IsValid())
                                    return;

                                using (CompilationContextCookie.GetExplicitUniversalContextIfNotSet())
                                {
                                    var type = declaredElement.Type();
                                    shortPresentation = GetPresentation(value, presentationType, type, solution,
                                        declaredElement.Module, true);
                                }
                            }

                            e.Descriptor.Text = shortPresentation;
                            OccurrencePresentationUtil.AppendRelatedFile(e.Descriptor, value.File.DisplayName.Replace("\\", "/"));

                            e.Descriptor.Icon = UnityFileTypeThemedIcons.FileUnity.Id;
                            e.Descriptor.Style = MenuItemStyle.Enabled;
                        });

                        menu.ItemClicked.Advise(lifetime, key =>
                        {
                            if (!myConnectionTracker.IsConnectionEstablished())
                            {
                                ShowNotification();
                                return;
                            }

                            var value = (key as MonoBehaviourPropertyValueWithLocation).NotNull("value != null");
                            
                            UnityEditorFindUsageResultCreator.CreateRequestAndShow(myProtocol, myUnityHost, myLifetime, solution.SolutionDirectory, myUnitySceneDataLocalCache, 
                                value.Value.MonoBehaviour, value.File);
                        });
                    });
            }
        }

        private void ShowNotification()
        {
            var textControl = myTextControlManager.LastFocusedTextControl.Value;
            if (textControl == null)
                return;
            
            myTooltipManager.Show("Start the Unity Editor to view changes in the Inspector", lifetime => textControl.PopupWindowContextFactory.CreatePopupWindowContext(lifetime));
        }

        private string GetPresentation(MonoBehaviourPropertyValueWithLocation monoBehaviourPropertyValueWithLocation,
            UnityPresentationType unityPresentationType, IType enumType, ISolution solution, IPsiModule psiModule, bool showOwner)
        {
            string baseResult;
            if (monoBehaviourPropertyValueWithLocation.Value is MonoBehaviourPrimitiveValue)
            {
                baseResult = GetRiderPresentation(unityPresentationType, monoBehaviourPropertyValueWithLocation.GetSimplePresentation(solution), enumType, psiModule);
            }
            else 
            {
                baseResult = monoBehaviourPropertyValueWithLocation.GetSimplePresentation(solution);
            }

            if (showOwner)
                baseResult += " in " + monoBehaviourPropertyValueWithLocation.GetOwnerPresentation(solution);

            return baseResult;
        }
        
        private string GetRiderPresentation(UnityPresentationType unityPresentationType, string unityValue, IType enumType, IPsiModule psiModule)
        {
            if (unityPresentationType == UnityPresentationType.Bool)
            {
                if (unityValue.Equals("0"))
                    return "\"false\"";
                return "\"true\"";
            }

            if (unityPresentationType == UnityPresentationType.Enum)
            {
                if (!int.TryParse(unityValue, out var result))
                    return  "...";
                var @enum = enumType.GetTypeElement() as IEnum;
                var enumMemberType = @enum?.EnumMembers.FirstOrDefault()?.ConstantValue.Type;
                if (enumMemberType == null)
                    return "...";
                var enumMembers = CSharpEnumUtil.CalculateEnumMembers(new ConstantValue(result, enumMemberType), @enum);

                return string.Join(" | ", enumMembers.Select(t => t.ShortName));
            }

            return $"\"{unityValue ?? "..." }\"";
        }
        
        private bool ShouldShowUnknownPresentation(UnityPresentationType presentationType)
        {
            return presentationType == UnityPresentationType.Other ||
                   presentationType == UnityPresentationType.ValueType;
        }
        
        
        public void AddInspectorHighlighting(IHighlightingConsumer consumer, ICSharpDeclaration element,
            IDeclaredElement declaredElement, string baseDisplayName, string baseTooltip, string moreText, IconModel iconModel,
            IEnumerable<BulbMenuItem> items, List<CodeLensEntryExtraActionModel> extraActions)
        {
            string displayName = null;
            string tooltip = "Values from Unity Editor Inspector";

            var solution = element.GetSolution();
            Assertion.Assert(solution.Locks.IsReadAccessAllowed(), "ReadLock required");

            var result = GetAssetGuidAndPropertyName(solution, declaredElement);
            if (!result.HasValue)
            {
                base.AddHighlighting(consumer, element, declaredElement, baseDisplayName, baseTooltip, moreText, iconModel, items, extraActions);
                return;
            }

            var guid = result.Value.guid;
            var propertyName = result.Value.propertyName;

            var cache = solution.GetComponent<UnitySceneDataLocalCache>();

            var field = (declaredElement as IField).NotNull();
            var type = field.Type;
            var presentationType = GetUnityPresentationType(type);

            if (ShouldShowUnknownPresentation(presentationType))
            {
                base.AddHighlighting(consumer, element, field, baseDisplayName, baseTooltip, moreText, iconModel, items, extraActions);
                return;
            }
            
            var initializer = (element as IFieldDeclaration).NotNull("element as IFieldDeclaration != null").Initial;
            var initValue = (initializer as IExpressionInitializer)?.Value?.ConstantValue.Value;
            
            var initValueUnityPresentation = GetUnitySerializedPresentation(presentationType, initValue);
            var initValueCount = cache.GetValueCount(guid, propertyName, initValueUnityPresentation);

            if (initValueCount == 0 && cache.GetPropertyUniqueValuesCount(guid, propertyName) == 1) // only modified value
            {
                var valueWithLocations = cache.GetUniqueValuesWithLocation(guid, propertyName).ToArray(); 
                Assertion.Assert(valueWithLocations.Length == 1, "valueWithLocations.Length == 1"); //performance assertion
                displayName = GetPresentation(valueWithLocations[0], presentationType, type, solution,element.GetPsiModule(), false);
            } else if (initValueCount > 0 && cache.GetPropertyUniqueValuesCount(guid, propertyName) == 2)  
            {
                    
                // original value & only one modified value
                var values = cache.GetUniqueValuesWithLocation(guid, propertyName).ToArray();
                Assertion.Assert(values.Length == 2, "values.Length == 2"); //performance assertion
                    
                var anotherValueWithLocation = values[0].Value.Value.Equals(initValueUnityPresentation) ? values[1] : values[0];
                displayName = GetPresentation(anotherValueWithLocation, presentationType, type, solution,element.GetPsiModule(), false);
            }
            
            if (displayName == null)
            {
                var count = cache.GetFilesWithPropertyCount(guid, propertyName) - cache.GetFilesCountWithoutChanges(guid, propertyName, initValueUnityPresentation);
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

            
            consumer.AddHighlighting(new UnityInspectorCodeInsightsHighlighting(element.GetNameDocumentRange(),
                displayName, tooltip, "Property Inspector values", this, 
                declaredElement, iconModel, presentationType, initValue));
        }
        
        private object GetUnitySerializedPresentation(UnityPresentationType presentationType, object value)
        {
            if (presentationType == UnityPresentationType.Bool && value is bool b)
                return b ? "1" : "0";

            if (presentationType == UnityPresentationType.FileId && value == null)
                return FileID.Null;

            if ((presentationType == UnityPresentationType.OtherSimple  || presentationType == UnityPresentationType.Bool) && value == null)
                return "0";
            
            if (value == null)
                return string.Empty;

            return value.ToString();
        }
        
        private UnityPresentationType GetUnityPresentationType(IType type)
        {
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

            return myUnityApi.IsDescendantOf(KnownTypes.GameObject, typeElement) ||
                   myUnityApi.IsDescendantOf(KnownTypes.Component, typeElement);
        }
        
        private bool IsSameWithInitializer(MonoBehaviourPropertyValue value, UnityPresentationType type, object initValue)
        {
            if (value is MonoBehaviourReferenceValue referenceValue)
            {
                var fileId = referenceValue.Reference;
                if (initValue == null && fileId.IsNullReference)
                    return true;
                return false;
            }

            if (value is MonoBehaviourPrimitiveValue primitiveValue)
            {
                if (type == UnityPresentationType.Bool)
                {
                    if (initValue == null && primitiveValue.PrimitiveValue.Equals("0"))
                        return true;

                    if (initValue == null)
                        return false;

                    if (initValue.Equals(true) && primitiveValue.PrimitiveValue.Equals("1"))
                        return true;

                    if (initValue.Equals(false) && primitiveValue.PrimitiveValue.Equals("0"))
                        return true;
                    return false;
                }
                
                if (type == UnityPresentationType.String)
                {
                    if (initValue == null && primitiveValue.PrimitiveValue.Equals(String.Empty))
                        return true;
                }

                return primitiveValue.PrimitiveValue.Equals(initValue?.ToString());
            }

            
            return false;
        }
    }
}