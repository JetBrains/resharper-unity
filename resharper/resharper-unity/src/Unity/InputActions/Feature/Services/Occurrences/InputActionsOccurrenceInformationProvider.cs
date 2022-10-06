// using JetBrains.Diagnostics;
// using JetBrains.IDE;
// using JetBrains.ProjectModel;
// using JetBrains.ReSharper.Feature.Services.Occurrences;
// using JetBrains.ReSharper.Feature.Services.Occurrences.OccurrenceInformation;
// using JetBrains.ReSharper.Psi;
// using JetBrains.ReSharper.Psi.Pointers;
// using JetBrains.Util;
//
// namespace JetBrains.ReSharper.Plugins.Unity.InputActions.Feature.Services.Occurrences
// {
//     [SolutionFeaturePart]
//     public class InputActionsOccurrenceInformationProvider : IOccurrenceInformationProvider2
//     {
//         public IDeclaredElementEnvoy GetTypeMember(IOccurrence occurrence) => null;
//         public IDeclaredElementEnvoy GetTypeElement(IOccurrence occurrence) => null;
//         public IDeclaredElementEnvoy GetNamespace(IOccurrence occurrence) => null;
//         public OccurrenceMergeContext GetMergeContext(IOccurrence occurrence) => new OccurrenceMergeContext(occurrence);
//
//         public TextRange GetTextRange(IOccurrence occurrence)
//         {
//             var InputActionsNameOccurrence = (occurrence as InputActionsNameOccurrence).NotNull("InputActionsNameOccurrence != null");
//             return new TextRange(InputActionsNameOccurrence.NavigationTreeOffset,
//                 InputActionsNameOccurrence.NavigationTreeOffset + InputActionsNameOccurrence.Name.Length);
//         }
//
//         public ProjectModelElementEnvoy GetProjectModelElementEnvoy(IOccurrence occurrence) => null;
//
//         public SourceFilePtr GetSourceFilePtr(IOccurrence occurrence) =>
//             (occurrence as InputActionsNameOccurrence)?.SourceFile.Ptr() ?? SourceFilePtr.Fake;
//
//         public bool IsApplicable(IOccurrence occurrence) => occurrence is InputActionsNameOccurrence;
//
//         public void SetTabOptions(TabOptions tabOptions, IOccurrence occurrence)
//         {
//         }
//     }
// }