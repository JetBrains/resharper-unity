// ReSharper disable Unity.RedundantEventFunction
using System;
using System.Collections;
using UnityEngine;

public class A : MonoBehaviour
{
  // Potential event handler - marked as in use
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

  // Should mark both parameters as in use
  public void OnRenderImage(RenderTexture src, RenderTexture dest)
  {
  }

  // Should mark collisionInfo as unused
  public void OnCollisionExit(Collision collisionInfo)
  {
  }

  // Should mark coll as in use
  public void OnCollisionExit2D(Collision2D coll)
  {
      Console.WriteLine(coll);
  }
}
