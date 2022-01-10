﻿using System;
using JetBrains.ProjectModel.Properties;
using JetBrains.ReSharper.Feature.Services.LiveTemplates.Scope;
using JetBrains.ReSharper.Psi;
using JetBrains.ReSharper.Psi.CSharp;

// DefaultUID is convention, but inconsistent
// ReSharper disable InconsistentNaming

namespace JetBrains.ReSharper.Plugins.Unity.CSharp.Feature.Services.LiveTemplates.Scope
{
    // Defines a scope point, but has no inherent behaviour, other than to compare against
    // other scope points. A template can declare that it requires this scope point, and
    // the template will only be made available if a ScopeProvider "publishes" this scope
    // point based on the current context (e.g. the project is a Unity project)
    public class InUnityCSharpProject : InLanguageSpecificProject
    {
        private static readonly Guid DefaultUID = new Guid("B37325A3-4F0A-405B-8A5C-00ECA4ED3B30");
        private static readonly Guid QuickUID = new Guid("D32F297F-E422-4612-839A-FE76D9914B34");

        public InUnityCSharpProject()
            : base(ProjectLanguage.CSHARP)
        {
            AdditionalSuperTypes.Add(typeof(InLanguageSpecificProject));
        }

        public override Guid GetDefaultUID() => DefaultUID;
        public override string PresentableShortName => "Unity projects";

        public override PsiLanguageType RelatedLanguage => CSharpLanguage.Instance;

        // Define the name and UID of the QuickList we'll use for Unity projects. Any
        // scope points that are subsets will appear in this QuickList (I think)
        public override string QuickListTitle => "Unity projects";
        public override Guid QuickListUID => QuickUID;
    }

    // TODO: Add scope for inside the Packages folder (with or without PSI project?)
    // TODO: Add scope for inside an assembly definition folder


    // Scope is anywhere inside the Assets folder. Note that this does not include packages. It will include assembly
    // definitions if they are inside Assets, but not elsewhere.
    // TODO: Should it have a base of C# project? What about folders outside of the PSI project structure?
    public class InUnityCSharpAssetsFolder : InUnityCSharpProject
    {
        private static readonly Guid DefaultUID = new Guid("400D0960-419A-4D68-B6BD-024A7C9E4DDB");

        public override Guid GetDefaultUID() => DefaultUID;
        public override string PresentableShortName => "Unity Assets folder";
    }

    // Scope is Assets/**/Editor/**
    public class InUnityCSharpEditorFolder : InUnityCSharpAssetsFolder
    {
        private static readonly Guid DefaultUID = new Guid("725DF216-7E35-4AAF-8C8E-3FEF06B172AA");

        public override Guid GetDefaultUID() => DefaultUID;
        public override string PresentableShortName => "Unity Editor folder";
    }

    // Scope is anywhere under non-editor folder under Assets: Assets/**/!Editor/**
    public class InUnityCSharpRuntimeFolder : InUnityCSharpAssetsFolder
    {
        private static readonly Guid DefaultUID = new Guid("AD3BD55C-0026-4C29-B6AD-6B82170CD657");

        public override Guid GetDefaultUID() => DefaultUID;
        public override string PresentableShortName => "Unity runtime folder";
    }


    // Scope is anywhere under a firstpass folder in Assets:
    // Assets/Standard Assets/**
    // Assets/Pro Standard Assets/**
    // Assets/Plugins/**
    // (For clarity, this includes Editor folders)
    public class InUnityCSharpFirstpassFolder : InUnityCSharpAssetsFolder
    {
        private static readonly Guid DefaultUID = new Guid("9B4C634E-812C-4699-BED0-7FC0A34533DB");

        public override Guid GetDefaultUID() => DefaultUID;
        public override string PresentableShortName => "Unity firstpass folder";
    }

    // Scope is anywhere under an Editor folder in a firstpass folder of Assets:
    // Assets/Standard Assets/**/Editor/**
    // Assets/Pro Standard Assets/**/Editor/**
    // Assets/Plugins/**/Editor/**
    public class InUnityCSharpFirstpassEditorFolder : InUnityCSharpFirstpassFolder
    {
        private static readonly Guid DefaultUID = new Guid("375D8555-CCD0-4D17-B6F6-2DCC1E01FCAB");

        public override Guid GetDefaultUID() => DefaultUID;
        public override string PresentableShortName => "Unity firstpass Editor folder";
    }

    // Scope is anywhere under a firstpass folder in Assets that is NOT an editor folder:
    // Assets/Standard Assets/**/!Editor/**
    // Assets/Pro Standard Assets/**/!Editor/**
    // Assets/Plugins/**/!Editor/**
    public class InUnityCSharpFirstpassRuntimeFolder : InUnityCSharpFirstpassFolder
    {
        private static readonly Guid DefaultUID = new Guid("101DB5F5-CE2E-4CD3-954F-34CE9AB3ECEA");

        public override Guid GetDefaultUID() => DefaultUID;
        public override string PresentableShortName => "Unity firstpass runtime folder";
    }
}