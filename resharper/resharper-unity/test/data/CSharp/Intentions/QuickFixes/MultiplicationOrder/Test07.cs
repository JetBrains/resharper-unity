using UnityEngine;

namespace DefaultNamespace
{
    public class Test02 : MonoBehaviour
    {
        public void Update()
        {
            Vector2[] calculatedPositions = {};
            Vector2 newPos = (calculatedPositions[0] - (Vector2) this.transform.localPosition) * {caret}10f *
                            Time.deltaTime;
        }
    }
}