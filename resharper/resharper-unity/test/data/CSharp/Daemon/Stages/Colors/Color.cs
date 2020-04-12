using UnityEngine;

namespace UnityTest
{
  public class Class1
  {
    public Class1()
    {
      var c1 = new Color(1, 0, 0);
      var c2 = new Color(0, 1, 1, 05.f);
      var c3 = new Color(10, 20, -2);

      var c4 = Color.red;
      var c5 = Color.green;

      var c6 = new Color(1, 0, 0, 0, 0);
      var c7 = new Color(0x1 / 2.0f, 0.5f, 0.1f);

      var c8 = Color.HSVToRGB(0, 1, 1); // Red
      var c9 = Color.HSVToRGB(0.333f, 1, 1); // Green
      var c10 = Color.HSVToRGB(0.667f, 1, 1); // Blue
      var c11 = Color.HSVToRGB(0.17f, 0, 0.50f); // 127 127 127

      var c12 = Color.HSVToRGB(0, 1, 1, false);

      var c13 = Color.HSVToRGB(0, 1, 1, 1, 1);
    }
  }
}
