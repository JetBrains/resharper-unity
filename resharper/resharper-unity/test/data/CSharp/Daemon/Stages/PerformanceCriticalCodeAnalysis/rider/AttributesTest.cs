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

        [ExpensiveMethod]
        private void SecondMethod()
        {
        }
    }
}

internal class ExpensiveMethodAttribute : Attribute
{
}

internal class FrequentlyCalledMethodAttribute : Attribute
{
}