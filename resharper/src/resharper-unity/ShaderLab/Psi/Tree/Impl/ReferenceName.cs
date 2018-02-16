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
using JetBrains.Util;

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
                var declaredElements = GetPropertyDeclaredElements(true);
                if (declaredElements.Count == 0)
                    return ResolveResultWithInfo.Unresolved;

                return ResolveUtil.CreateResolveResult(declaredElements);
            }

            public override string GetName()
            {
                return myOwner.Identifier?.Name ?? SharedImplUtil.MISSING_DECLARATION_NAME;
            }

            public override ISymbolTable GetReferenceSymbolTable(bool useReferenceName)
            {
                var declaredElements = GetPropertyDeclaredElements(useReferenceName);
                if (declaredElements.Count == 0)
                    return EmptySymbolTable.INSTANCE;

                return ResolveUtil.CreateSymbolTable(declaredElements, 0);
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

            public ISymbolTable GetCompletionSymbolTable()
            {
                return GetReferenceSymbolTable(false);
            }

            private IList<IDeclaredElement> GetPropertyDeclaredElements(bool useReferenceName)
            {
                if (!(myOwner.GetContainingFile() is IShaderLabFile file))
                    return EmptyList<IDeclaredElement>.InstanceList;

                var referenceName = myOwner.Identifier?.Name;
                if (string.IsNullOrEmpty(referenceName))
                    return EmptyList<IDeclaredElement>.InstanceList;

                var declaredElements = new List<IDeclaredElement>();
                if (file.Command?.Value is IShaderValue shaderValue)
                {
                    if (shaderValue.PropertiesCommand?.Value is IPropertiesValue propertiesValue)
                    {
                        foreach (var propertyDeclaration in propertiesValue.DeclarationsEnumerable)
                        {
                            if (useReferenceName)
                            {
                                // Note that both ShaderLab and Cg are case sensitive
                                var declarationName = propertyDeclaration?.Name?.GetText();
                                if (!string.Equals(referenceName, declarationName, StringComparison.InvariantCulture))
                                    continue;
                            }
                            declaredElements.Add(propertyDeclaration.DeclaredElement);
                        }
                    }
                }

                return declaredElements;
            }
        }
    }
}