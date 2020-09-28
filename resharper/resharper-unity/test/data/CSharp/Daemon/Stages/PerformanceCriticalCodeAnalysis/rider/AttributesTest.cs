using System;
using System.Diagnostics.CodeAnalysis;
using UnityEngine;

namespace DefaultNamespace
{
    public class AttributesNamesTest : MonoBehaviour
    {
        private void LateUpdate()
        {
            SecondMethod();
        }

        [SuppressMessage("ReSharper", "Cheap.Method")]
        private void SecondMethod()
        {
        }
    }
}