using UnityEngine;

namespace DefaultNamespace
{
    public class FieldGenerationWithRespectToCodeStyleTest : MonoBehaviour
    {
        public void Update()
        {
            var t{caret}est = new int[,] {{1, 2}, {1, 3}};

            test[0, 0] = 5;
            test[test[0, 1], test[0, test[0,1]]] = 5;
        }
    }
}