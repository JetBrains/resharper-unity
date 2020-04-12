using UnityEngine;
using JetBrains.Annotations;

public class Base : MonoBehaviour
{
  protected virtual void Update()
  {
  }
}

public class Derived : Base
{
  Up{caret}
}
