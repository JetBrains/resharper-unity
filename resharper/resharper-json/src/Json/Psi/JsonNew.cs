using JetBrains.Annotations;
using JetBrains.ReSharper.Psi;

namespace JetBrains.ReSharper.Plugins.Unity.JsonNew.Psi
{
    [LanguageDefinition(Name)]
    public class JsonNewLanguage : KnownLanguage
    {
        public new const string Name = "JSON_NEW";
        
        [CanBeNull, UsedImplicitly]
        public static JsonNewLanguage Instance { get; private set; }

        public JsonNewLanguage() : base(Name, "Json")
        {
        }
    }
}
