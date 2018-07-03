using System.Collections.Generic;
using JetBrains.Annotations;
using JetBrains.ReSharper.Psi;
using JetBrains.ReSharper.Psi.Files;
using JetBrains.ReSharper.Psi.JavaScript.Impl.DeclaredElements;
using JetBrains.ReSharper.Psi.JavaScript.Services;
using JetBrains.ReSharper.Psi.JavaScript.Tree;
using JetBrains.ReSharper.Psi.Search;
using JetBrains.ReSharper.Psi.Tree;
using JetBrains.Util;
using JetBrains.Util.DataStructures;

namespace JetBrains.ReSharper.Plugins.Unity.Json.Psi.DeclaredElements
{
    // The string literal value of the "name" JSON property doesn't have an IDeclaration, so it also doesn't have an
    // IDeclaredElement, so we have to create our own.
    // If we derive from JavaScriptDeclaredElementBase, then the JS reference searcher will consider us.
    // If we don't, then we'd need to create a references searcher just for these elements
    public class AsmDefNameDeclaredElement : JavaScriptDeclaredElementBase, IDeclaredElement
    {
        public AsmDefNameDeclaredElement(JavaScriptServices jsServices, string name, IPsiSourceFile sourceFile, int declarationOffset)
            : base(jsServices)
        {
            SourceFile = sourceFile;
            ShortName = name;
            DeclarationOffset = declarationOffset;
        }

        public IPsiSourceFile SourceFile { get; }
        public int DeclarationOffset { get; }
        public int NavigationOffset => DeclarationOffset + 1; // Skip quote

        public override IList<IDeclaration> GetDeclarationsIn(IPsiSourceFile sourceFile)
        {
            return sourceFile == SourceFile ? GetDeclarations() : EmptyList<IDeclaration>.InstanceList;
        }

        public override IList<IDeclaration> GetDeclarations() => EmptyList<IDeclaration>.Instance;
        public override DeclaredElementType GetElementType() => AsmDefDeclaredElementType.AsmDef;
        public override string ShortName { get; }
        public override bool HasDynamicName => false;

        public override ISearchDomain GetSearchDomain(SearchDomainFactory factory)
        {
            var solution = SourceFile.GetSolution();
            return factory.CreateSearchDomain(solution, false);
        }

        // We can't use the base class implementation, as this does it based on IDeclaration, and we don't have any.
        // We have to implement the interface explicitly because the method isn't virtual. Hacks are fun.
        HybridCollection<IPsiSourceFile> IDeclaredElement.GetSourceFiles()
        {
            return new HybridCollection<IPsiSourceFile>(SourceFile);
        }

        [CanBeNull]
        public IJavaScriptLiteralExpression GetTreeNode()
        {
            var range = TreeTextRange.FromLength(new TreeOffset(DeclarationOffset), ShortName.Length);
            var node = SourceFile.GetPrimaryPsiFile()?.FindNodeAt(range);
            return JavaScriptLiteralExpressionNavigator.GetByLiteral(node as ITokenNode);
        }

        private bool Equals(AsmDefNameDeclaredElement other)
        {
            return Equals(SourceFile, other.SourceFile) && DeclarationOffset == other.DeclarationOffset &&
                   string.Equals(ShortName, other.ShortName);
        }

        public override bool Equals(object obj)
        {
            if (ReferenceEquals(null, obj)) return false;
            if (ReferenceEquals(this, obj)) return true;
            if (obj.GetType() != GetType()) return false;
            return Equals((AsmDefNameDeclaredElement) obj);
        }

        public override int GetHashCode()
        {
            unchecked
            {
                var hashCode = (SourceFile != null ? SourceFile.GetHashCode() : 0);
                hashCode = (hashCode * 397) ^ DeclarationOffset;
                hashCode = (hashCode * 397) ^ ShortName.GetHashCode();
                return hashCode;
            }
        }
    }
}