using System;
using UnityEngine;

namespace DefaultNamespace
{
  public class CommonTest : MonoBehaviour
  {
    private Action action = AnotherMethodName;

    private static void AnotherMethodName()
    {
      var kek = new CommonTest();
      kek.AnotherMethodNameA();
    }
    private void AnotherMethodNameA()
    {
      GetComponent<CommonTest>();
    }
    
    private void Update()
    {
      action();
    }
  }
}