#nullable enable
using System.Collections.Generic;
using JetBrains.ReSharper.Plugins.Unity.Shaders.ShaderLab.Language;

namespace JetBrains.ReSharper.Plugins.Unity.Shaders.HlslSupport.Integration.Cpp;

public interface IUnityHlslCustomDefinesProvider
{
    IEnumerable<string> ProvideCustomDefines(UnityHlslDialectBase dialect);
}