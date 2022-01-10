using System.Collections.Generic;
using JetBrains.Annotations;
using JetBrains.ReSharper.Psi;
using JetBrains.ReSharper.Psi.CSharp.Tree;
using JetBrains.Util;

namespace JetBrains.ReSharper.Plugins.Unity.Yaml.Psi.DeferredCaches
{
    public interface IScriptUsagesElementContainer : IUnityAssetDataElementContainer
    {
        [NotNull, ItemNotNull]
        IEnumerable<IScriptUsage> GetScriptUsagesFor([NotNull] IPsiSourceFile sourceFile, [NotNull] ITypeElement declaredElement);
        
        LocalList<IPsiSourceFile> GetPossibleFilesWithScriptUsages([NotNull] IClass scriptClass);

        int GetScriptUsagesCount([NotNull] IClassLikeDeclaration classLikeDeclaration, out bool estimatedResult);
    }
}