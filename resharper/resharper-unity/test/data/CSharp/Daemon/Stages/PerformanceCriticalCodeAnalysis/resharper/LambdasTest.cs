using System;
using UnityEngine;

namespace DefaultNamespace
{
  public class CommonTest : MonoBehaviour
  {
    private Action action = AnotherMethodName;

    private static void AnotherMethodName()
    {
      var test = new CommonTest();
      test.AnotherMethodNameA();
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