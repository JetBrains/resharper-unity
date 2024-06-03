using UnityEngine;

namespace DefaultNamespace
{
    [CreateAssetMenu(fileName = "Test.asset", menuName = "Create Test asset", order = 0)]
    public class TestScriptableObject : ScriptableObject
    {
        public int SOVariable;
        public string SOText;
        public ScriptableObject SOScriptableObject;
        
    }
}