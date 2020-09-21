using System;
using UnityEngine;
using JetBrains.Annotations;

namespace DefaultNamespace
{
    public class AttributesNamesTest : MonoBehaviour
    {
        private void LateUpdate()
        {
            SecondMethod();
        }

        [PublicAPI("Expensive method")]
        private void SecondMethod()
        {
        }
    }
}

namespace JetBrains.Annotations
{
  public sealed class PublicAPIAttribute : Attribute
  {
    public PublicAPIAttribute()
    {
    }

    public PublicAPIAttribute(string comment) => this.Comment = comment;

    public string Comment { get; }
  }
}