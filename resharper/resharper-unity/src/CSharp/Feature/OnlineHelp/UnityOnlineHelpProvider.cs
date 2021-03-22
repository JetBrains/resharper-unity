// using System;
// using JetBrains.Application;
// using JetBrains.ReSharper.Feature.Services.OnlineHelp;
// using JetBrains.ReSharper.Plugins.Unity.Application.UI.Help;
// using JetBrains.ReSharper.Psi;
// using JetBrains.ReSharper.Psi.Modules;
//
// namespace JetBrains.ReSharper.Plugins.Unity.CSharp.Feature.OnlineHelp
// {
//     [ShellComponent]
//     public class UnityOnlineHelpProvider : IOnlineHelpProvider
//     {
//         private readonly ShowUnityHelp myShowUnityHelp;
//
//         public UnityOnlineHelpProvider(ShowUnityHelp showUnityHelp)
//         {
//             myShowUnityHelp = showUnityHelp;
//         }
//         
//         private Uri GetUnityUri(ICompiledElement element)
//         {
//             var searchableText = element.GetSearchableText();
//             return searchableText == null
//                 ? null
//                 : myShowUnityHelp.GetUri(searchableText);
//         }
//
//         public Uri GetUrl(IDeclaredElement element)
//         {
//             if (element is ICompiledElement el)
//             {
//                 if (!(el.Module is IAssemblyPsiModule module)) return null;
//                 if (!(element is ITypeElement || element is ITypeMember)) return null;
//
//                 var assemblyLocation = module.Assembly.Location;
//                 if (assemblyLocation == null || !assemblyLocation.ExistsFile)
//                     return null;
//
//                 if (!(assemblyLocation.Name.StartsWith("UnityEngine") || assemblyLocation.Name.StartsWith("UnityEditor")))
//                     return null;
//                 return GetUnityUri(el);
//             }
//
//             return null;
//         }
//
//         public bool IsAvailable(IDeclaredElement element)
//         {
//             return true;
//         }
//
//         public int Priority => 20;
//         public bool ShouldValidate => false;
//     }
// }