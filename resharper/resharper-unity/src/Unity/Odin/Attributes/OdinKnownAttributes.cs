using System.Collections.Generic;
using JetBrains.Metadata.Reader.API;
using JetBrains.Metadata.Reader.Impl;

namespace JetBrains.ReSharper.Plugins.Unity.Odin.Attributes;

public class OdinKnownAttributes
{
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

    
    
    public static readonly HashSet<IClrTypeName> LayoutAttributes = new()
    {
        BoxGroupAttribute,
        ButtonGroupAttribute,
        HorizontalGroupAttribute,
        ResponsiveButtonGroupAttribute,
        VerticalGroupAttribute,
        FoldoutGroupAttribute,
        
        TabGroupAttribute,
        TabGroupAttribute,
        
        ToggleGroupAttribute,
        TitleGroupAttribute,
    };
    
    public static readonly Dictionary<(IClrTypeName clrName, string parameterName), LayoutParameterKind> LayoutAttributesParameterKinds = new()
    {
        {(BoxGroupAttribute, "group"), LayoutParameterKind.Group},
        {(ButtonGroupAttribute, "group"), LayoutParameterKind.Group},
        {(HorizontalGroupAttribute, "group"), LayoutParameterKind.Group},
        {(ResponsiveButtonGroupAttribute, "group"), LayoutParameterKind.Group},
        {(VerticalGroupAttribute, "groupId"), LayoutParameterKind.Group},
        {(FoldoutGroupAttribute, "groupName"), LayoutParameterKind.Group},

        {(TabGroupAttribute, "group"), LayoutParameterKind.Group},
        {(TabGroupAttribute, "tab"), LayoutParameterKind.Tab},

        {(ToggleGroupAttribute, "toggleMemberName"), LayoutParameterKind.Group},
        
        {(TitleGroupAttribute, "title"), LayoutParameterKind.Group},
        
        {(HideIfGroupAttribute, "path"), LayoutParameterKind.Group},
        {(ShowIfGroupAttribute, "condition"), LayoutParameterKind.Group},

    };
    
    public enum LayoutParameterKind
    {
        Group,
        Tab
    }
}

