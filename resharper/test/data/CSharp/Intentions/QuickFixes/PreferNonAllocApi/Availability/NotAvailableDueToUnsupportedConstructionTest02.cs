using UnityEngine;
using System;

namespace DefaultNamespace
{
    public class NotAvailableDueToUnsupportedConstructionTest02
    {
        public void SomeMethod()
        {
            RaycastHit[] data;
            var length = (data = Physics.RaycastAll(new Ray())).Length;

            Console.WriteLine(length);
            Console.WriteLine(data.Length);
        }
    }
}