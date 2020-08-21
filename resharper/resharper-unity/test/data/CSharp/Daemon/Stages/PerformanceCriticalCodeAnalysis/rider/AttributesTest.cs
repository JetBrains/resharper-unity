using System;
using UnityEngine;

namespace DefaultNamespace
{
    public class AttributesNames : MonoBehaviour
    {
        [FrequentlyCalledMethod]
        private void FirstMethod()
        {
            SecondMethod();
        }

        [PerformanceCharacteristicsHint]
        private void SecondMethod()
        {
        }
    }
}

internal class PerformanceCharacteristicsHintAttribute : Attribute
{
}

internal class FrequentlyCalledMethodAttribute : Attribute
{
}