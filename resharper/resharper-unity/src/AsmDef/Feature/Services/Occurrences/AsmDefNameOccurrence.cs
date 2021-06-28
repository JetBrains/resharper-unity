using JetBrains.ProjectModel;
using JetBrains.ReSharper.Plugins.Unity.AsmDefCommon.Feature.Services.Occurrences;
using JetBrains.ReSharper.Psi;
using JetBrains.ReSharper.Psi.JavaScript.LanguageImpl.JSon;

namespace JetBrains.ReSharper.Plugins.Unity.AsmDef.Feature.Services.Occurrences
{
    public class AsmDefNameOccurrence : AsmDefNameOccurrenceBase<JsonLanguage>
    {
        public AsmDefNameOccurrence(string name, IPsiSourceFile sourceFile,
            int declaredElementTreeOffset, int navigationTreeOffset, ISolution solution)
            : base(name, sourceFile, declaredElementTreeOffset, navigationTreeOffset, solution)
        {
        }
    }
}