#nullable enable
using System.Collections.Generic;
using JetBrains.ReSharper.Psi;
using JetBrains.ReSharper.Psi.Tree;

namespace JetBrains.ReSharper.Plugins.Unity.Odin.Attributes;

public class OdinAttributeUtil
{
    public static bool IsLayoutAttribute(ITypeElement? typeElement)
    {
        if (typeElement == null)
            return false;
        
        return OdinKnownAttributes.LayoutAttributes.ContainsKey(typeElement.GetClrName());
    }

    public static List<OdinGroupInfo> CollectGroupInfo(ITypeElement typeElement)
    {
        var result = new List<OdinGroupInfo>();
        foreach (var member in typeElement.GetMembers())
        {
            foreach (var attributeInstance in member.GetAttributeInstances(true))
            {
                if (!OdinKnownAttributes.LayoutAttributes.TryGetValue(attributeInstance.GetClrName(), out var parameterName))
                    continue;
                
                var group = GetAttributeValue(attributeInstance, parameterName);

                if (attributeInstance.GetClrName().Equals(OdinKnownAttributes.TabGroupAttribute))
                {
                    if (group == null)
                        group = "_DefaultTabGroup";

                    group = $"{group}/{GetAttributeValue(attributeInstance, "tab") ?? ""}";
                }
                
                result.Add(new OdinGroupInfo(group, member, attributeInstance));
            }
        }

        return result;
    }

    public struct OdinGroupInfo
    {
        public string GroupPath { get; }
        public ITypeMember Member { get; }
        public IAttributeInstance AttributeInstance { get; }

        public OdinGroupInfo(string groupPath, ITypeMember member, IAttributeInstance attributeInstance)
        {
            GroupPath = groupPath;
            Member = member;
            AttributeInstance = attributeInstance;
        }
    }
    
    private static string? GetAttributeValue(IAttributeInstance attributeInstance, string parameterName)
    {
        var constructor = attributeInstance.Constructor;
        if (constructor == null)
            return null;

        var index = -1;
        foreach (var parameter in constructor.Parameters)
        {
            index++;
            
            if (!parameter.ShortName.Equals(parameterName))
                continue;
            
            var value = attributeInstance.PositionParameter(index);
            if (value.IsBadValue)
            {
                value = attributeInstance.NamedParameter(parameterName);
            }
            
            if (value.IsBadValue)
                break;

            if (!value.IsConstant)
                break;

            if (value.ConstantValue.IsString())
                return value.ConstantValue.StringValue;
        }

        return null;
    }
}