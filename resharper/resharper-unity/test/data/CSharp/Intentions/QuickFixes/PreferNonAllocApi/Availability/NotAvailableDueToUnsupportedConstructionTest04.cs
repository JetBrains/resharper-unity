using UnityEngine;
using System;

namespace DefaultNamespace
{
    public class NotAvailableDueToUnsupportedConstructionTest04
    {
        public void SomeMethod()
        {
            int size = Physics.OverlapBox(Vector3.zero, new Vector3(1, 1, 1), Quaternion.identity, 0, QueryTriggerInteraction.Collide).Length, b = size;
            Console.WriteLine(size + b);
        }
    }
}