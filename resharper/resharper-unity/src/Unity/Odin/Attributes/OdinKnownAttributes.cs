using System.Collections.Generic;
using JetBrains.Metadata.Reader.API;
using JetBrains.Metadata.Reader.Impl;

namespace JetBrains.ReSharper.Plugins.Unity.Odin.Attributes;

public static class OdinKnownAttributes
{
    public static readonly string OdinNamespace = "Sirenix.OdinInspector";
    
    public static readonly IClrTypeName BoxGroupAttribute = new ClrTypeName("Sirenix.OdinInspector.BoxGroupAttribute");
    public static readonly IClrTypeName ButtonGroupAttribute = new ClrTypeName("Sirenix.OdinInspector.ButtonGroupAttribute");
    public static readonly IClrTypeName FoldoutGroupAttribute = new ClrTypeName("Sirenix.OdinInspector.FoldoutGroupAttribute");
    public static readonly IClrTypeName HorizontalGroupAttribute = new ClrTypeName("Sirenix.OdinInspector.HorizontalGroupAttribute");
    public static readonly IClrTypeName VerticalGroupAttribute = new ClrTypeName("Sirenix.OdinInspector.VerticalGroupAttribute");
    public static readonly IClrTypeName ResponsiveButtonGroupAttribute = new ClrTypeName("Sirenix.OdinInspector.ResponsiveButtonGroupAttribute");
    public static readonly IClrTypeName TabGroupAttribute = new ClrTypeName("Sirenix.OdinInspector.TabGroupAttribute");
    public static readonly IClrTypeName ToggleGroupAttribute = new ClrTypeName("Sirenix.OdinInspector.ToggleGroupAttribute");
    public static readonly IClrTypeName TitleGroupAttribute = new ClrTypeName("Sirenix.OdinInspector.TitleGroupAttribute");

    // TODO support reference to toggle
    public static readonly IClrTypeName HideIfGroupAttribute = new ClrTypeName("Sirenix.OdinInspector.HideIfGroupAttribute");
    public static readonly IClrTypeName ShowIfGroupAttribute = new ClrTypeName("Sirenix.OdinInspector.ShowIfGroupAttribute");


    // Asset attributes
    public static readonly IClrTypeName AssetListAttribute = new ClrTypeName("Sirenix.OdinInspector.AssetListAttribute");
    public static readonly IClrTypeName AssetSelectorAttribute = new ClrTypeName("Sirenix.OdinInspector.AssetSelectorAttribute");
    public static readonly IClrTypeName FilePathAttribute = new ClrTypeName("Sirenix.OdinInspector.FilePathAttribute");
    public static readonly IClrTypeName FolderPathAttribute = new ClrTypeName("Sirenix.OdinInspector.FolderPathAttribute");

    // Color
    public static readonly IClrTypeName GUIColorAttribute = new ClrTypeName("Sirenix.OdinInspector.GUIColorAttribute");

    // Code Annotations
    
    public static readonly IClrTypeName MinValueAttribute = new ClrTypeName("Sirenix.OdinInspector.MinValueAttribute");
    public static readonly IClrTypeName MaxValueAttribute = new ClrTypeName("Sirenix.OdinInspector.MaxValueAttribute");
    public static readonly IClrTypeName MinMaxSliderAttribute = new ClrTypeName("Sirenix.OdinInspector.MinMaxSliderAttribute");
    public static readonly IClrTypeName ProgressBarAttribute = new ClrTypeName("Sirenix.OdinInspector.ProgressBarAttribute");
    public static readonly IClrTypeName PropertyRangeAttribute = new ClrTypeName("Sirenix.OdinInspector.PropertyRangeAttribute");
    public static readonly IClrTypeName WrapAttribute = new ClrTypeName("Sirenix.OdinInspector.WrapAttribute");

    // Members completion without $
    public static readonly IClrTypeName CustomValueDrawerAttribute = new ClrTypeName("Sirenix.OdinInspector.CustomValueDrawerAttribute");
    public static readonly IClrTypeName TypeFilterAttribute = new ClrTypeName("Sirenix.OdinInspector.TypeFilterAttribute");
    public static readonly IClrTypeName ValidateInputAttribute = new ClrTypeName("Sirenix.OdinInspector.ValidateInputAttribute");
    public static readonly IClrTypeName ValueDropdownAttribute = new ClrTypeName("Sirenix.OdinInspector.ValueDropdownAttribute");
    public static readonly IClrTypeName InlineButtonAttribute = new ClrTypeName("Sirenix.OdinInspector.InlineButtonAttribute");
    public static readonly IClrTypeName OnInspectorGUIAttribute = new ClrTypeName("Sirenix.OdinInspector.OnInspectorGUIAttribute");
    public static readonly IClrTypeName OnValueChangedAttribute = new ClrTypeName("Sirenix.OdinInspector.OnValueChangedAttribute");
    
    // first argument
    public static readonly IClrTypeName DisableIfAttribute = new ClrTypeName("Sirenix.OdinInspector.DisableIfAttribute");
    public static readonly IClrTypeName EnableIfAttribute = new ClrTypeName("Sirenix.OdinInspector.EnableIfAttribute");
    public static readonly IClrTypeName HideIfAttribute = new ClrTypeName("Sirenix.OdinInspector.HideIfAttribute");
    public static readonly IClrTypeName ShowIfAttribute = new ClrTypeName("Sirenix.OdinInspector.ShowIfAttribute");
    
    // second argument
    public static readonly IClrTypeName CustomContextMenuAttribute = new ClrTypeName("Sirenix.OdinInspector.CustomContextMenuAttribute");
    
    // first & second
    public static readonly IClrTypeName OnCollectionChangedAttribute = new ClrTypeName("Sirenix.OdinInspector.OnCollectionChangedAttribute");

    // serialization
    public static readonly IClrTypeName OdinSerializeAttribute = new ClrTypeName("Sirenix.Serialization.OdinSerializeAttribute");
 
    public static readonly IClrTypeName OdinSerializedMonoBehaviour = new ClrTypeName("Sirenix.OdinInspector.SerializedMonoBehaviour");
    public static readonly IClrTypeName OdinSerializedScriptableObject = new ClrTypeName("Sirenix.OdinInspector.SerializedScriptableObject");
    public static readonly IClrTypeName OdinSerializedBehaviour = new ClrTypeName("Sirenix.OdinInspector.SerializedBehaviour");
    public static readonly IClrTypeName OdinSerializedComponent = new ClrTypeName("Sirenix.OdinInspector.SerializedComponent");
    public static readonly IClrTypeName OdinSerializedStateMachineBehaviour = new ClrTypeName("Sirenix.OdinInspector.SerializedStateMachineBehaviour");
    public static readonly IClrTypeName OdinSerializedUnityObject= new ClrTypeName("Sirenix.OdinInspector.SerializedUnityObject");
  
    public static readonly IClrTypeName PropertyGroupAttribute = new ClrTypeName("Sirenix.OdinInspector.PropertyGroupAttribute");
    public static readonly IClrTypeName OdinDrawer = new ClrTypeName("Sirenix.OdinInspector.Editor.OdinDrawer");

    
    public static readonly Dictionary<IClrTypeName, string[]> AttributesWithMemberCompletion = new()
    {
        {CustomValueDrawerAttribute, new [] {"action"} },
        {TypeFilterAttribute, new [] {"filterGetter"} },
        {ValidateInputAttribute, new [] {"condition"} },
        {ValueDropdownAttribute, new [] {"valuesGetter"} },
        {InlineButtonAttribute, new [] {"action"} },
        {OnInspectorGUIAttribute, new [] {"action"} },
        {OnValueChangedAttribute, new [] {"action"} },
        
        {DisableIfAttribute, new [] {"condition"} },
        {EnableIfAttribute, new [] {"condition"} },
        {HideIfAttribute, new [] {"condition"} },
        {ShowIfAttribute, new [] {"condition"} },
        
        {CustomContextMenuAttribute, new [] {"action"} },
        
        {OnCollectionChangedAttribute, new [] {"before", "after"} },
    };
    
    
    public static readonly Dictionary<IClrTypeName, string> LayoutAttributes = new()
    {
        {BoxGroupAttribute, "group"},
        {ButtonGroupAttribute, "group"},
        {HorizontalGroupAttribute, "group"},
        {ResponsiveButtonGroupAttribute, "group"},
        {VerticalGroupAttribute, "groupId"},
        {FoldoutGroupAttribute, "groupName"},
        
        {TabGroupAttribute, "group"},
        
        {ToggleGroupAttribute, "toggleMemberName"},
        {TitleGroupAttribute, "title"},
        
        {HideIfGroupAttribute, "path"},
        {ShowIfGroupAttribute, "condition"},

    };
}

