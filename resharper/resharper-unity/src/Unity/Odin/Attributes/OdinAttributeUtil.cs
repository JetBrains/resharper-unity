#nullable enable
using System.Collections.Generic;
using JetBrains.ProjectModel;
using JetBrains.ReSharper.Plugins.Unity.Core.Feature.Services.Technologies;
using JetBrains.ReSharper.Psi;

namespace JetBrains.ReSharper.Plugins.Unity.Odin.Attributes;

public class OdinAttributeUtil
{
    public static List<OdinGroupInfo> CollectGroupInfo(ITypeElement typeElement)
    {
        var result = new List<OdinGroupInfo>();
        foreach (var member in typeElement.GetMembers())
        {
            foreach (var attributeInstance in member.GetAttributeInstances(true))
            {
                if (!OdinKnownAttributes.LayoutAttributes.TryGetValue(attributeInstance.GetClrName(), out var parameterName))
                    continue;

                var basePath = GetBaseGroupPath(attributeInstance);
                if (basePath == null)
                    continue;
                
                var majorPath = GetMajorGroupPath(attributeInstance);
                if (majorPath != null)
                {
                    result.Add(new OdinGroupInfo(majorPath, member, attributeInstance, true));
                    result.Add(new OdinGroupInfo(basePath, member, attributeInstance, false));

                }
                else
                {
                    result.Add(new OdinGroupInfo(basePath, member, attributeInstance, true));
                }
            }
        }

        return result;
    }

    public struct OdinGroupInfo
    {
        public string GroupPath { get; }
        public ITypeMember Member { get; }
        public IAttributeInstance AttributeInstance { get; }
        
        public bool IsMajorGroup { get; }

        public OdinGroupInfo(string groupPath, ITypeMember member, IAttributeInstance attributeInstance, bool isMajorGroup)
        {
            GroupPath = groupPath;
            Member = member;
            AttributeInstance = attributeInstance;
            IsMajorGroup = isMajorGroup;
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

    public static string? GetBaseGroupPath(IAttributeInstance attributeInstance)
    {
        if (!OdinKnownAttributes.LayoutAttributes.TryGetValue(attributeInstance.GetClrName(), out var parameterName))
            return null;
        
        var result = GetAttributeValue(attributeInstance, parameterName);
        if (result == null && attributeInstance.GetClrName().Equals(OdinKnownAttributes.TabGroupAttribute))
        {
            return "_DefaultTabGroup";
        }

        return result;
    }

    public static string? GetMajorGroupPath(IAttributeInstance attributeInstance)
    {
        var basePath = GetBaseGroupPath(attributeInstance);
        if (basePath == null)
            return null;
        
        if (attributeInstance.GetClrName().Equals(OdinKnownAttributes.TabGroupAttribute))
        {
            return $"{basePath}/{GetAttributeValue(attributeInstance, "tab") ?? ""}";
        }

        return basePath;
    }

    public static bool HasOdinSupport(ISolution solution)
    {
        return HasOdinSupport(solution.GetComponent<UnityTechnologyDescriptionCollector>());
    }
    
    public static bool HasOdinSupport(UnityTechnologyDescriptionCollector technologyDescriptionCollector)
    {
        return false;
        //return technologyDescriptionCollector.DiscoveredTechnologies.ContainsKey(OdinUnityTechnologyDescription.OdinId);
    }
}