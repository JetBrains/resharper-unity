// using System;
// using System.Collections.Generic;
// using JetBrains.Application.Progress;
// using JetBrains.ReSharper.Feature.Services.Navigation.ContextNavigation;
// using JetBrains.ReSharper.Feature.Services.Navigation.Requests;
// using JetBrains.ReSharper.Feature.Services.Occurrences;
// using JetBrains.ReSharper.Plugins.Unity.InputActions.Feature.Services.Occurrences;
// using JetBrains.ReSharper.Plugins.Unity.InputActions.Psi.DeclaredElements;
// using JetBrains.ReSharper.Psi;
// using JetBrains.Util;
//
// namespace JetBrains.ReSharper.Plugins.Unity.InputActions.Feature.Services.Navigation.Requests
// {
//     public class InputActionsNameSearchDeclarationRequest : SearchDeclarationsRequest
//     {
//         public InputActionsNameSearchDeclarationRequest(DeclaredElementTypeUsageInfo elementInfo, Func<bool> checkCancelled)
//             : base(elementInfo)
//         {
//         }
//
//         public override ICollection<IOccurrence> Search(IProgressIndicator progressIndicator)
//         {
//             var declaredElement = Target.GetValidDeclaredElement() as InputActionsDeclaredElement;
//             if (declaredElement == null)
//                 return EmptyList<IOccurrence>.InstanceList;
//
//             return new[]
//             {
//                 new InputActionsNameOccurrence(declaredElement.ShortName, declaredElement.SourceFile,
//                     declaredElement.DeclarationOffset, declaredElement.NavigationOffset,
//                     declaredElement.GetSolution())
//             };
//         }
//     }
// }