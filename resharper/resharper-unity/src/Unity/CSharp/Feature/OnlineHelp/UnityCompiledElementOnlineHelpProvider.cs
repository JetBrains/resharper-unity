using System;
using JetBrains.Annotations;
using JetBrains.Application;
using JetBrains.Application.Parts;
using JetBrains.ReSharper.Feature.Services.OnlineHelp;
using JetBrains.ReSharper.Plugins.Unity.Core.Application.UI.Help;
using JetBrains.ReSharper.Plugins.Unity.Core.ProjectModel;
using JetBrains.ReSharper.Psi;
using JetBrains.ReSharper.Psi.Modules;

namespace JetBrains.ReSharper.Plugins.Unity.CSharp.Feature.OnlineHelp
{
    [ShellComponent(Instantiation.DemandAnyThreadSafe)]
    public class UnityCompiledElementOnlineHelpProvider : CompiledElementOnlineHelpProvider
    {
        private readonly ShowUnityHelp myShowUnityHelp;

        public UnityCompiledElementOnlineHelpProvider(ShowUnityHelp showUnityHelp)
        {
            myShowUnityHelp = showUnityHelp;
        }

        public override string GetPresentableName(IDeclaredElement element)
        {
            if (!(element is ICompiledElement compiledElement)) 
                return base.GetPresentableName(element);
            
            if (!IsUnityCompiledCode(compiledElement)) return base.GetPresentableName(element);
            
            if (compiledElement is ITypeMember typeMemberElement)
            {
                var containingType = typeMemberElement.ContainingType;
                if (containingType is IEnum) 
                    return ShowUnityHelp.FormatDocumentationKeyword($"{containingType.GetClrName().FullName}.{typeMemberElement.ShortName}");
            }
            return base.GetPresentableName(element);
        }

        public override Uri GetUrl(ICompiledElement element)
        {
            if (!IsUnityCompiledCode(element)) return null;

            var searchableText = GetSearchableText(element);
            if (searchableText == null) return null;

            return myShowUnityHelp.GetUri(searchableText);
        }

        private static bool IsUnityCompiledCode(ICompiledElement element)
        {
            if (!element.GetSolution().HasUnityReference()) return false;
            if (!(element.Module is IAssemblyPsiModule module)) return false;
            var assemblyLocation = module.Assembly.Location;
            if (assemblyLocation?.AssemblyPhysicalPath?.ExistsFile != true)
                return false;

            if (!(assemblyLocation.Name.StartsWith("UnityEngine") || assemblyLocation.Name.StartsWith("UnityEditor")))
                return false;
            return true;
        }

        [Pure, CanBeNull]
        private static string GetSearchableText(ICompiledElement compiledElement)
        {
            if (compiledElement is ITypeMember typeMemberElement)
            {
                var containingType = typeMemberElement.ContainingType;
                if (containingType is IEnum) 
                    return ShowUnityHelp.FormatDocumentationKeyword($"{containingType.GetClrName().FullName}.{typeMemberElement.ShortName}");
            }
            
            return ShowUnityHelp.FormatDocumentationKeyword(compiledElement.GetSearchableText());
        }
        
        // setting this to be more preferable then MsdnOnlineHelpProvider, because Unity assemblies (only compiled locally, I guess) sometimes have CompanyName = `Microsoft`
        // https://youtrack.jetbrains.com/issue/RIDER-101136/Pressing-F1-for-UnityEngine.UI-classes-members-is-no-longer-working-properly#focus=Comments-27-8323079.0-0
        public override int Priority => 7; 
        public override bool ShouldValidate => false;
    }
}