using UnityEngine;
using UnityEngine.Serialization;

public class Test : MonoBehaviour
{
  public void Update()
  {
    float a = 1.7f;
    float b = 4.2f;
    if(a == b) //Must be 2 quickfixes available
      Debug.Log("Equals");
      
    if(a != b) //Must be 2 quickfixes available
      Debug.Log("Not Equals");
      
      
    double c = 1.0;
    double d = 2.0;
    if(c == d)//Must only ONE quickfixe available
      Debug.Log("Equals");
    
    if(c != d)//Must only ONE quickfixe available
      Debug.Log("Not Equals");
  }
}
