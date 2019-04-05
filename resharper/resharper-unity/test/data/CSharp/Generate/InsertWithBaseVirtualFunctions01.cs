// ${KIND:Unity.EventFunctions}
// ${SELECT0:Update():System.Void}
using UnityEngine;

class Base : MonoBehaviour
{
  protected virtual void Update()
  {
  }
}

class Derived : Base
{
  {caret}
}
