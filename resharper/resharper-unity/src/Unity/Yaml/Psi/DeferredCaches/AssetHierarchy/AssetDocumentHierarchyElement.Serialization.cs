using System;
using JetBrains.Annotations;
using JetBrains.Application.PersistentMap;
using JetBrains.ReSharper.Plugins.Unity.Yaml.Psi.DeferredCaches.AssetHierarchy.Elements;
using JetBrains.ReSharper.Plugins.Unity.Yaml.Psi.DeferredCaches.AssetHierarchy.Elements.Stripped;
using JetBrains.Serialization;

namespace JetBrains.ReSharper.Plugins.Unity.Yaml.Psi.DeferredCaches.AssetHierarchy
{
    [PolymorphicMarshaller]

    public partial class AssetDocumentHierarchyElement
    {
        
        [UsedImplicitly] 
        public static UnsafeReader.ReadDelegate<object> ReadDelegate = Read;
        [UsedImplicitly]
        public static UnsafeWriter.WriteDelegate<object> WriteDelegate = (w, o) => Write(w, o as AssetDocumentHierarchyElement);
        
        private static object Read(UnsafeReader reader)
        {
            var otherCount = reader.ReadInt32();
            var gameObjectsCount = reader.ReadInt32();
            var transformCount = reader.ReadInt32();
            var scriptCount = reader.ReadInt32();
            var componentsCount = reader.ReadInt32();

            
            var result = new AssetDocumentHierarchyElement(otherCount, gameObjectsCount, transformCount, scriptCount, componentsCount);

            for (int i = 0; i < otherCount; i++)
            {
                var hierarchyElement = ReadHieraerchyElement(reader);
                result.myOtherBoxedElements.Add(hierarchyElement);
            }
            
            for (int i = 0; i < gameObjectsCount; i++)
            {
                result.myGameObjectHierarchies.Add(GameObjectHierarchy.Read(reader));
            }
            
            for (int i = 0; i < transformCount; i++)
            {
                result.myTransformElements.Add(TransformHierarchy.Read(reader));
            }
            
            for (int i = 0; i < scriptCount; i++)
            {
                result.myScriptComponentElements.Add(ScriptComponentHierarchy.Read(reader));
            }
            
            for (int i = 0; i < componentsCount; i++)
            {
                result.myComponentElements.Add(ComponentHierarchy.Read(reader));
            }
            
            return result;
        }

        private static IHierarchyElement ReadHieraerchyElement(UnsafeReader reader)
        {
            switch (reader.ReadInt32())
            {
                case 0:
                    return GameObjectHierarchy.Read(reader);
                case 1:
                    return ComponentHierarchy.Read(reader);
                case 2:
                    return PrefabInstanceHierarchy.Read(reader);
                case 3:
                    return ScriptComponentHierarchy.Read(reader);
                case 4:
                    return StrippedHierarchyElement.Read(reader);
                case 5:
                    return TransformHierarchy.Read(reader);
                default:
                    throw new InvalidOperationException("Unknown type");
            }
        }

        private static void Write(UnsafeWriter writer, AssetDocumentHierarchyElement value)
        {
            writer.Write(value.myOtherBoxedElements.Count);
            writer.Write(value.myGameObjectHierarchies.Count);
            writer.Write(value.myTransformElements.Count);
            writer.Write(value.myScriptComponentElements.Count);
            writer.Write(value.myComponentElements.Count);

            foreach (var v in value.myOtherBoxedElements)
            {
                WriteHierarchyElement(writer, v);
            }
            
            foreach (var v in value.myGameObjectHierarchies)
            {
                GameObjectHierarchy.Write(writer, v);
            }
            
            foreach (var v in value.myTransformElements)
            {
                TransformHierarchy.Write(writer, v);
            }
            
            foreach (var v in value.myScriptComponentElements)
            {
                ScriptComponentHierarchy.Write(writer, v);
            }
            
            foreach (var v in value.myComponentElements)
            {
                ComponentHierarchy.Write(writer, v);
            }
        }

        private static void WriteHierarchyElement(UnsafeWriter writer, IHierarchyElement hierarchyElement)
        {
            switch (hierarchyElement)
            {
                case GameObjectHierarchy gameObjectHierarchy:
                    writer.Write(0);
                    GameObjectHierarchy.Write(writer, gameObjectHierarchy);
                    break;
                case ComponentHierarchy componentHierarchy:
                    writer.Write(1);
                    ComponentHierarchy.Write(writer, componentHierarchy);
                    break;
                case PrefabInstanceHierarchy prefabInstanceHierarchy:
                    writer.Write(2);
                    PrefabInstanceHierarchy.Write(writer, prefabInstanceHierarchy);
                    break;
                case ScriptComponentHierarchy scriptComponentHierarchy:
                    writer.Write(3);
                    ScriptComponentHierarchy.Write(writer, scriptComponentHierarchy);
                    break;
                case StrippedHierarchyElement strippedHierarchyElement:
                    writer.Write(4);
                    StrippedHierarchyElement.Write(writer, strippedHierarchyElement);
                    break;
                case TransformHierarchy transformHierarchy:
                    writer.Write(5);
                    TransformHierarchy.Write(writer, transformHierarchy);

                    break;
            }
        }
    }
}