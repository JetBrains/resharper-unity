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
        
        // 1ReSharper restore Unity.ExpensiveCode
        // ReSharper 1 restore Unity.ExpensiveCode
        // ReSharper restore 23 Unity.ExpensiveCode
        // ReSharper restore Unity.ExpensiveCodee
        private void ManyFixe{off}d()//no
        {
            FFFF3();
        }
        
        private void {on}FFFF3()//yes
        {
            FFFF4();
        }

        private void FFFF4{on}()//yes
        {

        }
        
        // ReSharper disable Unity.PerformanceAnalysis
        private void Cro{off}ss()//no
        {
        }

    }
}