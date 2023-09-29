using JetBrains.Annotations;
using JetBrains.ReSharper.Psi;
using JetBrains.ReSharper.Psi.Asxx.Tree;
using JetBrains.ReSharper.Psi.Asxx.Util;
using JetBrains.ReSharper.Psi.Impl.Shared.References;
using JetBrains.ReSharper.Psi.Resolve;
using JetBrains.ReSharper.Psi.Tree;
using JetBrains.ReSharper.Psi.Web.Impl.WebConfig.Tree.References;
using JetBrains.ReSharper.Psi.Web.Tree;
using JetBrains.ReSharper.Psi.Web.WebConfig.Util;

namespace JetBrains.ReSharper.Plugins.Unity.UIElements.Uxml.Psi.References
{
    public interface IUxmlTreeNode : IWebTreeNode
    {
    }
    
    public interface IUxmlToken : IAsxxTreeNode, ITokenNode
    {
        string GetUnquotedText();
    
        TreeTextRange GetUnquotedRangeWithin();
        TreeOffset GetUnquotedTreeStartOffset();
        TreeTextRange GetUnquotedTreeTextRange();
    }
    
    public class UxmlNamespaceReference: QualifiableReferenceWithinElement<IUxmlTreeNode,IUxmlToken>, IWebNamespaceReference
    {
        public UxmlNamespaceReference(IUxmlTreeNode owner, IQualifier qualifier, IUxmlToken token) : base(owner, qualifier, token)
        {
        }

        public UxmlNamespaceReference(IUxmlTreeNode owner, [CanBeNull] IQualifier qualifier, IUxmlToken token, TreeTextRange rangeWithin) : base(owner, qualifier, token, rangeWithin)
        {
        }

        public override Staticness GetStaticness() { return Staticness.OnlyStatic; }

        public override ITypeElement GetQualifierTypeElement() { return null; }
        public bool Resolved { get { return Resolve().DeclaredElement != null; } }
        
        public QualifierKind GetKind() { return QualifierKind.NAMESPACE; }
        
        public ISymbolTable GetSymbolTable( SymbolTableMode mode )
        {
            return AspNamespaceReferenceUtil.GetSymbolTable( this,mode);
        }

        protected override IReference BindToInternal( IDeclaredElement declaredElement,ISubstitution substitution )
        {
            var newNamespace = declaredElement as INamespace;
            if( newNamespace == null )
                return this;

            var start = RangeWithin.StartOffset;
            for( IWebNamespaceReference r = this; r != null; r = r.GetQualifier() as IWebNamespaceReference )
                start = r.RangeWithin.StartOffset;

            var oldRange = new TreeTextRange( start,RangeWithin.EndOffset);
            AsxxReferenceWithTokenUtil.SetText(Token,oldRange,newNamespace.QualifiedName,GetElement() );

            var end = start + new TreeOffset(newNamespace.QualifiedName.Length - 1);
            foreach (var newReference in GetElement().GetReferences())
            {
                var newNamespaceReference = newReference as UxmlNamespaceReference;
                if( newNamespaceReference != null && newNamespaceReference.RangeWithin.Contains(end) )
                    return newNamespaceReference;
            }

            return this;
        }
    }
}