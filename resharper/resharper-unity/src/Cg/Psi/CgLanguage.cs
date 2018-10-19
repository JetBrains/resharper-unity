﻿using JetBrains.Annotations;
using JetBrains.ReSharper.Psi;

namespace JetBrains.ReSharper.Plugins.Unity.Cg.Psi
{
    [LanguageDefinition(Name)]
    public class CgLanguage : KnownLanguage
    {
        public new const string Name = "CG";
        
        [CanBeNull]
        public static readonly CgLanguage Instance = null;

        public CgLanguage() : base(Name, "Cg")
        {
        }
    }
}