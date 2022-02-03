using UnityEngine;

namespace UnityTest
{
  public class Class1
  {
    public Class1()
    {
      // Color32 doesn't have an overload that takes 3 parameters, but we recognise it because it's the same as Color(r,g,b)
      var c1 = new Color32(255, 0, 0);
      var c2 = new Color32(0, 255, 255, 127);
      var c3 = new Color32(10, 20, -2);

      var c4 = new Color32(255, 0, 0, 0, 0);
      var c5 = new Color32(0xFF / 2, 128, 128);

      Color32 c6 = new(200, 180, 190, 255);
      Color32 c7 = new(1, 3, 0.5f, 0.8f);
    }
  }
}
