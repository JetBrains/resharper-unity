using System.Diagnostics.CodeAnalysis;
using UnityEngine;

namespace Performance
{
    public class Everything : MonoBehaviour
    {
        private void Upd{off}ate()//no
        {
            Fupdate();
        }

        private void Fup{on}date()//yes
        {
        }

        private void Dup{off}date()//no
        {
            GetComponent<int>();
        }
        
        private void Late{off}Update()//no
        {
            Flate();
        }
        
        private void Fl{off}ate()//no
        {
            Dlate();
        }

        private void D{off}late()//no
        {
            GetComponent<int>();
        }

        private void FixedUp{off}date()//no
        {
            GetComponent<int>();
            DFixed();
            ManyFixed();
            FFixed();
        }

        private void FFi{off}xed()//no
        {
            GetComponent<int>();
        }

        [SuppressMessage("ReSharper", "Cheap.Method")]
        private void DFi{off}xed()//no
        {
        }
        
        [SuppressMessage("Bla", "Cheap.Method")]
        [SuppressMessage("Resharper", "Cheap.Met111hod")]
        private void ManyFixe{on}d()//yes
        {
        }

    }
}