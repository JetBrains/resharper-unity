---
guid: E7134AFA-22FA-439E-AC0B-F572EA75D5FC
image: UnityCSharp
type: File
reformat: True
shortenReferences: True
categories: unity
customProperties: Extension=cs, FileName=AuthoringBehaviour, ValidateFileName=True
scopes: UnityDotsScope
uitag: DOTS
parameterOrder: HEADER, (CLASS), (NAMESPACE)
HEADER-expression: fileheader()
CLASS-expression: getAlphaNumericFileNameWithoutExtension()
NAMESPACE-expression: fileDefaultNamespace()
---

# Authoring Mono Behaviour

```
$HEADER$namespace $NAMESPACE$ {
  public class $CLASS$ : UnityEngine.MonoBehaviour 
  {
    private class $CLASS$Baker : Unity.Entities.Baker<$CLASS$>
    {
      public override void Bake($CLASS$ authoring)
      {
      }
    }
  }
}
```
