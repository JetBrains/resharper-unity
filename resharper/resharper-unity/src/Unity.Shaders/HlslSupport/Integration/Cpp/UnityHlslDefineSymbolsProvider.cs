#nullable enable
using System;
using System.Collections.Generic;
using JetBrains.ProjectModel;
using JetBrains.ReSharper.Plugins.Unity.UnityEditorIntegration;
using JetBrains.ReSharper.Psi.Cpp.Symbols;

namespace JetBrains.ReSharper.Plugins.Unity.Shaders.HlslSupport.Integration.Cpp
{
    [SolutionComponent]
    public class UnityHlslDefineSymbolsProvider
    {
        private readonly IUnityVersion myUnityVersion;
        
        public UnityHlslDefineSymbolsProvider(IUnityVersion unityVersion)
        {
            myUnityVersion = unityVersion;
        }

        public IEnumerable<CppPPDefineSymbol> GetDefineSymbols()
        {
            yield return CppPPDefineSymbolUtil.ParsePredefinedMacro("SHADER_API_D3D11");
            yield return CppPPDefineSymbolUtil.ParsePredefinedMacro("__RESHARPER__");
            yield return CppPPDefineSymbolUtil.ParsePredefinedMacro("INTERNAL_DATA= ");
            yield return CppPPDefineSymbolUtil.ParsePredefinedMacro("WorldReflectionVector(data,normal)=data.worldRefl");
            yield return CppPPDefineSymbolUtil.ParsePredefinedMacro("WorldNormalVector(data,normal)=normal");
            yield return CppPPDefineSymbolUtil.CreatePredefinedSymbol("UNITY_VERSION", GetHlslUnityVersion().ToString());
        }
        
        private int GetHlslUnityVersion()
        {
            var version = myUnityVersion.ActualVersionForSolution.Maybe.ValueOrDefault;
            return version != null ? version.Major * 1_00 + version.Minor * 10 + Math.Min(version.Build, 9) : 0;
        }
    }
}