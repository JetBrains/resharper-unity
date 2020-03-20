using JetBrains.Annotations;
using JetBrains.Collections.Viewable;
using JetBrains.Diagnostics;
using JetBrains.Platform.RdFramework.Util;
using JetBrains.ProjectModel;
using JetBrains.ReSharper.Feature.Services.Occurrences;
using JetBrains.ReSharper.Feature.Services.Tree;
using JetBrains.ReSharper.Host.Features.Usages;
using JetBrains.ReSharper.Host.Platform.Icons;
using JetBrains.ReSharper.Plugins.Unity.Feature.Caches;
using JetBrains.ReSharper.Plugins.Unity.ProjectModel;
using JetBrains.ReSharper.Plugins.Unity.Resources.Icons;
using JetBrains.ReSharper.Plugins.Unity.Yaml.Feature.Services.Navigation;
using JetBrains.ReSharper.Plugins.Unity.Yaml.Psi.Caches;
using JetBrains.ReSharper.Plugins.Unity.Yaml.Psi.DeferredCaches;
using JetBrains.ReSharper.Plugins.Unity.Yaml.Psi.DeferredCaches.AssetHierarchy;
using JetBrains.ReSharper.Plugins.Unity.Yaml.Psi.DeferredCaches.AssetHierarchy.Elements;
using JetBrains.ReSharper.Plugins.Unity.Yaml.Psi.DeferredCaches.Interning;
using JetBrains.ReSharper.Psi;
using JetBrains.ReSharper.Resources.Shell;
using JetBrains.Rider.Model;
using JetBrains.UI.Icons;

namespace JetBrains.ReSharper.Plugins.Unity.Yaml.Host.Feature
{
    [SolutionComponent]
    public class UnityYamlExtraGroupingRulesProvider : IRiderExtraGroupingRulesProvider
    {
        // IconHost is optional so that we don't fail if we're in tests
        public UnityYamlExtraGroupingRulesProvider(UnityInterningCache unityInterningCache, MetaFileGuidCache metaFileGuidCache = null, UnitySolutionTracker unitySolutionTracker = null, IconHost iconHost = null)
        {
            if (unitySolutionTracker != null && unitySolutionTracker.IsUnityProject.HasValue() && unitySolutionTracker.IsUnityProject.Value
                && iconHost != null && metaFileGuidCache != null)
            {
                ExtraRules = new IRiderUsageGroupingRule[]
                {
                    new GameObjectUsageGroupingRule(iconHost),
                    new ComponentUsageGroupingRule(metaFileGuidCache, unityInterningCache, iconHost)
                };
            }
            else
            {
                ExtraRules = new IRiderUsageGroupingRule[0];
            }
        }

        public IRiderUsageGroupingRule[] ExtraRules { get; }
    }

    public abstract class UnityYamlUsageGroupingRuleBase : IRiderUsageGroupingRule
    {
        [CanBeNull] private readonly IconHost myIconHost;

        protected UnityYamlUsageGroupingRuleBase(string name, IconId iconId, [CanBeNull] IconHost iconHost,
            double sortingPriority)
        {
            Name = name;
            IconId = iconId;
            SortingPriority = sortingPriority;
            myIconHost = iconHost;
        }

        protected RdUsageGroup CreateModel(string text)
        {
            return new RdUsageGroup(Name, text, myIconHost?.Transform(IconId));
        }

        protected RdUsageGroup EmptyModel()
        {
            return new RdUsageGroup(Name, string.Empty, null);
        }

        public abstract RdUsageGroup CreateModel(IOccurrence occurrence, IOccurrenceBrowserDescriptor descriptor);
        public abstract void Navigate(IOccurrence occurrence);

        public string Name { get; }
        public IconId IconId { get; }
        public bool IsSeparable => true;
        public abstract bool IsNavigateable { get; }
        public bool Configurable => true;
        public bool PriorityDependsOnUsages => true;
        public double SortingPriority { get; }
        public bool DefaultValue { get; } = true;
        public IDeclaredElement GetDeclaredElement(IOccurrence occurrence) => null;
        public IProjectItem GetProjectItem(IOccurrence occurrence) => null;
    }

    // The priorities here put us after directory, file, namespace, type and member
    public class GameObjectUsageGroupingRule : UnityYamlUsageGroupingRuleBase
    {
        public GameObjectUsageGroupingRule([NotNull] IconHost iconHost)
            : base("Unity Game Object", UnityObjectTypeThemedIcons.UnityGameObject.Id, iconHost, 7.0)
        {
        }

        public override RdUsageGroup CreateModel(IOccurrence occurrence, IOccurrenceBrowserDescriptor descriptor)
        {
            using (CompilationContextCookie.GetExplicitUniversalContextIfNotSet())
            {
                if (occurrence is UnityAssetOccurrence assetOccurrence && !assetOccurrence.SourceFile.GetLocation().IsAsset())
                {
                    using (ReadLockCookie.Create())
                    {
                        var solution = occurrence.GetSolution();
                        var processor = solution.GetComponent<AssetHierarchyProcessor>();
                        var consumer = new UnityScenePathGameObjectConsumer();
                        processor.ProcessSceneHierarchyFromComponentToRoot(assetOccurrence.AttachedElementLocation, consumer, true, true);
                        string name = "...";
                        if (consumer.NameParts.Count > 0)
                            name = string.Join("\\", consumer.NameParts);

                        return CreateModel(name); 
                    }
                }
            }

            return EmptyModel();
        }

        public override void Navigate(IOccurrence occurrence)
        {
            throw new System.NotImplementedException();
        }

        public override bool IsNavigateable => false;
    }

    public class ComponentUsageGroupingRule : UnityYamlUsageGroupingRuleBase
    {
        private readonly MetaFileGuidCache myMetaFileGuidCache;
        private readonly UnityInterningCache myUnityInterningCache;

        public ComponentUsageGroupingRule(MetaFileGuidCache metaFileGuidCache, UnityInterningCache unityInterningCache, [NotNull] IconHost iconHost)
            : base("Unity Component", UnityObjectTypeThemedIcons.UnityComponent.Id, iconHost, 8.0)
        {
            myMetaFileGuidCache = metaFileGuidCache;
            myUnityInterningCache = unityInterningCache;
        }

        public override RdUsageGroup CreateModel(IOccurrence occurrence, IOccurrenceBrowserDescriptor descriptor)
        {
            using (CompilationContextCookie.GetExplicitUniversalContextIfNotSet())
            {
                if (occurrence is UnityAssetOccurrence assetOccurrence)
                {
                    var hierarchyContainer = assetOccurrence.GetSolution()?.GetComponent<AssetDocumentHierarchyElementContainer>();
                    var element = hierarchyContainer?.GetHierarchyElement(assetOccurrence.AttachedElementLocation, true);
                    if (element is IComponentHierarchy componentHierarchyElement)
                        return CreateModel(AssetUtils.GetComponentName(myMetaFileGuidCache, myUnityInterningCache, componentHierarchyElement));
                }
            }

            return EmptyModel();
        }

        public override void Navigate(IOccurrence occurrence)
        {
            throw new System.NotImplementedException();
        }

        public override bool IsNavigateable => false;
    }
}