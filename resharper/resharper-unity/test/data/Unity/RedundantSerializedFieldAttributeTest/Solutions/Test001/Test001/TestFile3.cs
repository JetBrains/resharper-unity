using UnityEngine;

namespace Test001
{
    public class Foo : MonoBehaviour
    {
        [SerializeReference] private A bar = new B();
    }
}