using UnityEngine;

namespace DefaultNamespace
{
    public class Test02 : MonoBehaviour
    {
        public void Update()
        {
            var z = Vector2.down * (Vector2.down * 7f * {caret}10f * Vector2.down) * 5f * 6f;
        }
    }
}