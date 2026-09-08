using System.ComponentModel;

using JetBrains.Annotations;

// ReSharper disable CheckNamespace
namespace System.Runtime.CompilerServices
{
    /// <summary>
    /// Reserved to be used by the compiler for tracking metadata.
    /// This class should not be used by developers in source code.
    /// </summary>
    [EditorBrowsable(EditorBrowsableState.Never)]
    [UsedImplicitly]
    internal static class IsExternalInit;
}