using System;
using UnityEngine;
using System.Diagnostics.CodeAnalysis;

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