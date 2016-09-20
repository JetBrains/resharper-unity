using UnityEngine;

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
}
