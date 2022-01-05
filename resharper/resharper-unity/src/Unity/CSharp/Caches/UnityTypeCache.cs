using System.Linq;
using JetBrains.Annotations;
using JetBrains.ProjectModel;
using JetBrains.ReSharper.Psi;
using JetBrains.ReSharper.Psi.CodeAnnotations;
using JetBrains.Util.Caches;

namespace JetBrains.ReSharper.Plugins.Unity.CSharp.Caches
{
    [SolutionComponent]
    public class UnityTypeCache : IInvalidatingCache
    {
        private readonly UnityVersion myUnityVersion;
        private readonly UnityTypesProvider myUnityTypesProvider;
        private readonly KnownTypesCache myKnownTypesCache;
        private const int MAX_SIZE_OF_CACHE = 1 << 12;
        [NotNull] private readonly DirectMappedCache<ITypeElement, bool> myCompiledElementsCache = new DirectMappedCache<ITypeElement, bool>(MAX_SIZE_OF_CACHE);
        [NotNull] private readonly DirectMappedCache<ITypeElement, bool> mySourceElementsCache = new DirectMappedCache<ITypeElement, bool>(MAX_SIZE_OF_CACHE);


        public UnityTypeCache(UnityVersion unityVersion, UnityTypesProvider unityTypesProvider, KnownTypesCache knownTypesCache)
        {
            myUnityVersion = unityVersion;
            myUnityTypesProvider = unityTypesProvider;
            myKnownTypesCache = knownTypesCache;
        }
        
        public bool IsUnityType(ITypeElement typeElement)
        {
            var cache = typeElement is ICompiledElement ? myCompiledElementsCache : mySourceElementsCache;
            if (cache.TryGetFromCache(typeElement, out var result))
            {
                return result;
            }
            
            result =  UnityTypeUtils.GetBaseUnityTypes(typeElement, myUnityVersion, myUnityTypesProvider, myKnownTypesCache).Any();
            
            cache.AddToCache(typeElement, result);

            return result;
        }
        
        public void Invalidate(PsiChangedElementType changeType)
        {
            if (changeType == PsiChangedElementType.CompiledContentsChanged)
            {
                myCompiledElementsCache.Clear();
            }

            mySourceElementsCache.Clear();
        }
    }
}