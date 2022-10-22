using JetBrains.ReSharper.Psi;

namespace JetBrains.ReSharper.Plugins.Unity.InputActions.Psi.DeclaredElements
{
    public interface IInputActionsDeclaredElement : IDeclaredElement
    {
        IPsiSourceFile SourceFile { get; }
        int DeclarationOffset { get; }
    }
}