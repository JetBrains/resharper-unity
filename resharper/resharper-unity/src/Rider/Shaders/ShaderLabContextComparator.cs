// using System.Collections.Generic;
// using JetBrains.Collections.Viewable;
// using JetBrains.ProjectModel;
// using JetBrains.ReSharper.Plugins.Unity.ProjectModel;
// using JetBrains.ReSharper.Psi.Cpp.Caches;
//
// namespace JetBrains.ReSharper.Plugins.Unity.Rider.Shaders
// {
//     [SolutionComponent]
//     public class ShaderLabContextComparator : IRootFileComparator
//     {
//         private readonly ISolution mySolution;
//         private readonly ShaderContextCache myShaderContextCache;
//         private readonly UnitySolutionTracker myUnitySolutionTracker;
//
//         public ShaderLabContextComparator(ISolution solution, ShaderContextCache shaderContextCache, UnitySolutionTracker unitySolutionTracker)
//         {
//             mySolution = solution;
//             myShaderContextCache = shaderContextCache;
//             myUnitySolutionTracker = unitySolutionTracker;
//         }
//         
//         public bool IsAvailable(CppFileLocation currentFile)
//         {
//             return myUnitySolutionTracker.IsUnityProject.HasTrueValue();
//         }
//
//         public List<CppFileLocation> Sort(CppFileLocation currentFile, List<CppFileLocation> possibleRoots)
//         {
//             var rootFile = myShaderContextCache.GetCustomRootFor(currentFile);
//             if (!rootFile.IsValid())
//                 return possibleRoots;
//             
//             
//             for (int i = 0; i < possibleRoots.Count; i++)
//             {
//                 if (rootFile.Equals(possibleRoots[i]))
//                 {
//                     var first = possibleRoots[0];
//                     possibleRoots[0] = possibleRoots[i];
//                     possibleRoots[i] = first;
//                 }
//             }
//             
//             
//             return possibleRoots;
//         }
//     }
// }