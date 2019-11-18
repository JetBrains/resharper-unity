namespace JetBrains.ReSharper.Plugins.Unity.Yaml.Psi
{
    public static class UnityYamlConstants
    {
        public static readonly string AssetsFolder = "Assets";
        
        public static readonly string Prefab = ".prefab";
        public static readonly string Scene = ".unity";
        public static readonly string MonoScript = ".cs";
        
        
        // Unity components
        public static readonly string TransformComponent = "Transform";
        public static readonly string RectTransformComponent = "RectTransform";

        // Yaml file properties
        public static readonly string GameObjectProperty = "m_GameObject";
        public static readonly string RootOrderProperty = "m_RootOrder";
        public static readonly string NameProperty = "m_Name";
        public static readonly string ScriptProperty = "m_Script";
        public static readonly string FatherProperty = "m_Father";
        public static readonly string CorrespondingSourceObjectProperty = "m_CorrespondingSourceObject";
        public static readonly string CorrespondingSourceObjectProperty2017 = "m_PrefabParentObject";
        public static readonly string PrefabInstanceProperty = "m_PrefabInstance";
        public static readonly string PrefabInstanceProperty2017 = "m_PrefabInternal";
        public static readonly string TransformParentProperty = "m_TransformParent";
        public static readonly string ModificationProperty = "m_Modification";
        public static readonly string Components = "m_Component";
        public static readonly string Children = "m_Children";
        public static readonly string ModificationsProperty = "m_Modifications";
        
        // prefab modifications
        public static readonly string PropertyPathProperty = "propertyPath";
        public static readonly string TargetProperty = "target";
        public static readonly string ValueProperty = "value";

    }
}