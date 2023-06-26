#nullable enable
using JetBrains.ReSharper.Psi;
using JetBrains.ReSharper.Psi.Resolve;
using JetBrains.ReSharper.Psi.Tree;

namespace JetBrains.ReSharper.Plugins.Unity.Common.Psi.Resolve
{
    public interface IReferenceOrigin<in TOwner> where TOwner : ITreeNode
    {
        string? GetReferenceName(TOwner owner);
        TreeTextRange GetReferenceNameRange(TOwner owner);
        IReference RenameFromReference(IReference fromReference, TOwner owner, string newName, ISubstitution? substitution);
    }
}