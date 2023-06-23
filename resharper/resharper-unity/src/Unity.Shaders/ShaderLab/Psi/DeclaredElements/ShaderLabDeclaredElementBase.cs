using System.Collections.Generic;
using System.Xml;
using JetBrains.ReSharper.Plugins.Unity.Shaders.ShaderLab.Psi.Tree;
using JetBrains.ReSharper.Psi;
using JetBrains.ReSharper.Psi.Files;
using JetBrains.ReSharper.Psi.Tree;
using JetBrains.Util;
using JetBrains.Util.DataStructures;

namespace JetBrains.ReSharper.Plugins.Unity.Shaders.ShaderLab.Psi.DeclaredElements
{
    public abstract class ShaderLabDeclaredElementBase : IShaderLabDeclaredElement
    {
        protected readonly IPsiSourceFile SourceFile;
        protected int TreeOffset { get; }
        public string ShortName { get; }

        // Note that ShaderLab variable references and Cg parameters are case sensitive
        public bool CaseSensitiveName => true;

        // ReSharper disable once AssignNullToNotNullAttribute
        public PsiLanguageType PresentationLanguage => ShaderLabLanguage.Instance;

        protected ShaderLabDeclaredElementBase(string shortName, IPsiSourceFile sourceFile, int treeOffset)
        {
            SourceFile = sourceFile;
            TreeOffset = treeOffset;
            ShortName = shortName;
        }

        public IPsiServices GetPsiServices() => SourceFile.GetPsiServices();

        public virtual IList<IDeclaration> GetDeclarations()
        {
            if (SourceFile.GetPrimaryPsiFile() is not IShaderLabFile psi)
                return EmptyList<IDeclaration>.InstanceList;

            var node = psi.FindNodeAt(TreeTextRange.FromLength(new TreeOffset(TreeOffset), 1));
            while (node != null && node is not IDeclaration)
                node = node.Parent;
            if (node == null)
                return EmptyList<IDeclaration>.Instance;

            return new[] {(IDeclaration) node};
        }

        public IList<IDeclaration> GetDeclarationsIn(IPsiSourceFile sourceFile)
        {
            if (SourceFile == sourceFile)
                return GetDeclarations();
            return EmptyList<IDeclaration>.Instance;
        }

        public abstract DeclaredElementType GetElementType();

        public XmlNode GetXMLDoc(bool inherit) => null;
        public XmlNode GetXMLDescriptionSummary(bool inherit) => null;
        public bool IsValid() => true;
        public bool IsSynthetic() => false;

        public HybridCollection<IPsiSourceFile> GetSourceFiles() => new(SourceFile);

        public bool HasDeclarationsIn(IPsiSourceFile sourceFile) => SourceFile == sourceFile;

        public override bool Equals(object obj)
        {
            if (ReferenceEquals(obj, this)) return true;
            if (ReferenceEquals(obj, null) || obj.GetType() != GetType()) return false;
            var other = (ShaderLabDeclaredElementBase)obj;
            return TreeOffset == other.TreeOffset && ShortName == other.ShortName && SourceFile.Equals(other.SourceFile);
        }

        public override int GetHashCode() => Hash.Combine(SourceFile.GetHashCode(), ShortName.GetHashCode());
    }
}
