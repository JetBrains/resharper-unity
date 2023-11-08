#nullable enable
using System;
using System.Runtime.CompilerServices;

namespace JetBrains.ReSharper.Plugins.Unity.Common.Utils;

public readonly struct DelegateValueAction<T> : IValueAction<T>
{
    private readonly Action<T> myAction;

    public DelegateValueAction(Action<T> action) => myAction = action;

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public void Invoke(T arg) => myAction.Invoke(arg);

    public static implicit operator DelegateValueAction<T>(Action<T> action) => new(action);
}