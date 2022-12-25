using System;
using System.Security.Policy;
using JetBrains.Application;
using JetBrains.ProjectModel;
using JetBrains.ReSharper.Feature.Services.OnlineHelp;
using JetBrains.ReSharper.Plugins.Unity.AsmDef.Psi.Caches;
using JetBrains.ReSharper.Plugins.Unity.Core.ProjectModel;
using JetBrains.ReSharper.Plugins.Unity.UnityEditorIntegration.Packages;
using JetBrains.ReSharper.Psi;
using JetBrains.ReSharper.Psi.Util;

namespace JetBrains.ReSharper.Plugins.Unity.CSharp.Feature.OnlineHelp
{
    // Unity registry package have their api doc online like:
    // https://docs.unity3d.com/Packages/com.unity.mathematics@1.2/api/Unity.Mathematics.math.clamp.html

    [ShellComponent]
    public class UnityPackagesOnlineHelpProvider : CompiledElementOnlineHelpProvider
    {
        public override string GetPresentableName(IDeclaredElement element)
        {
            if (element is not ICompiledElement compiledElement) 
                return base.GetPresentableName(element);
            
            if (!IsApplicable(compiledElement)) return base.GetPresentableName(element);

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
            if (!IsApplicable(compiledElement)) return null;
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

            var urlHost = "docs.unity3d.com";
            if (!packageData.Id.StartsWith("com.unity.") && packageData.PackageDetails.DocumentationUrl != null && Uri.TryCreate(packageData.PackageDetails.DocumentationUrl, UriKind.Absolute, out var result)) 
                urlHost = result.Host;
            
            return new Uri($"https://{urlHost}/Packages/{packageData.Id}@{version.ToString(2)}/api/{compiledElement.GetSearchableText()}.html");
        }

        private static bool IsPublic(ICompiledElement element)
        {
            return element is ITypeMember typeMemberElement && typeMemberElement.AccessibilityDomain.DomainType ==
                AccessibilityDomain.AccessibilityDomainType.PUBLIC;
        }

        private static bool IsApplicable(ICompiledElement element)
        {
            var solution = element.GetSolution();
            if (!solution.HasUnityReference()) return false;
            if (!IsPublic(element)) return false;
            var asmDefCache = solution.GetComponent<AsmDefCache>();
            var asmDefLocation = asmDefCache.GetAsmDefLocationByAssemblyName(element.Module.Name);
            if (asmDefLocation.IsEmpty)
                return false;
            var packageManager = solution.GetComponent<PackageManager>();
            var packageData = packageManager.GetOwningPackage(asmDefLocation);
            if (packageData == null)
                return false;

            if (packageData.Source != PackageSource.Registry)
                return false;

            if (!packageData.Id.StartsWith("com.unity.") && (packageData.PackageDetails.DocumentationUrl == null ||
                !Uri.TryCreate(packageData.PackageDetails.DocumentationUrl, UriKind.Absolute, out _)))
                return false;
            
            if (!Version.TryParse(packageData.PackageDetails.Version, out _)) return false;

            return true;
        }

        // same priority as MsdnOnlineHelpProvider,
        // but this provider only applies to Unity registry packages and MSDN only applies to Microsoft/Mono
        public override int Priority => 10; 
        public override bool ShouldValidate => false;
    }
}