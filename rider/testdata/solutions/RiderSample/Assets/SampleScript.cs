using System.Collections;
using System.Collections.Generic;
using System.Text.RegularExpressions;
using DefaultNamespace;
using UnityEngine;

public class SampleScript : MonoBehaviour
{
    //private static readonly Regex UnsafeCharsWindows = new Regex(@"[^A-Za-z0-9\_\-\.\:\,\/\@\\]");

    
    
    // Rider shows unique change right in the editor
    public int PrimitiveValue;
    public string Description;

    // For game objects & components too
    public GameObject UniqueGameObject;

    // Rider notifies about unchanged values. 
    public int UnchangedValues = 13;

    // Rider shows all changes from Unity Editor
    public int ChangeMe;

    // and navigates to change in Unity Editor
    public GameObject MyObject;

    public Test Test;
    
    public void AddSmth()
    {
        MyObject.AddComponent<SampleScript>();

        PrimitiveValue += 1 + PrimitiveValue;
        Description = Description;
        UniqueGameObject = UniqueGameObject;
        UnchangedValues = UnchangedValues;
        ChangeMe = ChangeMe;
    }
}
