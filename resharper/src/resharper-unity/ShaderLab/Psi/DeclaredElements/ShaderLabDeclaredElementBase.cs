using System.Collections.Generic;
using System.Xml;
using JetBrains.ReSharper.Plugins.Unity.ShaderLab.Psi.Tree;
using JetBrains.ReSharper.Psi;
using JetBrains.ReSharper.Psi.Files;
using JetBrains.ReSharper.Psi.Tree;
using JetBrains.Util;
using JetBrains.Util.DataStructures;

namespace JetBrains.ReSharper.Plugins.Unity.ShaderLab.Psi.DeclaredElements
{
    public abstract class ShaderLabDeclaredElementBase : IShaderLabDeclaredElement
    {
        private readonly IPsiSourceFile mySourceFile;

        protected ShaderLabDeclaredElementBase(string shortName, IPsiSourceFile sourceFile, int treeOffset)
        {
            mySourceFile = sourceFile;
            TreeOffset = treeOffset;
            ShortName = shortName;
        }

        protected int TreeOffset { get; }

        public IPsiServices GetPsiServices()
        {
            return mySourceFile.GetPsiServices();
        }

        public IList<IDeclaration> GetDeclarations()
        {
            if (!(mySourceFile.GetPrimaryPsiFile() is IShaderLabFile psi))
                return EmptyList<IDeclaration>.InstanceList;

            var node = psi.FindNodeAt(TreeTextRange.FromLength(new TreeOffset(TreeOffset), 1));
            while (node != null && !(node is IDeclaration))
                node = node.Parent;
            if (node == null)
                return EmptyList<IDeclaration>.Instance;

            return new[] {(IDeclaration) node};
        }

        public IList<IDeclaration> GetDeclarationsIn(IPsiSourceFile sourceFile)
        {
            if (mySourceFile == sourceFile)
                return GetDeclarations();
            return EmptyList<IDeclaration>.Instance;
        }

        public abstract DeclaredElementType GetElementType();

        public XmlNode GetXMLDoc(bool inherit) => null;
        public XmlNode GetXMLDescriptionSummary(bool inherit) => null;
        public bool IsValid() => true;
        public bool IsSynthetic() => false;

        public HybridCollection<IPsiSourceFile> GetSourceFiles()
        {
            return new HybridCollection<IPsiSourceFile>(mySourceFile);
        }

        public bool HasDeclarationsIn(IPsiSourceFile sourceFile)
        {
            return mySourceFile == sourceFile;
        }

        public string ShortName { get; }

        // Note that ShaderLab variable references and Cg parameters are case sensitive
        public bool CaseSensitiveName => true;

        // ReSharper disable once AssignNullToNotNullAttribute
        public PsiLanguageType PresentationLanguage => ShaderLabLanguage.Instance;
    }
}