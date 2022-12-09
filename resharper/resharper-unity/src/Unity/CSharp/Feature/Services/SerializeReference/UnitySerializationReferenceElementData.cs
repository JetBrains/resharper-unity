#nullable enable
using System.Linq;
using JetBrains.Collections;
using JetBrains.ReSharper.Daemon.UsageChecking;
using JetBrains.ReSharper.Daemon.UsageChecking.SwaExtension;
using JetBrains.ReSharper.Psi;
using JetBrains.ReSharper.Psi.CSharp.Tree;
using JetBrains.ReSharper.Psi.Tree;
using JetBrains.Util.Collections;

namespace JetBrains.ReSharper.Plugins.Unity.CSharp.Feature.Services.SerializeReference
{
    public class UnitySerializationReferenceElementData : ISwaExtensionData
    {
        private readonly IUnityElementIdProvider myProvider;

        public UnitySerializationReferenceElementData(IUnityElementIdProvider provider)
        {
            myProvider = provider;
        }

        internal ClassMetaInfoDictionary ClassInfoDictionary { get; } = new();
        internal CountingSet<TypeParameterResolve> TypeParameterResolves { get; } = new();

        void ISwaExtensionData.AddData(ISwaExtensionData data)
        {
            var referenceElement = data as UnitySerializationReferenceElementData;
            foreach (var (@class, typeElements) in referenceElement!.ClassInfoDictionary)
            {
                if (!ClassInfoDictionary.ContainsKey(@class))
                    ClassInfoDictionary[@class] =
                        new ClassMetaInfo(typeElements.ClassName, typeElements.SuperClasses,
                            typeElements.SerializeReferenceHolders, typeElements.TypeParameters);
                else
                    ClassInfoDictionary[@class].UnionWith(typeElements);
            }

            foreach (var (key, count) in referenceElement.TypeParameterResolves)
                TypeParameterResolves.Add(key, count);
        }

        ISwaExtensionInfo ISwaExtensionData.ToInfo(CollectUsagesStagePersistentData persistentData)
        {
            return new UnitySerializationReferenceElementInfo(this);
        }

        void ISwaExtensionData.ProcessBeforeInterior(ITreeNode element, IParameters parameters)
        {
        }

        void ISwaExtensionData.ProcessAfterInterior(ITreeNode element, IParameters parameters)
        {
        }

        void ISwaExtensionData.ProcessNode(ITreeNode element, IParameters parameters)
        {
            //IClassLikeDeclaration - class, struct, interface, record
            if (element is not IClassLikeDeclaration classLikeDeclaration)
                return;

            //TODO - struct without superclass as Interface - couldn't be used with SerializeRef attribute
            // if(!classLikeDeclaration.IsPartial && !classLikeDeclaration.SuperTypes.Any() && classLikeDeclaration.DeclaredElement is IStruct)
            //     return;
            
            var classInfoAdapter = classLikeDeclaration.DeclaredElement!.ToAdapter(myProvider);
            SerializeReferenceTypesUtils.CollectClassData(classInfoAdapter, ClassInfoDictionary, TypeParameterResolves);
        }
    }
}