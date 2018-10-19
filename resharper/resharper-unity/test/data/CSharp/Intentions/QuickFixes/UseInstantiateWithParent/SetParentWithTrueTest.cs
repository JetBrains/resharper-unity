using UnityEngine;

namespace DefaultNamespace
{
    public class SetParentWithTrueTest
    {
        public void Test(Transform t, GameObject root)
        {
            GameObject go = (GameObject) GameObject.Instantiate(t.gameObject);
            go.transform.SetPar{caret}ent(root.transform, true);
        }
    }
}