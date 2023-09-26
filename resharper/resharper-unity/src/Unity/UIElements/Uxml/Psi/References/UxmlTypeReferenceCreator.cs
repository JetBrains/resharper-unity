// using JetBrains.Diagnostics;
// using JetBrains.ReSharper.Psi;
// using JetBrains.ReSharper.Psi.Resolve;
// using JetBrains.ReSharper.Psi.Tree;
// using JetBrains.ReSharper.Psi.Web.Impl.WebConfig.Tree.References;
// using JetBrains.ReSharper.Psi.Web.WebConfig.Util;
// using JetBrains.ReSharper.Psi.Xml.Tree;
//
// namespace JetBrains.ReSharper.Plugins.Unity.UIElements.Uxml.Psi.References
// {
//     public class UxmlTypeReferenceCreator : IWebTypeReferenceCreator
//     {
//         public IWebNamespaceReference CreateNamespaceReference(ITreeNode owner, IQualifier qualifier, ITreeNode token,
//             TreeTextRange rangeWithin)
//         {
//             return new UxmlNamespaceReference((IUxmlTreeNode) owner, qualifier, (IUxmlToken) token, rangeWithin);
//         }
//
//         public IWebTypeReference CreateTypeReference(ITreeNode owner, IQualifier qualifier, ITreeNode token, TreeTextRange rangeWithin,
//             string expectedBaseType = null)
//         {
//             Assertion.Assert(token is IXmlToken, "token is IXmlTokenNode");
//             return new UxmlTypeReference(owner, qualifier, (IXmlToken)token, rangeWithin, expectedBaseType);
//         }
//
//         public IWebModuleQualificationReference CreateModuleQualificationReference(ITreeNode owner, ITreeNode token,
//             TreeTextRange rangeWithin, bool lateBound = false)
//         {
//             Assertion.Assert(token is IXmlToken, "token is IXmlTokenNode");
//             return new ModuleQualificationReference(owner, (IXmlToken)token, rangeWithin, lateBound);
//         }
//
//         public TreeTextRange CalcRangeWithin(ITreeNode tokenElement)
//         {
//             var token = tokenElement as IUxmlToken;
//             Assertion.Assert(token != null, "token != null: " + tokenElement);
//             return token.GetUnquotedRangeWithin();
//         }
//
//         public ComplexTypeParser CreateParser(string text, TreeTextRange range)
//         {
//             return ComplexTypeParser.CreateParser(text, range);
//         }
//     }
// }