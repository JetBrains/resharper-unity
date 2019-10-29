using System.Collections.Generic;
using JetBrains.ReSharper.Feature.Services.Refactorings.Specific.Rename;
using JetBrains.ReSharper.Plugins.Unity.AsmDefCommon.Feature.Services.Refactorings;
using JetBrains.ReSharper.Plugins.Unity.AsmDefNew.Psi.DeclaredElements;
using JetBrains.ReSharper.Psi;
using JetBrains.ReSharper.Refactorings.Rename;
using JetBrains.Util;

namespace JetBrains.ReSharper.Plugins.Unity.AsmDefNew.Feature.Services.Refactorings
{
    // Support renaming the asmdef file to match the name of the assembly
    [FileRenameProvider]
    public class AsmDefFileRenameProvider : AsmDefFileRenameProviderBase<AsmDefNameDeclaredElement>
    { 
    }
}