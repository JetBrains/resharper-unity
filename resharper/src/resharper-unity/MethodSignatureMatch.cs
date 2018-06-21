using System;

namespace JetBrains.ReSharper.Plugins.Unity
{
    [Flags]
    public enum MethodSignatureMatch
    {
        NoMatch = -1,
        ExactMatch = 0,
        IncorrectStaticModifier = 2,
        IncorrectParameters = 4,
        IncorrectReturnType = 8,
        IncorrectTypeParameters = 16
    }
}