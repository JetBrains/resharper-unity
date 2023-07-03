#nullable enable
using System;
using JetBrains.ReSharper.Feature.Services.LiveTemplates.Scope;
using JetBrains.ReSharper.Plugins.Unity.Shaders.Resources;

namespace JetBrains.ReSharper.Plugins.Unity.Shaders.ShaderLab.Feature.Services.LiveTemplates.Scope
{
    /// Mandatory scope point. Must be in root of ShaderLab file. Can be used for creation of whole Shader commands. 
    public class MustBeInShaderLabRoot : InUnityShaderLabFile, IMandatoryScopePoint
    {
        private static readonly Guid ourDefaultGuid = new("D0D46DBF-51CD-4906-A31C-54750C5577A4");
        
        public override Guid GetDefaultUID() => ourDefaultGuid;
        public override string PresentableShortName => Strings.InUnityShaderLabRoot_PresentableShortName;
        public override string ToString() => PresentableShortName;
    }
}