using UnityEngine;

namespace DefaultNamespace
{
    public class NotAvailableDueToUnsupportedConstructionTest01
    {
        private readonly RaycastHit[] myResult = Physics.RaycastAll(new Ray());

        public RaycastHit[] Get()
        {
            return myResult;
        }
    }
}