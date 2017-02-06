using UnityEngine;
using System.Collections;

public class A : MonoBehaviour
{
  public void UnusedMethod()
  {
  }

  private void UnusedPrivateMethod()
  {
  }

  // Should be used
  public void OnDestroy()
  {
  }

  // Should be used
  private void OnDisable()
  {
  }

  // Coroutine - should be used
  public IEnumerator Start()
  {
      return null;
  }
}
