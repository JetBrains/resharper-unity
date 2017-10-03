using System;

namespace JetBrains.ReSharper.Plugins.Unity.Feature.Services.LiveTemplates.Scope
{
    public class InUnityCSharpAssetsFolder : InUnityCSharpProject
    {
        private static readonly Guid DefaultUID = new Guid("400D0960-419A-4D68-B6BD-024A7C9E4DDB");

        public override Guid GetDefaultUID() => DefaultUID;
        public override string PresentableShortName => "Unity Assets folder";
    }
}