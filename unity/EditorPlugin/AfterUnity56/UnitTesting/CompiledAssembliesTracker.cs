using System.Collections.Generic;
using System.IO;
using System.Linq;
using JetBrains.Rider.Model.Unity.BackendUnity;
using UnityEditor.Compilation;
using UnityEngine;

namespace JetBrains.Rider.Unity.Editor.AfterUnity56.UnitTesting
{
  internal static class CompiledAssembliesTracker
  {
    private static UnityModelAndLifetime ourModelAndLifetime;
    private static readonly HashSet<string> ourCompiledAssemblyPaths = new HashSet<string>();

    public static void Init(UnityModelAndLifetime modelAndLifetime)
    {
      ourModelAndLifetime = modelAndLifetime;

      void OnCompilationFinished(string assemblyPath, CompilerMessage[] messages)
      {
        if (!ourCompiledAssemblyPaths.Contains(assemblyPath))
          UpdateAssemblies();
      }

      modelAndLifetime.Lifetime.Bracket(
        () => CompilationPipeline.assemblyCompilationFinished += OnCompilationFinished,
        () => CompilationPipeline.assemblyCompilationFinished -= OnCompilationFinished);

      UpdateAssemblies();
    }

    private static void UpdateAssemblies()
    {
      ourCompiledAssemblyPaths.Clear();

      var projectPath = Directory.GetParent(Application.dataPath).FullName;
      var compiledAssemblies = CompilationPipeline.GetAssemblies().Select(a =>
      {
        ourCompiledAssemblyPaths.Add(a.outputPath);

        var fullOutputPath = Path.Combine(projectPath, a.outputPath);
        return new CompiledAssembly(a.name, fullOutputPath);
      })
      .ToList();

      if (ourModelAndLifetime.Lifetime.IsAlive)
        ourModelAndLifetime.Model.CompiledAssemblies(compiledAssemblies);
    }
  }
}