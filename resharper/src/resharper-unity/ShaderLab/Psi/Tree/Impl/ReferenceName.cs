using JetBrains.Annotations;
using JetBrains.ReSharper.Psi;
using JetBrains.ReSharper.Psi.ExtensionsAPI;
using JetBrains.ReSharper.Psi.ExtensionsAPI.Resolve;
using JetBrains.ReSharper.Psi.Resolve;
using JetBrains.ReSharper.Psi.Tree;
using JetBrains.ReSharper.Resources.Shell;

namespace JetBrains.ReSharper.Plugins.Unity.ShaderLab.Psi.Tree.Impl
{
    internal partial class ReferenceName
    {
        private IReference myReference;

        public override ReferenceCollection GetFirstClassReferences()
        {
            return new ReferenceCollection(Reference);
        }

        public IReference Reference
        {
            get
            {
                if (myReference == null)
                {
                    lock (this)
                    {
                        if (myReference == null)
                            myReference = new PropertyReference(this);
                    }
                }

                return myReference;
            }
        }

        private class PropertyReference : TreeReferenceBase<ReferenceName>
        {
            public PropertyReference([NotNull] ReferenceName owner)
                : base(owner)
            {
            }

            public override ResolveResultWithInfo ResolveWithoutCache()
            {
                // Look for the item in the properties
                return ResolveResultWithInfo.Unresolved;
            }

            public override string GetName()
            {
                return myOwner.Identifier?.Name ?? SharedImplUtil.MISSING_DECLARATION_NAME;
            }

            public override ISymbolTable GetReferenceSymbolTable(bool useReferenceName)
            {
                return EmptySymbolTable.INSTANCE;
            }

            public override TreeTextRange GetTreeTextRange()
            {
                return myOwner.Identifier?.GetTreeTextRange() ?? TreeTextRange.InvalidRange;
            }

            public override IReference BindTo(IDeclaredElement element)
            {
                using (WriteLockCookie.Create(myOwner.IsPhysical()))
                {
                    var shaderLabIdentifier = new ShaderLabIdentifier();
                    shaderLabIdentifier.AddChild(new Identifier(element.ShortName));
                    if (myOwner.Identifier != null)
                        LowLevelModificationUtil.ReplaceChildRange(myOwner.Identifier, myOwner.Identifier,
                            shaderLabIdentifier);
                    else
                        LowLevelModificationUtil.AddChild(myOwner.Identifier, shaderLabIdentifier);
                }

                return myOwner.Reference;
            }

            public override IReference BindTo(IDeclaredElement element, ISubstitution substitution)
            {
                return BindTo(element);
            }

            public override IAccessContext GetAccessContext()
            {
                return new DefaultAccessContext(myOwner);
            }
        }
    }
}