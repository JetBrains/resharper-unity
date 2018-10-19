﻿using JetBrains.Annotations;
using JetBrains.ReSharper.Psi;

namespace JetBrains.ReSharper.Plugins.Unity.ShaderLab.Psi
{
    [LanguageDefinition(Name)]
    public class ShaderLabLanguage : KnownLanguage
    {
        public new const string Name = "SHADERLAB";

        [CanBeNull]
        public static readonly ShaderLabLanguage Instance = null;

        public ShaderLabLanguage()
            : base(Name, "ShaderLab")
        {
        }
    }
}