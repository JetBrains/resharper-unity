#nullable enable
using JetBrains.ReSharper.Plugins.Unity.Common.Psi.Resolve;
using JetBrains.ReSharper.Plugins.Unity.Shaders.ShaderLab.Psi.Resolve;
using JetBrains.ReSharper.Psi;
using JetBrains.ReSharper.Psi.Resolve;
using JetBrains.ReSharper.Psi.Tree;
using JetBrains.ReSharper.Psi.Util;

namespace JetBrains.ReSharper.Plugins.Unity.Shaders.ShaderLab.Psi.Tree.Impl
{
    internal partial class ShaderReferenceElement
    {
        private static readonly ReferenceOrigin ourReferenceOrigin = new();
        private readonly IReference myReference;

        internal ShaderReferenceElement()
        {
            myReference = new ShaderReference<ShaderReferenceElement>(this, ourReferenceOrigin);
        }

        public override ReferenceCollection GetFirstClassReferences() => new(myReference);

        private class ReferenceOrigin : IReferenceOrigin<ShaderReferenceElement>
        {
            public string? GetReferenceName(ShaderReferenceElement owner) => owner.Name?.GetUnquotedText();

            public TreeTextRange GetReferenceNameRange(ShaderReferenceElement owner) => owner.Name?.GetUnquotedTreeTextRange() ?? TreeTextRange.InvalidRange;

            public IReference RenameFromReference(IReference fromReference, ShaderReferenceElement owner, string newName, ISubstitution? substitution)
            {
                owner.SetStringLiteral(owner.Name, newName);
                return fromReference;
            }
        }
    }
}
