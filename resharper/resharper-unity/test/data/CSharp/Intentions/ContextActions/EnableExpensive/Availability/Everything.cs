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
            Cross();
            FFixed();
        }

        private void FFi{off}xed()//no
        {
            GetComponent<int>();
        }

        // ReSharper restore Unity.ExpensiveCode
        private void DFi{off}xed()//no
        {
        }
        
        [SuppressMessage("Bla", "Cheap.Method")]
        [SuppressMessage("Resharper", "Cheap.Met111hod")]
        // 1ReSharper restore Unity.ExpensiveCode
        // ReSharper 1 restore Unity.ExpensiveCode
        // ReSharper restore 23 Unity.ExpensiveCode
        // ReSharper restore Unity.ExpensiveCodee
        private void ManyFixe{on}d()//yes
        {
        }
        
        // ReSharper disable Unity.PerformanceAnalysis
        private void Cro{off}ss()//yes
        {
        }

    }
}