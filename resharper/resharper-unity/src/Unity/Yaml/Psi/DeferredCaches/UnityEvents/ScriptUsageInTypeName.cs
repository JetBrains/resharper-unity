using JetBrains.ReSharper.Plugins.Unity.Yaml.Psi.DeferredCaches.AssetHierarchy.References;
using JetBrains.Util;

namespace JetBrains.ReSharper.Plugins.Unity.Yaml.Psi.DeferredCaches.UnityEvents;

public class ScriptUsageInTypeName : IScriptUsage
{
    public ScriptUsageInTypeName(LocalReference location, ExternalReference usageTarget, string typeName,
        string psiModuleName, TextRange range)
    {
        Location = location;
        UsageTarget = usageTarget;
        TypeName = typeName;
        PSIModuleName = psiModuleName;
        Range = range;
    }

    public LocalReference Location { get; }
    public ExternalReference UsageTarget { get; }
    public string TypeName { get; }
    public string PSIModuleName { get; }
    public TextRange Range { get; }

    protected bool Equals(ScriptUsageInTypeName other)
    {
        return Location.Equals(other.Location) && UsageTarget.Equals(other.UsageTarget) && TypeName == other.TypeName &&
               PSIModuleName == other.PSIModuleName && Range.Equals(other.Range);
    }

    public override bool Equals(object obj)
    {
        if (ReferenceEquals(null, obj)) return false;
        if (ReferenceEquals(this, obj)) return true;
        if (obj.GetType() != this.GetType()) return false;
        return Equals((ScriptUsageInTypeName)obj);
    }

    public override int GetHashCode()
    {
        unchecked
        {
            var hashCode = Location.GetHashCode();
            hashCode = (hashCode * 397) ^ UsageTarget.GetHashCode();
            hashCode = (hashCode * 397) ^ (TypeName != null ? TypeName.GetHashCode() : 0);
            hashCode = (hashCode * 397) ^ (PSIModuleName != null ? PSIModuleName.GetHashCode() : 0);
            hashCode = (hashCode * 397) ^ Range.GetHashCode();
            return hashCode;
        }
    }
}