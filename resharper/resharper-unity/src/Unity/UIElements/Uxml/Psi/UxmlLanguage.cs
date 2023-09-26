// using JetBrains.Annotations;
// using JetBrains.ReSharper.Psi;
// using JetBrains.ReSharper.Psi.Xml;
//
// namespace JetBrains.ReSharper.Plugins.Unity.UIElements.Uxml.Psi
// {
//     [LanguageDefinition(Name)]
//     public class UxmlLanguage : XmlLanguage
//     {
//         public new const string Name = "UXML";
//
//         [CanBeNull, UsedImplicitly] 
//         public new static UxmlLanguage Instance { get; private set; }
//
//         private UxmlLanguage() : this(Name)
//         {
//         }
//
//         protected UxmlLanguage([NotNull] string name) : base(name)
//         {
//         }
//
//         protected UxmlLanguage([NotNull] string name, [NotNull] string presentableName) : base(name, presentableName)
//         {
//         }
//     }
// }