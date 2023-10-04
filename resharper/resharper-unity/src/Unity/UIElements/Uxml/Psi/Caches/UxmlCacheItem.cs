// using System.Collections.Generic;
// using JetBrains.Annotations;
// using JetBrains.Serialization;
// using JetBrains.Util.PersistentMap;
//
// namespace JetBrains.ReSharper.Plugins.Unity.UIElements.Uxml.Psi.Caches
// {
//     public class UxmlCacheItem
//     {
//          private static readonly IUnsafeMarshaller<UxmlCacheItem> ourUxmlCacheItemMarshaller =
//             new UniversalMarshaller<UxmlCacheItem>(Read, Write);
//          
//          public static readonly UnsafeFilteredCollectionMarshaller<UxmlCacheItem, List<UxmlCacheItem>> Marshaller =
//              new(ourUxmlCacheItemMarshaller, n => new List<UxmlCacheItem>(n), item => item != null);
//
//
//         public UxmlCacheItem(string controlTypeName, [CanBeNull] UxmlElement nameElement, [CanBeNull] UxmlElement classNameElement)
//         {
//             ControlTypeName = controlTypeName;
//             NameElement = nameElement;
//             ClassNameElement = classNameElement;
//         }
//
//         public string ControlTypeName { get; }
//         public UxmlElement NameElement { get; }
//         public UxmlElement ClassNameElement { get; }
//
//         private static UxmlCacheItem Read(UnsafeReader reader)
//         {
//             var controlTypeName = reader.ReadString();
//             var nameElement = UxmlElement.Read(reader);
//             var classNameElement = UxmlElement.Read(reader);
//             return new UxmlCacheItem(controlTypeName, nameElement, classNameElement);
//         }
//
//         private static void Write(UnsafeWriter writer, UxmlCacheItem value)
//         {
//             writer.Write(value.ControlTypeName);
//             UxmlElement.Write(writer, value.NameElement);
//             UxmlElement.Write(writer, value.ClassNameElement);
//         }
//         
//         public override string ToString()
//         {
//             return $"{ControlTypeName}:{NameElement}:{ClassNameElement}";
//         }
//     }
// }