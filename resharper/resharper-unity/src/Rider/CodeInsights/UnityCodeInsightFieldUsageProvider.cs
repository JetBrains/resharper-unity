using System;
using System.Collections.Generic;
using System.Linq;
using JetBrains.Application.UI.Controls;
using JetBrains.Application.UI.Controls.GotoByName;
using JetBrains.Application.UI.Controls.JetPopupMenu;
using JetBrains.Diagnostics;
using JetBrains.ProjectModel;
using JetBrains.ReSharper.Daemon.CodeInsights;
using JetBrains.ReSharper.Feature.Services.Daemon;
using JetBrains.ReSharper.Feature.Services.Occurrences;
using JetBrains.ReSharper.Plugins.Unity.ProjectModel;
using JetBrains.ReSharper.Plugins.Unity.Resources.Icons;
using JetBrains.ReSharper.Plugins.Unity.Yaml.Psi.Caches;
using JetBrains.ReSharper.Plugins.Unity.Yaml.Psi.Caches.UnityEditorPropertyValues;
using JetBrains.ReSharper.Psi;
using JetBrains.ReSharper.Psi.CSharp.Tree;
using JetBrains.ReSharper.Psi.Tree;
using JetBrains.ReSharper.Psi.Util;
using JetBrains.ReSharper.Resources.Shell;
using JetBrains.Rider.Model;

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
            FileId,
            ValueType,
            Other
        }
        
        private readonly UnityApi myUnityApi;
        public override string ProviderId => "Unity serialized field";
        public override string DisplayName => "Unity serialized field";
        public override CodeLensAnchorKind DefaultAnchor => CodeLensAnchorKind.Right;

        public override ICollection<CodeLensRelativeOrdering> RelativeOrderings =>
            new[] {new CodeLensRelativeOrderingLast()};

        public UnityCodeInsightFieldUsageProvider(UnitySolutionTracker unitySolutionTracker, UnityApi unityApi, UnityHost host,
            BulbMenuComponent bulbMenu)
            : base(unitySolutionTracker, host, bulbMenu)
        {
            myUnityApi = unityApi;
        }
        
        private static (string guid, string propertyName)? GetAssetGuidAndPropertyName(ISolution solution, IDeclaredElement declaredElement)
        {
            var containingType = (declaredElement as IClrDeclaredElement)?.GetContainingType();
            if (containingType == null)
                return null;

            var sourceFile = declaredElement.GetSourceFiles().FirstOrDefault();
            if (sourceFile == null)
                return null;

            var guid = solution.GetComponent<MetaFileGuidCache>().GetAssetGuid(sourceFile);
            if (guid == null)
                return null;

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
                        var result = GetAssetGuidAndPropertyName(solution, highlighting.DeclaredElement);
                        if (!result.HasValue)
                            return;

                        var valuesCache = solution.GetComponent<UnityPropertyValueCache>();
                        var values = valuesCache.GetUnityPropertyValues(result.Value.guid, result.Value.propertyName);

                        menu.Caption.Value = WindowlessControlAutomation.Create("Inspector values");
                        menu.KeyboardAcceleration.Value = KeyboardAccelerationFlags.QuickSearch;

                        foreach (var valueWithLocation in values.Take(POP_UP_MAX_COUNT))
                        {
                            var value = valueWithLocation.Value;
                            if (ShouldShowUnknownPresentation(value, presentationType))
                                menu.ItemKeys.Add(valueWithLocation);
                            else if (!IsSameWithInitializer(value, presentationType, initValue)) 
                                menu.ItemKeys.Add(valueWithLocation);

                        }

                        menu.DescribeItem.Advise(lifetime, e =>
                        {
                            var value = (e.Key as MonoBehaviourPropertyValueWithLocation)
                                .NotNull("value != null");

                            string shortPresentation;
                            if (ShouldShowUnknownPresentation(value.Value, presentationType))
                            {
                                shortPresentation = "...";
                            }
                            else
                            {
                                shortPresentation = value.Value.GetSimplePresentation(solution, value.File) ?? "...";
                            }

                            e.Descriptor.Text = shortPresentation;
                            OccurrencePresentationUtil.AppendRelatedFile(e.Descriptor, value.File.DisplayName);

                            e.Descriptor.Icon = UnityFileTypeThemedIcons.FileUnity.Id;
                        });

                        menu.ItemClicked.Advise(lifetime, key =>
                        {
                            
                        });
                    });
            }
        }

        private bool ShouldShowUnknownPresentation(MonoBehaviourPropertyValue value, UnityPresentationType presentationType)
        {
            return presentationType == UnityPresentationType.Other ||
                presentationType == UnityPresentationType.ValueType && value is MonoBehaviourHugeValue;
        }
        
        
        public void AddInspectorHighlighting(IHighlightingConsumer consumer, ICSharpDeclaration element,
            IDeclaredElement declaredElement, IconModel iconModel)
        {
            string displayName = null;
            string tooltip = "Values from Unity Editor Inspector";

            var solution = element.GetSolution();

            var result = GetAssetGuidAndPropertyName(solution, declaredElement);
            if (!result.HasValue)
                return;

            var guid = result.Value.guid;
            var propertyName = result.Value.propertyName;

            var cache = solution.GetComponent<UnityPropertyValueCache>();
            var values = cache.GetUnityPropertyValues(guid, propertyName).ToArray();

            var field = (declaredElement as IField).NotNull();
            var type = field.Type;
            var presentationType = GetUnityPresentationType(type);
            var changesCount = 0;
            
            
            var initializer = (element as IFieldDeclaration).NotNull("element as IFieldDeclaration != null").Initial;
            var initValue = (initializer as IExpressionInitializer)?.Value?.ConstantValue.Value;

            if (presentationType != UnityPresentationType.Other)
            {
                if (cache.HasUniqueValue(guid, propertyName))
                {
                    if (IsSameWithInitializer(values[0].Value, presentationType, initValue))
                    {
                        changesCount = 0;
                    }
                    else
                    {
                        displayName = values[0].GetSimplePresentation(solution);
                    }
                }
                else
                {
                    changesCount = cache.GetValueCount(guid, propertyName, GetUnitySerializedPresentation(presentationType, initValue));
                }
            }
            else
            {
                changesCount = values.Length;
            }


            if (displayName == null)
            {
                if (changesCount == 0)
                {
                    displayName = "No Inspector changes";
                } else if (changesCount == 1)
                {
                    displayName = "1 Inspector change";
                }
                else
                {
                    displayName = $"{changesCount} Inspector changes";
                }
            }

            
            consumer.AddHighlighting(new UnityInspectorCodeInsightsHighlighting(element.GetNameDocumentRange(),
                displayName, tooltip, "Property Inspector values", this, 
                declaredElement, iconModel, presentationType, initValue));
        }

        private string GetUnitySerializedPresentation(UnityPresentationType presentationType, object value)
        {
            if (presentationType == UnityPresentationType.Bool && value is bool b)
                return b ? "1" : "0";

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