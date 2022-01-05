using JetBrains.ReSharper.Psi;

namespace JetBrains.ReSharper.Plugins.Unity.AsmDef.Psi.DeclaredElements
{
    public interface IAsmDefDeclaredElement : IDeclaredElement
    {
        IPsiSourceFile SourceFile { get; }
        int DeclarationOffset { get; }
    }
}