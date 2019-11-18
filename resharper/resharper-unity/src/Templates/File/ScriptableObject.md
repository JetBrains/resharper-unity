---
guid: 1404A333-DA7C-47AC-8CB5-7C944DD1422D
image: UnityCSharp
type: File
reformat: True
shortenReferences: True
categories: unity
customProperties: Extension=cs, FileName=ScriptableObject, ValidateFileName=True
scopes: InUnityCSharpProject
parameterOrder: HEADER, (CLASS), (NAMESPACE), FILENAME, MENUNAME
HEADER-expression: fileheader()
CLASS-expression: getAlphaNumericFileNameWithoutExtension()
NAMESPACE-expression: fileDefaultNamespace()
FILENAME-expression: complete()
MENUNAME-expression: complete()
---

# Scriptable Object

```
$HEADER$namespace $NAMESPACE$ {
  [UnityEngine.CreateAssetMenu(fileName = "$FILENAME$", menuName = "$MENUNAME$", order = 0)]
  public class $CLASS$ : UnityEngine.ScriptableObject
  {
    $END$
  }
}
```
