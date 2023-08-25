using UnityEngine;
using UnityEngine.UIElements;

class MyElement : VisualElement
{
    public new class UxmlFactory : UxmlFactory<MyElement, UxmlTraits> { }

    // Add the two custom UXML attributes.
    public new class UxmlTraits : VisualElement.UxmlTraits
    {
        UxmlStringAttributeDescription m_String =
            new UxmlStringAttributeDescription { name = "my-string", defaultValue = "default_value" };
        UxmlIntAttributeDescription m_Int =
            new UxmlIntAttributeDescription { name = "my-int", defaultValue = 2 };
            
        public override void Init(VisualElement ve, IUxmlAttributes bag, CreationContext cc)
        {
            base.Init(ve, bag, cc);
            var ate = ve as MyElement;

            ate.myString = m_String.GetValueFromBag(bag, cc);
            ate.myInt = m_Int.GetValueFromBag(bag, cc);
        }
    }
        
    // Must expose your element class to a { get; set; } property that has the same name 
    // as the name you set in your UXML attribute description with the camel case format
    public string myString { get; set; }
    public int myInt { get; set; }
}