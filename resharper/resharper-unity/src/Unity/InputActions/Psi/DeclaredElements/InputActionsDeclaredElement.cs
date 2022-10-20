using System.Collections.Generic;
using JetBrains.Annotations;
using JetBrains.ReSharper.Plugins.Json.Psi.DeclaredElements;
using JetBrains.ReSharper.Plugins.Json.Psi.Tree;
using JetBrains.ReSharper.Psi;
using JetBrains.ReSharper.Psi.Files;
using JetBrains.ReSharper.Psi.Search;
using JetBrains.ReSharper.Psi.Tree;
using JetBrains.Util;
using JetBrains.Util.DataStructures;

namespace JetBrains.ReSharper.Plugins.Unity.InputActions.Psi.DeclaredElements
{
    // Very similar to AsmDefNameDeclaredElement
    public class InputActionsDeclaredElement : JsonNewDeclaredElementBase, IInputActionsDeclaredElement
    {
        public InputActionsDeclaredElement(string name, IPsiSourceFile sourceFile, int declarationOffset)
            : base(sourceFile.GetPsiServices())
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
        public override DeclaredElementType GetElementType() => InputActionsDeclaredElementType.InputActions;
        public override string ShortName { get; }

        // We can't use the base class implementation, as this does it based on IDeclaration, and we don't have any.
        // We have to implement the interface explicitly because the method isn't virtual. Hacks are fun.
        HybridCollection<IPsiSourceFile> IDeclaredElement.GetSourceFiles()
        {
            return new HybridCollection<IPsiSourceFile>(SourceFile);
        }

        public override ISearchDomain GetSearchDomain(SearchDomainFactory factory)
        {
            var solution = SourceFile.GetSolution();
            return factory.CreateSearchDomain(solution, false);
        }

        [CanBeNull]
        public IJsonNewLiteralExpression GetTreeNode()
        {
            var range = TreeTextRange.FromLength(new TreeOffset(DeclarationOffset), ShortName.Length);
            var node = SourceFile.GetPrimaryPsiFile()?.FindNodeAt(range);
            return node?.Parent as IJsonNewLiteralExpression;
        }

        private bool Equals(InputActionsDeclaredElement other)
        {
            return Equals(SourceFile, other.SourceFile) && DeclarationOffset == other.DeclarationOffset &&
                   string.Equals(ShortName, other.ShortName);
        }

        public override bool Equals(object obj)
        {
            if (ReferenceEquals(null, obj)) return false;
            if (ReferenceEquals(this, obj)) return true;
            if (obj.GetType() != GetType()) return false;
            return Equals((InputActionsDeclaredElement) obj);
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