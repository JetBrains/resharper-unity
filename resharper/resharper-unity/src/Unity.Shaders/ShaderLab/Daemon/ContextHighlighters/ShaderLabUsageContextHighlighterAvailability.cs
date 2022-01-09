using JetBrains.Annotations;
using JetBrains.Application;
using JetBrains.ReSharper.Psi;

namespace JetBrains.ReSharper.Plugins.Unity.ShaderLab.Daemon.ContextHighlighters
{
    // ReSharper disable once ClassWithVirtualMembersNeverInherited.Global - derived in Rider
    [ShellComponent]
    public class ShaderLabUsageContextHighlighterAvailability
    {
        // ReSharper disable once UnusedParameter.Global
        public virtual bool IsAvailable([NotNull] IPsiSourceFile psiSourceFile)
        {
            // Always available in ReSharper - R# doesn't have an option for this,
            // and actually lets Visual Studio do it for C#. It only does it for
            // C# files that are non user files (e.g. decompiled files).
            // Rider has an option, so we have a derived class that checks that.
            return true;
        }
    }
}