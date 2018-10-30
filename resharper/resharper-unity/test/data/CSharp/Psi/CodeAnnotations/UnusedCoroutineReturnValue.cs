using System.Collections;
using UnityEngine;

public class Foo : MonoBehaviour
{
    public void Update()
    {
        StartCoroutine(DoSomething(1, 2));
        DoSomething(2, 3);

        StartCoroutine(DoSomethingIterator(1, 2));
        DoSomethingIterator(2, 3);
    }

    public IEnumerator DoSomething(int i, int j)
    {
        return null;
    }

    public IEnumerator DoSomethingIterator(int i, int j)
    {
        yield return null;
    }
}
