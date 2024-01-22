#nullable enable
using JetBrains.ReSharper.Psi;

namespace JetBrains.ReSharper.Plugins.Unity.Odin.Attributes;

public class OdinAttributeUtil
{
    public static bool IsLayoutAttribute(ITypeElement? typeElement)
    {
        if (typeElement == null)
            return false;
        
        return OdinKnownAttributes.LayoutAttributes.Contains(typeElement.GetClrName());
    }

    public static string? GetLayoutName(IAttributeInstance attributeInstance, OdinKnownAttributes.LayoutParameterKind kind)
    {
        var clrName = attributeInstance.GetClrName();

        if (!OdinKnownAttributes.LayoutAttributes.Contains(clrName))
            return null;

        var constructor = attributeInstance.Constructor;
        if (constructor == null)
            return null;

        var index = -1;
        foreach (var parameter in constructor.Parameters)
        {
            index++;
            var name = parameter.ShortName;
            if (!OdinKnownAttributes.LayoutAttributesParameterKinds.TryGetValue((clrName, name), out var parameterKind))
                continue;
            
            if (!parameterKind.Equals(kind))
                continue;
            
            var value = attributeInstance.PositionParameter(index);
            if (value.IsBadValue)
            {
                value = attributeInstance.NamedParameter(name);
            }
            
            if (value.IsBadValue)
                continue;

            if (!value.IsConstant)
                continue;

            if (value.ConstantValue.IsString())
                return value.ConstantValue.StringValue;
        }

        return null;
    }
}