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

  // Should be unused - invalid parameters!
  public void OnAudioFilterRead()
  {
  }

  // Should be unused - invalid return type!
  public bool FixedUpdate()
  {
      return true;
  }

  // Should be unused - invalid static modifier!
  public static void LateUpdate()
  {
  }
}
