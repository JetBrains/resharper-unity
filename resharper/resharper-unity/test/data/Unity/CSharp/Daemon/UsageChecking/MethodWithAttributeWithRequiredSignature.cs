using UnityEditor;

// These usages come from the attribute having a method that is marked with [RequiredSignature],
// and does not come from external annotations
public static class A 
{
	[SettingsProvider]
    public static SettingsProvider CreateMyCustomSettingsProvider()
    {
        return null;
    }

    [SettingsProviderGroup]
    public static SettingsProvider[] CreateMyCustomSettingsProviderGroup()
    {
        return null;
    }
}
