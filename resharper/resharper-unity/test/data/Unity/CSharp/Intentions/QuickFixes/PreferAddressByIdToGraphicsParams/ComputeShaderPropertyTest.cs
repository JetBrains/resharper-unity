using UnityEngine;

namespace JetBrains.ReSharper.Psi.CSharp.Tree
{
  public class ComputeShaderPropertyTest
  {
    public void Method(ComputeShader shader)
    {
      shader.SetInt("te{caret}st", 0);
    }
  }
}