using System;
using JetBrains.Annotations;
using JetBrains.Diagnostics;
using JetBrains.ProjectModel;
using JetBrains.ReSharper.Plugins.Unity.Yaml.Psi.Modules;
using JetBrains.ReSharper.Psi;
using JetBrains.Serialization;
using JetBrains.Util;

namespace JetBrains.ReSharper.Plugins.Unity.Yaml.Psi.Caches.UnityEditorPropertyValues
{
    public class MonoBehaviourPrimitiveValue : MonoBehaviourPropertyValue
    {
        [NotNull]
        public string PrimitiveValue { get; }
        
        public override object Value => PrimitiveValue;

        public MonoBehaviourPrimitiveValue([CanBeNull] string primitiveValue, [NotNull] string monoBehaviour,
            [CanBeNull] string localGameObjectAnchor)
            : base(monoBehaviour, localGameObjectAnchor)
        {
            PrimitiveValue = primitiveValue ?? string.Empty;
        }

        protected bool Equals(MonoBehaviourPrimitiveValue other)
        {
            return base.Equals(other) && string.Equals(PrimitiveValue, other.PrimitiveValue);
        }

        public override bool Equals(object obj)
        {
            if (ReferenceEquals(null, obj)) return false;
            if (ReferenceEquals(this, obj)) return true;
            if (obj.GetType() != GetType()) return false;
            return Equals((MonoBehaviourPrimitiveValue) obj);
        }

        public override int GetHashCode()
        {
            unchecked
            {
                return (base.GetHashCode() * 397) ^ PrimitiveValue.GetHashCode();
            }
        }

        internal override void WriteTo(UnsafeWriter writer)
        {
            writer.Write(1);
            writer.Write(PrimitiveValue);
            base.WriteTo(writer);
        }

        public override string GetSimplePresentation(ISolution solution, IPsiSourceFile file) => StringUtil.DropMiddleIfLong(PrimitiveValue, 30);

        public static MonoBehaviourPropertyValue ReadFrom(UnsafeReader reader)
        {
            return new MonoBehaviourPrimitiveValue(reader.ReadString().NotNull("primitiveValue != null"),
                reader.ReadString().NotNull("monoBehaviour != null"), reader.ReadString());
        }
    }

    public class MonoBehaviourReferenceValue : MonoBehaviourPropertyValue
    {
        [NotNull]
        public AssetDocumentReference Reference { get; }

        public MonoBehaviourReferenceValue([NotNull] AssetDocumentReference referenceValue, [NotNull] string monoBehaviour,
            [CanBeNull] string localGameObjectAnchor)
            : base(monoBehaviour, localGameObjectAnchor)
        {
            Reference = referenceValue;
        }

        protected bool Equals(MonoBehaviourReferenceValue other)
        {
            return base.Equals(other) && Reference.Equals(other.Reference);
        }

        public override object Value => Reference;

        public override bool Equals(object obj)
        {
            if (ReferenceEquals(null, obj)) return false;
            if (ReferenceEquals(this, obj)) return true;
            if (obj.GetType() != this.GetType()) return false;
            return Equals((MonoBehaviourReferenceValue) obj);
        }

        public override int GetHashCode()
        {
            unchecked
            {
                return (base.GetHashCode() * 397) ^ Reference.GetHashCode();
            }
        }

        public static MonoBehaviourPropertyValue ReadFrom(UnsafeReader reader)
        {
            return new MonoBehaviourReferenceValue(AssetDocumentReference.ReadFrom(reader),
                reader.ReadString().NotNull("monoBehaviour != null"), reader.ReadString());
        }

        internal override void WriteTo(UnsafeWriter writer)
        {
            writer.Write(0);
            Reference.WriteTo(writer);
            base.WriteTo(writer);
        }

        public override string GetSimplePresentation(ISolution solution, IPsiSourceFile file)
        {
            var componentName = GetReferenceName(solution, file);
            return $"{StringUtil.DropMiddleIfLong(componentName, 30)}";
        }
        
        private string GetReferenceName(ISolution solution, IPsiSourceFile file)
        {
            const string unknownResult = "...";
            
            var pathProvider = solution.GetComponent<UnitySceneDataLocalCache>();
            var consumer = new UnityPathCachedSceneConsumer();

            var gameObjectFile = file;
            if (Reference.ExternalAssetGuid != null)
            {
                var module = file.PsiModule as UnityExternalFilesPsiModule;
                Assertion.Assert(module != null, "module != null");
                var guidCache = solution.GetComponent<MetaFileGuidCache>();
                var filePath = guidCache.GetAssetFilePathsFromGuid(Reference.ExternalAssetGuid).FirstOrDefault(null);
                if (filePath == null)
                    return unknownResult;

                if (!module.TryGetFileByPath(filePath, out gameObjectFile))
                {
                    return unknownResult;
                }
            }
            
            
            pathProvider.ProcessSceneHierarchyFromComponentToRoot(gameObjectFile, Reference.LocalDocumentAnchor, consumer);

            var parts = consumer.NameParts;
            if (parts.Count == 0)
            {
                var cache = solution.GetComponent<UnityGameObjectNamesCache>();
                if (cache.Map.TryGetValue(gameObjectFile, out var anchorToName))
                {
                    if (anchorToName.TryGetValue(Reference.LocalDocumentAnchor, out var name))
                    {
                        return name;
                    }
                }
                return "...";

            }

            return parts[parts.Count - 1];
        }
    }

    public abstract class MonoBehaviourPropertyValue
    {
        [NotNull]
        public string MonoBehaviour { get; }

        [NotNull]
        public string LocalGameObjectAnchor { get; }
        
        public abstract object Value { get; }

        private const int wrapCount = 0;

        public MonoBehaviourPropertyValue([NotNull] string monoBehaviour, [CanBeNull] string localGameObjectAnchor)
        {
            MonoBehaviour = monoBehaviour;
            LocalGameObjectAnchor = localGameObjectAnchor;
        }

        public override bool Equals(object obj)
        {
            if (!(obj is MonoBehaviourPropertyValue value))
                return false;

            return MonoBehaviour.Equals(value.MonoBehaviour);
        }

        protected bool Equals(MonoBehaviourPropertyValue other)
        {
            return MonoBehaviour.Equals(other.MonoBehaviour);
        }

        public override int GetHashCode()
        {
            return MonoBehaviour.GetHashCode();
        }

        internal virtual void WriteTo(UnsafeWriter writer)
        {
            writer.Write(MonoBehaviour);
            writer.Write(LocalGameObjectAnchor);
        }

        public abstract string GetSimplePresentation(ISolution solution, IPsiSourceFile file);

        public virtual string GetOwnerPresentation(ISolution solution, IPsiSourceFile file)
        {
            const string unknownResult = "...";
            
            var pathProvider = solution.GetComponent<UnitySceneDataLocalCache>();
            var consumer = new UnityPathCachedSceneConsumer();
            
            pathProvider.ProcessSceneHierarchyFromComponentToRoot(file, LocalGameObjectAnchor, consumer);
            var parts = consumer.NameParts;
            if (parts.Count == 0)
                return unknownResult;
      
            
            return $"{StringUtil.DropMiddleIfLong(parts[parts.Count - 1], 30)}";
        }
    }

    public static class MonoBehaviourPropertyValueMarshaller
    {
        public static MonoBehaviourPropertyValue Read(UnsafeReader reader)
        {
            var type = reader.ReadInt32();
            switch (type)
            {
                case 0:
                    return MonoBehaviourReferenceValue.ReadFrom(reader);
                case 1:
                    return MonoBehaviourPrimitiveValue.ReadFrom(reader);
                default:
                    throw new InvalidOperationException();
            }
        }

        public static void Write(UnsafeWriter writer, MonoBehaviourPropertyValue value) => value.WriteTo(writer);
    }
}