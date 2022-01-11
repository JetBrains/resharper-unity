using UnityEngine;

namespace CommentRootsTests
{
    public class CommentRootsTests : MonoBehaviour
    {

        // ReSharper restore Unity.PerformanceCriticalContext
        public static void F1()
        {
            F2();
            F3();
            GetComponent<int>();
        }
        
        // ReSharper restore Unity.ExpensiveCode
        public void F2()
        {
        }

        // ReSharper disable Unity.PerformanceAnalysis
        public void F3()
        {
            GetComponent<int>();
        }
    }
}