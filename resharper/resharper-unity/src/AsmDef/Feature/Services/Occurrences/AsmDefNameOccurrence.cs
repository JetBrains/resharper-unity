using JetBrains.ProjectModel;
using JetBrains.ReSharper.Plugins.Unity.JsonNew.Psi;
using JetBrains.ReSharper.Psi;

namespace JetBrains.ReSharper.Plugins.Unity.AsmDef.Feature.Services.Occurrences
{
    public class AsmDefNameOccurrence : AsmDefNameOccurrenceBase<JsonNewLanguage>
    {
        public AsmDefNameOccurrence(string name, IPsiSourceFile sourceFile,
            int declaredElementTreeOffset, int navigationTreeOffset, ISolution solution)
            : base(name, sourceFile, declaredElementTreeOffset, navigationTreeOffset, solution)
        {
        }
    }
}