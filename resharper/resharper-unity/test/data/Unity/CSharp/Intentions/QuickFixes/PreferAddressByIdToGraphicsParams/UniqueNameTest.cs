using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class NewBehaviourScript : MonoBehaviour
{
  public class UniqueId
  {
  }
         
  void Update()
  {
    Material m;
    m.GetColor("Unique{caret}Id");
  }
}