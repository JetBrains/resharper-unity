// using JetBrains.Serialization;
//
// namespace JetBrains.ReSharper.Plugins.Unity.UIElements.Uxml.Psi.Caches
// {
//     public class UxmlElement
//     {
//         public UxmlElement(string name, int declarationOffset)
//         {
//             Name = name;
//             DeclarationOffset = declarationOffset;
//         }
//         
//         public string Name { get; }
//         public int DeclarationOffset { get; }
//
//         public static UxmlElement Read(UnsafeReader reader)
//         {
//             var name = reader.ReadString()!;
//             var declarationOffset = reader.ReadInt();
//             return new UxmlElement(name, declarationOffset);
//         }
//
//         public static void Write(UnsafeWriter writer, UxmlElement element)
//         {
//             writer.Write(element.Name);
//             writer.Write(element.DeclarationOffset);
//         }
//         
//         public override string ToString()
//         {
//             return $"{Name}:{DeclarationOffset}";
//         }
//     }
// }