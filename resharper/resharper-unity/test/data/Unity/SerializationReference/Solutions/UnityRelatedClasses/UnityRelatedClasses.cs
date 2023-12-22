using System;

namespace UnityEngine
{
    /// <summary>
    ///   <para>A that instructs Unity to serialize a field as a reference instead of as a value.</para>
    /// </summary>
    [AttributeUsage(AttributeTargets.Field)]
    public sealed class SerializeReference : Attribute
    {
    }

    [AttributeUsage(AttributeTargets.Class)]
    public class ExecuteInEditModeAttribute : Attribute
    {
    }

    public class MonoBehaviour
    {
    }
}