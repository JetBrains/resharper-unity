---
guid: 93FD2901-A77E-4914-ABE7-B3A8C6E28325
image: UnityCSharp
type: File
reformat: True
shortenReferences: True
categories: unity
customProperties: Extension=cs, FileName=VisualElement, ValidateFileName=True
scopes: InUnityCSharpProject
uitag: Unity Script
parameterOrder: HEADER, (CLASS), (NAMESPACE)
HEADER-expression: fileheader()
CLASS-expression: getAlphaNumericFileNameWithoutExtension()
NAMESPACE-expression: fileDefaultNamespace()
---

# Visual Element

```
$HEADER$namespace $NAMESPACE$ {
  [UnityEngine.UIElements.UxmlElement]
  public partial class $CLASS$ : UnityEngine.UIElements.VisualElement
  {
    [UnityEngine.UIElements.UxmlAttribute]
    public string title { get; set; }

    public $CLASS$()
    {
      $END$
    }
  }
}
```
