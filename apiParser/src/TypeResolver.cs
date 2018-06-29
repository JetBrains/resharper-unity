using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using JetBrains.Annotations;
using JetBrains.Util;

namespace ApiParser
{
    public static class TypeResolver
    {
        private static readonly HashSet<Assembly> Assemblies = new HashSet<Assembly>();
        private static readonly OneToListMap<string, string> FullNames = new OneToListMap<string, string>();

        static TypeResolver()
        {
            FullNames.Add("string", typeof(string).FullName);
            FullNames.Add("int", typeof(int).FullName);
            FullNames.Add("void", typeof(void).FullName);
            FullNames.Add("bool", typeof(bool).FullName);
            FullNames.Add("object", typeof(object).FullName);
            FullNames.Add("float", typeof(float).FullName);
            FullNames.Add("double", typeof(double).FullName);

            // UnityEngine.Experimental.Director.Playable moved to UnityEngine.Playables in 2017.1
            // We correctly set the max version to 5.6, but if we resolve against types in a newer
            // UnityEngine.dll, we resolve PlayState incorrectly. The heuristic when we have multiple
            // candidates (such as UnityEngine.Experimental.Director.PlayState and
            // UnityEngine.Playables.PlayState) is to prefer the one in the same namespace. This
            // works nicely, so let's give an extra candidate
            FullNames.Add("FrameData", "UnityEngine.Experimental.Director.FrameData");
            FullNames.Add("PlayState", "UnityEngine.Experimental.Director.PlayState");
        }

        public static void AddAssembly([NotNull] Assembly assembly)
        {
            if (Assemblies.Contains(assembly)) return;
            Assemblies.Add(assembly);

            var types = assembly.GetExportedTypes();
            Console.WriteLine($"Adding {types.Length} types from {assembly.FullName}...");

            foreach (var type in types)
            {
                FullNames.Add(type.Name, type.FullName);
                FullNames.Add(
                    type.FullName ?? throw new InvalidOperationException($"Got null full name for type: {type.Name}"),
                    type.FullName);
            }
        }

        [NotNull]
        public static string ResolveFullName([NotNull] string name, string namespaceHint)
        {
            var candidates = FullNames[name];
            if (!candidates.Any())
                throw new ApplicationException($"Unknown type '{name}'.");

            if (candidates.Count > 1)
            {
                // If we have more than one type with the same name, choose the one in the
                // same namespace as the owning message. This works for PlayState and Playable
                foreach (var candidate in candidates)
                {
                    Console.WriteLine($"Namespace hint: {namespaceHint}");
                    if (candidate.StartsWith(namespaceHint))
                    {
                        Console.WriteLine("WARNING: Multiple candidates for `{0}`. Choosing `{1}` based on namespace", name, candidate);
                        return candidate;
                    }
                }

                Console.WriteLine("Cannot resolve type: {0}", name);
                Console.WriteLine("Candidates:");
                foreach (var candidate in candidates)
                    Console.WriteLine(candidate);
                throw new InvalidOperationException("Cannot resolve type");
            }

            return candidates.Single();
        }
    }
}