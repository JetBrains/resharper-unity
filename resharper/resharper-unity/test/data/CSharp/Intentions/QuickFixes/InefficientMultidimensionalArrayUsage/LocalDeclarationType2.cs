using UnityEngine;

namespace DefaultNamespace
{
    public class FieldGenerationWithRespectToCodeStyleTest : MonoBehaviour
    {
        public void Update()
        {
            int[,] test = new int[2,2];

            test[0, 0] = 5;
            test[test[0,{caret} 1], test[0, test[0,1]]] = 5;
        }
    }
}