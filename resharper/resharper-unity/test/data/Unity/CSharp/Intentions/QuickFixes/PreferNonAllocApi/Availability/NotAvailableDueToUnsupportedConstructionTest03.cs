using UnityEngine;
using System;

namespace DefaultNamespace
{
    public class NotAvailableDueToUnsupportedConstructionTest03
    {
        public void SomeMethod()
        {
            if (((Physics.RaycastAll(new Ray()))).Length == 0)
            {
                return;
            }

            Console.WriteLine("Test");
        }
    }
}