# UIElements

UIElements was introduced as experimental in 2017.1. The first proper version was 2018.3

* 2017.1
  * First introduced, in the `UnityEngine.Experimental.UIElements` namespace, in `UnityEngine.dll`
  * Editor support in `UnityEditor.Experimental.UIElements` namespace, in `UnityEditor.dll`
  * No support for UXML, but does support USS
  * [Documentation for 2017.1](https://docs.unity3d.com/2017.1/Documentation/Manual/UIElements.html)
* 2017.4
  * Types are still in `UnityEngine.Experimental.UIElements` namespace, but moved to `UnityEngine.UIElementsModule.dll`
  * Types forwarded from `UnityEngine.dll`
  * Includes APIs for UXML - no obvious API for loading
* 2018.2
  * Still in experimental namespace
  * Includes `UnityEditor.Experimental.UIElements.UxmlSchemaGenerator` in `UnityEditor.dll`
* 2018.3
  * Still in experimental namespace
  * First documentation for UXML + USS: [Documentation for 2018.3](https://docs.unity3d.com/2018.3/Documentation/Manual/UIElements.html)
* 2018.4
  * Still in experimental namespace
* 2019.1
  * Migrated out of experimental namespace. Now in `UnityEngine.UIElements` and `UnityEditor.UIElements`
