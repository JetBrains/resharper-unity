#nullable disable

using System;
using System.Collections.Generic;
using System.Text;
using System.Xml;
using JetBrains.DocumentModel;
using JetBrains.ReSharper.Psi;
using JetBrains.ReSharper.Psi.ExtensionsAPI.Tree;
using JetBrains.ReSharper.Psi.Modules;
using JetBrains.ReSharper.Psi.Tree;
using JetBrains.Text;

// ReSharper disable UnassignedGetOnlyAutoProperty

namespace JetBrains.ReSharper.Plugins.Tests.UnityTestComponents
{
    public partial class DotsElementsSuperTypeDeclaredInOtherPartSuppressorMock
    {
        private class ClassLikeDeclarationFromGeneratedFileMock : ITypeDeclaration
        {
            private readonly IPsiSourceFile myPsiSourceFile;

            public ClassLikeDeclarationFromGeneratedFileMock(ITypeDeclaration declaration)
            {
                myPsiSourceFile = new SourceGeneratedFileMock(declaration.GetSourceFile()!);
            }

            public ITreeNode Parent { get; }
            public ITreeNode FirstChild { get; }
            public ITreeNode LastChild { get; }
            public ITreeNode NextSibling { get; }
            public ITreeNode PrevSibling { get; }
            public NodeType NodeType { get; }
            public PsiLanguageType Language { get; }
            public IPsiServices GetPsiServices()
            {
                throw new NotImplementedException();
            }

            public IPsiModule GetPsiModule()
            {
                throw new NotImplementedException();
            }

            public IPsiSourceFile GetSourceFile()
            {

                return myPsiSourceFile;
            }

            public ReferenceCollection GetFirstClassReferences()
            {
                throw new NotImplementedException();
            }

            public void ProcessDescendantsForResolve(IRecursiveElementProcessor processor)
            {
                throw new NotImplementedException();
            }

            public TTreeNode GetContainingNode<TTreeNode>(bool returnThis = false) where TTreeNode : ITreeNode
            {
                throw new NotImplementedException();
            }

            public bool Contains(ITreeNode other)
            {
                throw new NotImplementedException();
            }

            public bool IsPhysical()
            {
                throw new NotImplementedException();
            }

            public bool IsValid()
            {
                throw new NotImplementedException();
            }

            public bool IsFiltered()
            {
                throw new NotImplementedException();
            }

            public DocumentRange GetNavigationRange()
            {
                throw new NotImplementedException();
            }

            public TreeOffset GetTreeStartOffset()
            {
                throw new NotImplementedException();
            }

            public int GetTextLength()
            {
                throw new NotImplementedException();
            }

            public StringBuilder GetText(StringBuilder to)
            {
                throw new NotImplementedException();
            }

            public IBuffer GetTextAsBuffer()
            {
                throw new NotImplementedException();
            }

            public string GetText()
            {
                throw new NotImplementedException();
            }

            public ITreeNode FindNodeAt(TreeTextRange treeRange)
            {
                throw new NotImplementedException();
            }

            public IReadOnlyCollection<ITreeNode> FindNodesAt(TreeOffset treeOffset)
            {
                throw new NotImplementedException();
            }

            public ITreeNode FindTokenAt(TreeOffset treeTextOffset)
            {
                throw new NotImplementedException();
            }

            public NodeUserData UserData { get; }
            public NodeUserData PersistentUserData { get; }
            public XmlNode GetXMLDoc(bool inherit)
            {
                throw new NotImplementedException();
            }

            public string CLRName { get; }
            public IEnumerable<IDeclaredType> SuperTypes { get; }
            public ITypeElement DeclaredElement { get; }
            public IReadOnlyList<ITypeDeclaration> NestedTypeDeclarations { get; }
            public IEnumerable<ITypeDeclaration> NestedTypeDeclarationsEnumerable { get; }
            public IReadOnlyList<ITypeMemberDeclaration> MemberDeclarations { get; }

            IDeclaredElement IDeclaration.DeclaredElement => DeclaredElement;

            public string DeclaredName { get; }
            public void SetName(string name)
            {
                throw new NotImplementedException();
            }

            public TreeTextRange GetNameRange()
            {
                throw new NotImplementedException();
            }

            public bool IsSynthetic()
            {
                throw new NotImplementedException();
            }

            public IReadOnlyList<ITypeDeclaration> TypeDeclarations { get; }
            public IEnumerable<ITypeDeclaration> TypeDeclarationsEnumerable { get; }
        }
    }
}