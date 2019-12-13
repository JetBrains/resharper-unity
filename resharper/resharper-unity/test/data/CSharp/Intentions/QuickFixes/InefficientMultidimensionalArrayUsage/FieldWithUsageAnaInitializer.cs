using UnityEngine;

namespace DefaultNamespace
{
    public class FieldGenerationWithRespectToCodeStyleTest : MonoBehaviour
    {
        int[,] test = new i{caret}nt[2,2] {{0, 1}, {1, 2}};

        public void Update()
        {

            test[0, 0] = 5;
            test[test[0, 1], test[0, test[0,1]]] = 5;
        }
    }
}