// ${RUN:1}
using UnityEngine;
using UnityEngine.Serialization;

public class Test : MonoBehaviour
{
  public void Test(GameObject a, GameObject b)
  {
    if (a {caret}!= null)
      Debug.Log("Destroyed");
  }
}

