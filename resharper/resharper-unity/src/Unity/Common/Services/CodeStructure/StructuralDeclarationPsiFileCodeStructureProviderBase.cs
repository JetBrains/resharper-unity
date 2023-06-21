#nullable enable

using JetBrains.Application;
using JetBrains.Application.Threading;
using JetBrains.Diagnostics;
using JetBrains.ReSharper.Feature.Services.CodeStructure;
using JetBrains.ReSharper.Plugins.Unity.Common.Services.Tree;
using JetBrains.ReSharper.Psi.Tree;

namespace JetBrains.ReSharper.Plugins.Unity.Common.Services.CodeStructure
{
    /// <summary>
    /// Base class for code structure providers based on <see cref="IStructuralDeclaration"/>.
    /// It automatically traverses structure and adds <see cref="CodeStructureElement"/> for every <see cref="IStructuralDeclaration"/>.
    /// With <see cref="CreateDeclarationElement"/> override you may customize code structure building. 
    /// </summary>
    public abstract class StructuralDeclarationPsiFileCodeStructureProviderBase : IPsiFileCodeStructureProvider
    {
        public CodeStructureRootElement Build(IFile file, CodeStructureOptions options)
        {
            file.GetPsiServices().Locks.AssertReadAccessAllowed();
            file.GetPsiServices().Files.AssertAllDocumentAreCommitted();

            var root = new CodeStructureRootElement(file);
            foreach (var declaration in file.Children<IStructuralDeclaration>())
                AddDeclaration(root, declaration, options);
            return root;
        }

        private void AddDeclaration(CodeStructureElement parent, IStructuralDeclaration declaration, CodeStructureOptions options)
        {
            var element = CreateDeclarationElement(parent, declaration, options);
            Assertion.Assert(element == null || parent.Children.Contains(element) && element.Parent == parent, "Declaration element should be attached to parent");
            if (element != null)
            {
                Interruption.Current.CheckAndThrow();

                foreach (var childDeclaration in declaration.GetMemberDeclarations())
                    AddDeclaration(element, childDeclaration, options);
            }
        }

        /// <summary>
        /// Creates new <see cref="CodeStructureElement"/> from <paramref name="declaration"/>. You may override this method in your provider for code structure building customization.
        /// If method returns <c>null</c> then it won't continue child declarations processing, but you still may attach <see cref="CodeStructureElement"/> to <paramref name="parent"/>.
        /// It may be used to suppress further hierarchy processing or to define custom structure sorting order.
        /// </summary>
        protected virtual CodeStructureElement? CreateDeclarationElement(CodeStructureElement parent, IStructuralDeclaration declaration, CodeStructureOptions options) => new CodeStructureDeclarationElement(parent, declaration);
    }
}