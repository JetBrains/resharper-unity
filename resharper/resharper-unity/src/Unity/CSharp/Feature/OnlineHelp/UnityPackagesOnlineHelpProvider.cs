using System;
using JetBrains.Application;
using JetBrains.Application.Parts;
using JetBrains.ProjectModel;
using JetBrains.ReSharper.Feature.Services.OnlineHelp;
using JetBrains.ReSharper.Plugins.Unity.AsmDef.Psi.Caches;
using JetBrains.ReSharper.Plugins.Unity.Core.ProjectModel;
using JetBrains.ReSharper.Plugins.Unity.UnityEditorIntegration.Packages;
using JetBrains.ReSharper.Psi;
using JetBrains.Util;

namespace JetBrains.ReSharper.Plugins.Unity.CSharp.Feature.OnlineHelp
{
    // Unity registry package have their api doc online like:
    // https://docs.unity3d.com/Packages/com.unity.mathematics@1.2/api/Unity.Mathematics.math.clamp.html

    [ShellComponent(Instantiation.DemandAnyThreadSafe)]
    public class UnityPackagesOnlineHelpProvider : CompiledElementOnlineHelpProvider
    {
        public override string GetPresentableName(IDeclaredElement element)
        {
            if (element is not ICompiledElement compiledElement) 
                return base.GetPresentableName(element);
            
            if (!IsApplicable(compiledElement)) return base.GetPresentableName(element);
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

            var linkPart = GetSearchableText(compiledElement)!.Replace("+", ".").Replace("`", "-");

            if (!JetSemanticVersion.TryParse(packageData.PackageDetails.Version, out var version))
                return null;

            var urlHost = "docs.unity3d.com";
            if (!packageData.Id.StartsWith("com.unity.") 
                && packageData.PackageDetails.DocumentationUrl != null 
                && Uri.TryCreate(packageData.PackageDetails.DocumentationUrl, UriKind.Absolute, out var result)) 
                urlHost = result.Host;
            
            return new Uri($"https://{urlHost}/Packages/{packageData.Id}@{version.Major}.{version.Minor}/api/{linkPart}.html");
        }
        
        public static string GetSearchableText(ICompiledElement compiledElement)
        {
            // RIDER-100677 Broken links in Unity documentation
            
            if (compiledElement is ITypeMember typeMemberElement)
            {
                var containingType = typeMemberElement.ContainingType;
                if (containingType != null)
                { 
                    return containingType.GetClrName().FullName;
                }
            }

            return compiledElement.GetSearchableText();
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

            // example https://docs.unity3d.com/Packages/com.unity.ugui@1.0/api/index.html is BuiltIn
            if (packageData.Source != PackageSource.Registry && packageData.Source != PackageSource.BuiltIn)
                return false;

            if (!packageData.Id.StartsWith("com.unity.") && (packageData.PackageDetails.DocumentationUrl == null ||
                !Uri.TryCreate(packageData.PackageDetails.DocumentationUrl, UriKind.Absolute, out _)))
                return false;
            
            if (!JetSemanticVersion.TryParse(packageData.PackageDetails.Version, out _)) return false;

            if (element.GetSearchableText() == null) return false;

            return true;
        }

        // more preferable then UnityCompiledElementOnlineHelpProvider
        public override int Priority => 5; 
        public override bool ShouldValidate => false;
    }
}