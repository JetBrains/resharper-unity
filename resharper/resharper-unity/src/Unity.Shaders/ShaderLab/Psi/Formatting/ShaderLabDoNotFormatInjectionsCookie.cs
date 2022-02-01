using System;
using JetBrains.Diagnostics;

namespace JetBrains.ReSharper.Plugins.Unity.Shaders.ShaderLab.Psi.Formatting
{
    public class ShaderLabDoNotFormatInjectionsCookie : IDisposable
    {
        [ThreadStatic] public static bool IsInjectionFormatterSuppressed;

        public ShaderLabDoNotFormatInjectionsCookie()
        {
            Assertion.Assert(!IsInjectionFormatterSuppressed, "!IsInjectionFormatterSuppressed, Reentrancy is not expected");
            IsInjectionFormatterSuppressed = true;
            
        }

        public void Dispose()
        {
            IsInjectionFormatterSuppressed = false;
        }
    }
}