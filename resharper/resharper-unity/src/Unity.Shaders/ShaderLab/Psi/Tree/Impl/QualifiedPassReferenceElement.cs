#nullable enable
using JetBrains.ReSharper.Plugins.Unity.Common.Psi.Resolve;
using JetBrains.ReSharper.Plugins.Unity.Shaders.ShaderLab.Psi.Resolve;
using JetBrains.ReSharper.Psi;
using JetBrains.ReSharper.Psi.Resolve;
using JetBrains.ReSharper.Psi.Tree;
using JetBrains.ReSharper.Psi.Util;
using JetBrains.Util;

namespace JetBrains.ReSharper.Plugins.Unity.Shaders.ShaderLab.Psi.Tree.Impl
{
    internal partial class QualifiedPassReferenceElement
    {
        private static readonly ShaderReferenceOrigin ourShaderReferenceOrigin = new();
        private static readonly PassReferenceOrigin ourReferenceOrigin = new();
        
        private readonly ReferenceCollection myReferences;
        
        internal QualifiedPassReferenceElement()
        {
            var shaderReference = new ShaderReference<QualifiedPassReferenceElement>(this, ourShaderReferenceOrigin);
            myReferences = new ReferenceCollection(shaderReference, new TexturePassReference<QualifiedPassReferenceElement>(shaderReference, this, ourReferenceOrigin));
        }

        public override ReferenceCollection GetFirstClassReferences() => myReferences;
        
        private class ShaderReferenceOrigin : IReferenceOrigin<QualifiedPassReferenceElement>
        {
            private static StringSlice GetShaderNameSlice(ITreeNode node) => node.GetUnquotedTextSlice() is var unquotedName && unquotedName.TryGetSubstringBeforeLast('/', out var shaderNameSlice) ? shaderNameSlice : unquotedName;
            public string? GetReferenceName(QualifiedPassReferenceElement owner) => owner.QualifiedName is { } name ? GetShaderNameSlice(name).ToString() : null;
            public TreeTextRange GetReferenceNameRange(QualifiedPassReferenceElement owner) => owner.QualifiedName is { } name ? name.GetTreeTextRange(GetShaderNameSlice(name)) : TreeTextRange.InvalidRange;

            public IReference RenameFromReference(IReference fromReference, QualifiedPassReferenceElement owner, string newName, ISubstitution? substitution)
            {
                if (owner.QualifiedName is { } name) 
                    owner.SetStringLiteral(name, GetShaderNameSlice(name), newName);
                return fromReference;
            }
        }
        
        private class PassReferenceOrigin : IReferenceOrigin<QualifiedPassReferenceElement>
        {
            private static bool TryGetPassNameSlice(ITreeNode node, out StringSlice slice) => node.GetUnquotedTextSlice().TryGetSubstringAfterLast('/', out slice);
            public string? GetReferenceName(QualifiedPassReferenceElement owner) => owner.QualifiedName is { } name && TryGetPassNameSlice(name, out var slice) ? slice.ToString() : null;

            public TreeTextRange GetReferenceNameRange(QualifiedPassReferenceElement owner) => owner.QualifiedName is { } name&& TryGetPassNameSlice(name, out var slice) ? name.GetTreeTextRange(slice) : TreeTextRange.InvalidRange;

            public IReference RenameFromReference(IReference fromReference, QualifiedPassReferenceElement owner, string newName, ISubstitution? substitution)
            {
                if (owner.QualifiedName is { } name&& TryGetPassNameSlice(name, out var slice)) 
                    owner.SetStringLiteral(name, slice, newName);
                return fromReference;
            }
        }
    }
}