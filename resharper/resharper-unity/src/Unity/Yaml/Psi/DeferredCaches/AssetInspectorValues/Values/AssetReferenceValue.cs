using JetBrains.Annotations;
using JetBrains.Application.PersistentMap;
using JetBrains.Application.Threading;
using JetBrains.Diagnostics;
using JetBrains.ProjectModel;
using JetBrains.ReSharper.Plugins.Unity.Utils;
using JetBrains.ReSharper.Plugins.Unity.Yaml.Psi.Caches;
using JetBrains.ReSharper.Plugins.Unity.Yaml.Psi.DeferredCaches.AssetHierarchy;
using JetBrains.ReSharper.Plugins.Unity.Yaml.Psi.DeferredCaches.AssetHierarchy.Elements;
using JetBrains.ReSharper.Plugins.Unity.Yaml.Psi.DeferredCaches.AssetHierarchy.Elements.Stripped;
using JetBrains.ReSharper.Plugins.Unity.Yaml.Psi.DeferredCaches.AssetHierarchy.References;
using JetBrains.ReSharper.Psi;
using JetBrains.ReSharper.Psi.Util;
using JetBrains.Serialization;
using JetBrains.Util.Extension;

namespace JetBrains.ReSharper.Plugins.Unity.Yaml.Psi.DeferredCaches.AssetInspectorValues.Values
{
    [PolymorphicMarshaller]
    public class AssetReferenceValue : IAssetValue
    {
        [UsedImplicitly]
        public static UnsafeReader.ReadDelegate<object> ReadDelegate = Read;

        private static object Read(UnsafeReader reader) => new AssetReferenceValue( HierarchyReferenceUtil.ReadReferenceFrom(reader));

        [UsedImplicitly]
        public static UnsafeWriter.WriteDelegate<object> WriteDelegate = (w, o) => Write(w, o as AssetReferenceValue);

        private static void Write(UnsafeWriter writer, AssetReferenceValue value)
        {
            value.Reference.WriteTo(writer);
        }

        public IHierarchyReference Reference { get; }

        public AssetReferenceValue(IHierarchyReference reference)
        {
            Assertion.Assert(reference != null, "reference != null");
            Reference = reference;
        }

        protected bool Equals(AssetReferenceValue other)
        {
            return Reference.Equals(other.Reference);
        }

        public override bool Equals(object obj)
        {
            if (ReferenceEquals(null, obj)) return false;
            if (ReferenceEquals(this, obj)) return true;
            if (obj.GetType() != this.GetType()) return false;
            return Equals((AssetReferenceValue) obj);
        }

        public override int GetHashCode()
        {
            return Reference.GetHashCode();
        }

        public string GetPresentation(ISolution solution, IDeclaredElement declaredElement, bool prefabImport, bool isFull)
        {
            solution.GetComponent<IShellLocks>().AssertReadAccessAllowed();
            var hierarchyContainer = solution.GetComponent<AssetDocumentHierarchyElementContainer>();

            if ((declaredElement as IField)?.Type.GetTypeElement().DerivesFromScriptableObject() == true)
            {
                if (Reference is LocalReference localReference && localReference.LocalDocumentAnchor == 0)
                    return "None";

                var sourceFile = hierarchyContainer.GetSourceFile(Reference, out _);
                if (sourceFile == null)
                    return "...";

                if (!isFull)
                    return sourceFile.GetLocation().Name;

                return sourceFile.GetLocation().Name + $" (in {sourceFile.DisplayName.Replace('\\', '/').RemoveStart("Assets/").RemoveEnd("/" + sourceFile.GetLocation().Name)})";
            }

            if (Reference.LocalDocumentAnchor == 0)
                return "None";

            var processor = solution.GetComponent<AssetHierarchyProcessor>();
            var consumer = new UnityScenePathGameObjectConsumer(true);
            var element = hierarchyContainer.GetHierarchyElement(Reference, prefabImport);
            if (element == null)
                return "...";
            string result = "";

            if (!(element is IStrippedHierarchyElement))
            {
                processor.ProcessSceneHierarchyFromComponentToRoot(element, consumer, prefabImport);
                if (consumer.NameParts.Count == 0)
                    return "...";
                result += string.Join("/", consumer.NameParts);
            }
            else
            {
                return "...";
            }

            if (element is IComponentHierarchy componentHierarchy)
                result += $" ({AssetUtils.GetComponentName(solution.GetComponent<MetaFileGuidCache>(), componentHierarchy)})";

            return result;
        }

        public string GetPresentation(ISolution solution, IDeclaredElement declaredElement, bool prefabImport) => GetPresentation(solution, declaredElement, prefabImport, false);
        public string GetFullPresentation(ISolution solution, IDeclaredElement declaredElement, bool prefabImport) => GetPresentation(solution, declaredElement, prefabImport, true);
    }
}