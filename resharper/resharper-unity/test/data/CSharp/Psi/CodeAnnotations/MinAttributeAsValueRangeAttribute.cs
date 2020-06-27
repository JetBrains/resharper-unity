using System;
using System.Diagnostics;
using UnityEngine;

public class MyMonoBehaviour : MonoBehaviour
{
    [Min(100)] public int intValue;
    [Min(100)] public uint uintValue;
    [Min(100)] public long longValue;
    [Min(100)] public ulong ulongValue;
    [Min(100)] public byte byteValue;
    [Min(100)] public sbyte sbyteValue;
    [Min(100)] public short shortValue;
    [Min(100)] public ushort ushortValue;

    [Min(1.3f)] public int intWithFloatRange;

    [Min(100)] private int nonSerialisedField;

    public void Update()
    {
        // Only the types that are implicitly converted into int will have
        // integer value analysis warnings
        if (intValue < 100) { }
        if (uintValue < 100) { }
        if (longValue < 100) { }
        if (ulongValue < 100) { }
        if (byteValue < 100) { }
        if (sbyteValue < 100) { }
        if (shortValue < 100) { }
        if (ushortValue < 100) { }

        if (nonSerialisedField < 100) { }

        if (intWithFloatRange < 1) { }
        if (intWithFloatRange == 0) { }
        if (intWithFloatRange == 1) { }
        if (intWithFloatRange == 2) { }
    }

    public void LateUpdate()
    {
        if (intValue > 500) { }
        if (uintValue > 500) { }
        if (longValue > 500) { }
        if (ulongValue > 500) { }
        if (byteValue > 500) { }
        if (sbyteValue > 500) { }
        if (shortValue > 500) { }
        if (ushortValue > 500) { }
    }
}

// We contribute a custom attribute instance when we see RangeAttribute on an
// int field. In a production context, this attribute instance resolves with
// JetBrains.Annotations loaded from the install directory by the external
// annotations module. This doesn't happen in a test context, so we have to
// provide the class here in the test, so there's something to resolve against.
// Other annotation based tests can work because Unity.Engine includes an old
// subset of JetBrains.Annotations
//
// ReSharper disable All
namespace JetBrains.Annotations
{
  [AttributeUsage(AttributeTargets.Method | AttributeTargets.Property | AttributeTargets.Field | AttributeTargets.Parameter | AttributeTargets.Delegate, AllowMultiple = true)]
  [Conditional("JETBRAINS_ANNOTATIONS")]
  public sealed class ValueRangeAttribute : Attribute
  {
    public object From { get; }

    public object To { get; }

    public ValueRangeAttribute(long from, long to)
    {
      this.From = (object) from;
      this.To = (object) to;
    }

    public ValueRangeAttribute(ulong from, ulong to)
    {
      this.From = (object) from;
      this.To = (object) to;
    }

    public ValueRangeAttribute(long value) => this.From = this.To = (object) value;

    public ValueRangeAttribute(ulong value) => this.From = this.To = (object) value;
  }
}
// ReSharper enable All

