using System;
using System.Collections.Generic;
using JetBrains.Annotations;
using JetBrains.ReSharper.Plugins.Unity.ShaderLab.Psi.Resolve;
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
        private IVariableReferenceReference myReference;

        public override ReferenceCollection GetFirstClassReferences()
        {
            return new ReferenceCollection(Reference);
        }

        public IVariableReferenceReference Reference
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

        private class PropertyReference : TreeReferenceBase<ReferenceName>, IVariableReferenceReference
        {
            public PropertyReference([NotNull] ReferenceName owner)
                : base(owner)
            {
            }

            public override ResolveResultWithInfo ResolveWithoutCache()
            {
                if (!(myOwner.GetContainingFile() is IShaderLabFile file))
                    return ResolveResultWithInfo.Unresolved;

                if (myOwner.Identifier?.Name == null)
                    return ResolveResultWithInfo.Unresolved;

                if (file.Command.Value is IShaderValue shaderValue)
                {
                    if (shaderValue.PropertiesCommand.Value is IPropertiesValue propertiesValue)
                    {
                        var name = myOwner.Identifier.Name;
                        var declaredElements = new List<IDeclaredElement>();
                        foreach (var propertyDeclaration in propertiesValue.DeclarationsEnumerable)
                        {
                            var declarationName = propertyDeclaration?.Name?.GetText();
                            // TODO: Is ShaderLab case sensitive or not?
                            // I suspect property references aren't, but Cg references are...
                            if (string.Equals(name, declarationName, StringComparison.InvariantCulture))
                                declaredElements.Add(propertyDeclaration.DeclaredElement);
                        }

                        if (declaredElements.Count > 1)
                            return new ResolveResultWithInfo(ResolveResultFactory.CreateResolveResult(declaredElements), ResolveErrorType.MULTIPLE_CANDIDATES);
                        if (declaredElements.Count == 1)
                            return new ResolveResultWithInfo(ResolveResultFactory.CreateResolveResult(declaredElements[0]), ResolveErrorType.OK);
                    }
                }

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