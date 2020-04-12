using UnityEngine;

namespace DefaultNamespace
{
    public class BasicTest01
    {
        public void Test()
        {
            var result = Physics.Overl{caret}apBox(Vector3.zero, new Vector3(1, 1, 1), Quaternion.identity, 0,
                QueryTriggerInteraction.Collide);
        }
    }
}