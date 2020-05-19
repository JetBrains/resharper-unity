using UnityEngine;

namespace DefaultNamespace
{
    public class SomethingBetweenTest
    {
        public void Call()
        {
            
        }
        
        public void Test(Transform t, GameObject root)
        {
            GameObject go = (GameObject) GameObject.Instantiate(t.gameObject);
            Call();
            Call();
            Call();
            Call();
            Call();
            go.transform.SetPar{caret}ent(root.transform);
        }
    }
}