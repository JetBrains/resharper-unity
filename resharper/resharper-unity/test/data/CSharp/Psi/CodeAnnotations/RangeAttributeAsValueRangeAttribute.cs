using System;
using System.Diagnostics;
using UnityEngine;

public class MyMonoBehaviour : MonoBehaviour
{
    [Range(1, 10)] public int intValue;
    [Range(1, 10)] public uint uintValue;
    [Range(1, 10)] public long longValue;
    [Range(1, 10)] public ulong ulongValue;
    [Range(1, 10)] public byte byteValue;
    [Range(1, 10)] public sbyte sbyteValue;
    [Range(1, 10)] public short shortValue;
    [Range(1, 10)] public ushort ushortValue;

    [Range(1.3f, 10.7f)] public int intWithFloatRange;

    [Range(1, 10)] private int nonSerialisedField;

    public void Update()
    {
        // Only the types that are implicitly converted into int will have
        // integer value analysis warnings
        if (intValue > 20) { }
        if (uintValue > 20) { }
        if (longValue > 20) { }
        if (ulongValue > 20) { }
        if (byteValue > 20) { }
        if (sbyteValue > 20) { }
        if (shortValue > 20) { }
        if (ushortValue > 20) { }

        if (nonSerialisedField > 20) { }

        if (intWithFloatRange < 1) { }
        if (intWithFloatRange == 0) { }
        if (intWithFloatRange == 1) { }
        if (intWithFloatRange == 2) { }
        if (intWithFloatRange > 10) { }
        if (intWithFloatRange == 10) { }
        if (intWithFloatRange == 11) { }
    }

    public void LateUpdate()
    {
        if (intValue > 5) { }
        if (uintValue > 5) { }
        if (longValue > 5) { }
        if (ulongValue > 5) { }
        if (byteValue > 5) { }
        if (sbyteValue > 5) { }
        if (shortValue > 5) { }
        if (ushortValue > 5) { }
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

