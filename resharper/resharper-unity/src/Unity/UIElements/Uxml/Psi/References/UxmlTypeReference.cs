// using System.Linq;
// using JetBrains.Annotations;
// using JetBrains.Diagnostics;
// using JetBrains.ReSharper.Psi;
// using JetBrains.ReSharper.Psi.ExtensionsAPI.Resolve;
// using JetBrains.ReSharper.Psi.Modules;
// using JetBrains.ReSharper.Psi.Resolve;
// using JetBrains.ReSharper.Psi.Tree;
// using JetBrains.ReSharper.Psi.Web.Impl.WebConfig.Tree.References;
// using JetBrains.ReSharper.Psi.Web.WebConfig.Tree;
// using JetBrains.ReSharper.Psi.Web.WebConfig.Util;
// using JetBrains.ReSharper.Psi.Xml.Impl.Tree.References;
// using JetBrains.ReSharper.Psi.Xml.Impl.Util;
// using JetBrains.ReSharper.Psi.Xml.Tree;
// using JetBrains.ReSharper.Psi.Xml.Tree.References;
// using JetBrains.Util;
//
// namespace JetBrains.ReSharper.Plugins.Unity.UIElements.Uxml.Psi.References
// {
//   public class UxmlTypeReference : XmlQualifiableReferenceWithToken, IXmlSmartCompletableReference, IXmlTypeNameCompletableReference, IWebTypeReference
//   {
//     private readonly string myExpectedBaseType;
//     private readonly bool myAllowEmpty;
//
//     public virtual bool KeywordsAllowed { get { return false; } }
//     public virtual PsiLanguageType KeywordsLanguage { get { return UnknownLanguage.Instance; } }
//
//     public bool MustBePublic { get; }
//
//     public UxmlTypeReference([NotNull] ITreeNode owner, [CanBeNull] IQualifier qualifier, IXmlToken token, TreeTextRange rangeWithin, string expectedBaseType = null, bool mustBePublic = false, bool allowEmpty = false)
//       : base(owner, qualifier, token, rangeWithin)
//     {
//       myExpectedBaseType = expectedBaseType;
//       myAllowEmpty = allowEmpty;
//       MustBePublic = mustBePublic;
//     }
//
//     public override string GetName()
//     {
//       var baseName = base.GetName();
//       var apos = baseName.IndexOf('`');
//       if (apos < 0)
//         return baseName;
//
//       return baseName.Substring(0, apos);
//     }
//
//     public int GetTypeArgumentCount()
//     {
//       var baseName = base.GetName();
//       var apos = baseName.IndexOf( '`' );
//       if( apos < 0 )
//         return 0;
//       
//       var s = baseName.Substring( apos+1 ).Trim();
//       if( !int.TryParse( s,out var typeArgumentCount ) )
//         return -1;
//       return typeArgumentCount;
//     }
//
//
//     public override Staticness GetStaticness() { return Staticness.OnlyStatic; }
//     public override ITypeElement GetQualifierTypeElement() { return null; }
//     public bool Resolved { get { return Resolve().DeclaredElement != null; } }
//     public QualifierKind GetKind() { return QualifierKind.TYPE; }
//
//     public virtual string ExpectedBaseType { get { return myExpectedBaseType; } }
//
//     protected override SymbolTableMode SymbolTableMode
//     {
//       get { return SymbolTableMode.TYPE_AND_NAMESPACES; }
//     }
//
//     public ISymbolTable GetSymbolTable(SymbolTableMode mode)
//     {
//       var resolveResult = Resolve().Result;
//       var typeElement = resolveResult.DeclaredElement as ITypeElement;
//       if (typeElement == null)
//         return EmptySymbolTable.INSTANCE;
//
//       var type = TypeFactory.CreateType(typeElement, resolveResult.Substitution);
//       return type.GetSymbolTable(GetPsiModule());
//     }
//
//     ITreeNode IWebTypeReference.TokenElement { get { return Token; } }
//
//     public IReference FixModuleQualification(IPsiModule psiModule)
//     {
//       if (psiModule == null)
//         return this;
//
//       var offset = RangeWithin.StartOffset + Token.GetTreeStartOffset();
//
//       ParsedTypeInfo typeInfo = null;
//       this.GetTypePart(ref typeInfo);
//       if (typeInfo == null)
//         return this;
//
//       var moduleQualificationReference = this.FindModuleQualificationReference(Token);
//       if (moduleQualificationReference != null)
//       {
//         moduleQualificationReference.BindTo(psiModule);
//       }
//       else
//       {
//         var typeAttribute = GetElement() as IWebConfigTypeAttribute;
//         if (typeAttribute != null && typeAttribute.ModuleQualificationRequired &&
//             WebConfigTypeAttributeUtil.GetParsedTypeInfo(typeAttribute) == typeInfo)
//         {
//           ReferenceWithTokenUtil.SetText(Token, new TreeTextRange(RangeWithin.EndOffset),
//             ", " + AspFilesUtil.GetModuleQualification(GetElement(), psiModule), GetElement());
//         }
//       }
//
//       var newReference = GetElement().FindReference<IReference>(offset);
//       Assertion.AssertNotNull(newReference);
//       return newReference;
//     }
//
//     protected override IReference BindToInternal(IDeclaredElement declaredElement, ISubstitution substitution)
//     {
//       var @namespace = declaredElement as INamespace;
//       if (@namespace != null)
//       {
//         var newReference = ReferenceWithTokenUtil.SetText(this, @namespace.ShortName);
//         return newReference;
//       }
//
//       var newType = declaredElement as ITypeElement;
//       Assertion.Assert(newType != null, "TypeElement expected, {0} passed", declaredElement);
//
//       var newName = newType.ShortName + ((newType.TypeParameters.Count == 0) ? "" : "`" + newType.TypeParameters.Count);
//       if (base.GetName() != newName)
//       {
//         var newReference = ReferenceWithTokenUtil.SetText(this, newName);
//         return newReference.BindTo(declaredElement);
//       }
//
//       if (newType.Equals(Resolve().DeclaredElement))
//         return FixModuleQualification(newType.Module);
//
//       ParsedTypeInfo typeInfo = null;
//       var part = this.GetTypePart(ref typeInfo);
//       Assertion.AssertNotNull(part);
//
//       var end = RangeWithin.EndOffset;
//       if (typeInfo.Parts.Last().TypeArgumentCountRange.IsValid())
//         end = typeInfo.Parts.Last().TypeArgumentCountRange.EndOffset;
//       var oldRange = new TreeTextRange(typeInfo.Parts[0].IdentifierRange.StartOffset, end);
//       var tokenStartOffset = Token.GetTreeStartOffset();
//       ReferenceWithTokenUtil.SetText(Token, oldRange, newType.GetClrName().FullName, GetElement());
//
//       var apos = newType.GetClrName().FullName.LastIndexOf('`');
//       var newOffset = oldRange.StartOffset + (apos < 0 ? newType.GetClrName().FullName.Length : apos) - 1;
//
//       {
//         var newReference = GetElement().FindReference<UxmlTypeReference>(newOffset + tokenStartOffset);
//         Assertion.Assert(newReference != null);
//         return newReference.FixModuleQualification(newType.Module);
//       }
//     }
//
//     public override ResolveResultWithInfo GetResolveResult(ISymbolTable symbolTable, string referenceName)
//     {
//       var resolveResult = base.GetResolveResult(symbolTable, referenceName);
//       if ((resolveResult.ResolveErrorType == ResolveErrorType.NOT_RESOLVED) && myAllowEmpty && referenceName.IsEmpty())
//       {
//         resolveResult = new ResolveResultWithInfo(resolveResult.Result, ResolveErrorType.IGNORABLE);
//       }
//       return resolveResult;
//     }
//
//     public override ResolveResultWithInfo Resolve(ISymbolTable symbolTable, IAccessContext context)
//     {
//       var resolveResult = base.Resolve(symbolTable, context);
//       if (resolveResult.ResolveErrorType == ResolveErrorType.NOT_RESOLVED)
//       {
//         // if (!IsKnownTypeReference())
//         //   return ResolveResultWithInfo.Ignore;
//
//         if (GetTreeNode().IsInCustomSection())
//           return ResolveResultWithInfo.Ignore;
//
//         return this.CheckResolveResultWithModule(Token, resolveResult);
//       }
//       return resolveResult;
//     }
//   }
// }