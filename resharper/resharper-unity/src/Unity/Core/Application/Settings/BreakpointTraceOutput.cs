using System;

namespace JetBrains.ReSharper.Plugins.Unity.Core.Application.Settings;

[Flags]
public enum BreakpointTraceOutput
{
    UnityOutput = 1 << 0,
    DebugConsole = 1 << 1,
    Both = UnityOutput | DebugConsole
}