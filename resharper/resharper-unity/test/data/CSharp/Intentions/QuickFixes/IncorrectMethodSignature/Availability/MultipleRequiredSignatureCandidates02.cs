// ${RUN:2}
using UnityEditor.ShortcutManagement;

public class Foo
{
  [Shortcut("id")]
  public void Do{caret}Method()
  {
  }
}
