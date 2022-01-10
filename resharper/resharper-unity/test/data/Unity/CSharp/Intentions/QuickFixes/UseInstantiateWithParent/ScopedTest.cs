using UnityEngine;

namespace DefaultNamespace
{
    public class SimpleTest
    {
        public void Test(Transform t, GameObject root)
        {
            GameObject go = (GameObject) GameObject.Instantiate(t.gameObject);
            go.transform.SetPar{caret}ent(root.transform);

            GameObject go2 = (GameObject) GameObject.Instantiate(t.gameObject);
            go2.transform.parent = root.transform;
        }

        public void Test2(Transform t, GameObject root)
        {
            GameObject go = (GameObject) GameObject.Instantiate(t.gameObject);
            go.transform.SetParent(root.transform, false);
        }
    }

    public class SimpleTest2
    {
        public void Test2(Transform t, GameObject root)
        {
            GameObject go = (GameObject) GameObject.Instantiate(t.gameObject);
            go.transform.SetParent(root.transform);
    }
}
