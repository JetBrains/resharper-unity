using System;
using JetBrains.Application;
using JetBrains.ProjectModel;
using JetBrains.ReSharper.Feature.Services.OnlineHelp;
using JetBrains.ReSharper.Plugins.Unity.AsmDef.Psi.Caches;
using JetBrains.ReSharper.Plugins.Unity.Core.ProjectModel;
using JetBrains.ReSharper.Plugins.Unity.UnityEditorIntegration.Packages;
using JetBrains.ReSharper.Psi;

namespace JetBrains.ReSharper.Plugins.Unity.CSharp.Feature.OnlineHelp
{
    // Unity registry package have their api doc online like:
    // https://docs.unity3d.com/Packages/com.unity.mathematics@1.2/api/Unity.Mathematics.math.clamp.html
    
    // todo: somehow skip the non-unity npm 
    
    [ShellComponent]
    public class UnityPackagesOnlineHelpProvider : CompiledElementOnlineHelpProvider
    {
        public override string GetPresentableName(IDeclaredElement element)
        {
            if (element is not ICompiledElement compiledElement) 
                return base.GetPresentableName(element);
            
            if (!IsUnityPackageCompiledCode(compiledElement)) return base.GetPresentableName(element);

            // todo: check
            // if (compiledElement is ITypeMember typeMemberElement)
            // {
            //     var containingType = typeMemberElement.ContainingType;
            //     if (containingType is IEnum) 
            //         return ShowUnityHelp.FormatDocumentationKeyword($"{containingType.GetClrName().FullName}.{typeMemberElement.ShortName}");
            // }
            
            return base.GetPresentableName(element);
        }

        public override Uri GetUrl(ICompiledElement compiledElement)
        {
            if (!IsUnityPackageCompiledCode(compiledElement)) return null;
            var solution = compiledElement.GetSolution();
            
            var asmDefCache = solution.GetComponent<AsmDefCache>();
            var asmDefLocation = asmDefCache.GetAsmDefLocationByAssemblyName(compiledElement.Module.Name);
            var packageManager = solution.GetComponent<PackageManager>();
            var packageData = packageManager.GetOwningPackage(asmDefLocation);    
            
            // todo: check
            // if (compiledElement is ITypeMember typeMemberElement)
            // {
            //     var containingType = typeMemberElement.ContainingType;
            //     if (containingType is IEnum) 
            //         return ShowUnityHelp.FormatDocumentationKeyword($"{containingType.GetClrName().FullName}.{typeMemberElement.ShortName}");
            // }

            var version = new Version(packageData.PackageDetails.Version);
            var ulr = $"https://docs.unity3d.com/Packages/{packageData.Id}@{version.ToString(2)}/api/{compiledElement.GetSearchableText()}.html";
            return new Uri(ulr);
        }

        private static bool IsUnityPackageCompiledCode(ICompiledElement element)
        {
            var solution = element.GetSolution();
            if (!solution.HasUnityReference()) return false;
            var asmDefCache = solution.GetComponent<AsmDefCache>();
            var asmDefLocation = asmDefCache.GetAsmDefLocationByAssemblyName(element.Module.Name);
            if (asmDefLocation.IsEmpty)
                return false;
            var packageManager = solution.GetComponent<PackageManager>();
            var package = packageManager.GetOwningPackage(asmDefLocation);
            if (package == null)
                return false;

            if (package.Source != PackageSource.Registry)
                return false;
            
            if (!Version.TryParse(package.PackageDetails.Version, out _)) return false;

            return true;
        }

        // same priority as MsdnOnlineHelpProvider,
        // but this provider only applies to Unity registry packages and MSDN only applies to Microsoft/Mono
        public override int Priority => 10; 
        public override bool ShouldValidate => false;
    }
}