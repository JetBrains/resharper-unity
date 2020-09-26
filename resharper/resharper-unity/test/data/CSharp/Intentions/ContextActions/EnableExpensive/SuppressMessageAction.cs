using System.Diagnostics.CodeAnalysis;
using UnityEngine;

namespace Performance
{
    public class Everything : MonoBehaviour
    {

        private void Update()
        {
            ManyFixed();
        }
        [SuppressMessage("Bla", "Cheap.Method")]
        [SuppressMessage("Resharper", "Cheap.Met111hod")]
        private void ManyFixe{caret}d()
        {
        }

    }
}