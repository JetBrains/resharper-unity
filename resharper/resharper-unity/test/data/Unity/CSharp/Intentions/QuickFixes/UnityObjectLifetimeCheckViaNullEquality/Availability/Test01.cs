using UnityEngine;
using UnityEngine.Serialization;

public class Test : MonoBehaviour
{
  public void Test(GameObject a, GameObject b)
  {
    if(a == null) //Must be 2 quickfixes available
      Debug.Log("Equals");
    
    if(b != null) //Must be 2 quickfixes available
      Debug.Log("Not Equals");
  }
}
