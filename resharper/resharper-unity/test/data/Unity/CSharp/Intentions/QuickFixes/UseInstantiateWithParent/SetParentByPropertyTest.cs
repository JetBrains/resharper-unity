using UnityEngine;

namespace DefaultNamespace
{
    public class SetParentByProperty
    {
        public void Test(Transform t, GameObject root)
        {
            GameObject go = (GameObject) GameObject.Instantiate(t.gameObject);
            go.transform.par{caret}ent = root.transform;
        }
    }
}