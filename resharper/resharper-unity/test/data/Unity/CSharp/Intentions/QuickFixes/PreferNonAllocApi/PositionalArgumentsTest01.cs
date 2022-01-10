using UnityEngine;
 
namespace DefaultNamespace
{
    public class PositionalArgumentsTest01
    {
        public void Test()
        {
            Physics.Overl{caret}apBox(center: Vector3.zero, halfExtents: new Vector3(1, 1, 1), orientation: Quaternion.identity, layerMask: 0, queryTriggerInteraction: QueryTriggerInteraction.Collide); 
        }
    }
}