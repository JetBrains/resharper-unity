// ${RUN:1}
using UnityEngine;
using UnityEngine.Serialization;

public class Test : MonoBehaviour
{
  public void Test(GameObject a, GameObject b)
  {
    if (null !={caret} a)
      Debug.Log("Destroyed");
  }
}

