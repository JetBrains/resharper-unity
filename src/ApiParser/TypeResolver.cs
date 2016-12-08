using System;
using System.CodeDom;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using JetBrains.Annotations;
using Microsoft.CSharp;

namespace ApiParser
{
    public static class TypeResolver
    {
        private static readonly List<Assembly> Assemblies = new List<Assembly>();
        private static readonly List<Type[]> Entries = new List<Type[]>();
        private static readonly Dictionary<string, Type> Specials;
        private static Type[] ourAllEntries;

        static TypeResolver()
        {
            Specials = typeof(string).Assembly.GetTypes().ToDictionary(GetSpecialName, t => t);
        }

        public static void AddAssembly([NotNull] Assembly assembly)
        {
            if (Assemblies.Contains(assembly)) return;

            Console.WriteLine($"Loading types from {assembly.FullName}...");
            var types = assembly.GetTypes();
            Console.WriteLine($"Adding {types.Length} types from {assembly.FullName}...");
            Entries.Insert(0, types.OrderBy(t => t.Name).ToArray());
            ourAllEntries = null;
            Assemblies.Add(assembly);
        }

        [NotNull]
        public static Type Resolve([NotNull] string name, string namespaceHint)
        {
            if (Specials.ContainsKey(name)) return Specials[name];
            if (ourAllEntries == null) ourAllEntries = Entries.SelectMany(l => l).ToArray();

            var candidates = ourAllEntries.Where(t => name == t.FullName).ToArray();
            if (!candidates.Any()) candidates = ourAllEntries.Where(t => name == t.Name).ToArray();
            if (!candidates.Any()) throw new ApplicationException($"Unknown type '{name}'.");

            if (candidates.Length > 1)
            {
                // If we have more than one type with the same name, choose the one in the
                // same namespace as the owning message. This works for PlayState and Playable
                foreach (var candidate in candidates)
                {
                    if (candidate.Namespace == namespaceHint)
                    {
                        Console.WriteLine("WARNING: Multiple candidates for {0}. Choosing {1} based on namespace", name, candidate.FullName);
                        return candidate;
                    }
                }

                Console.WriteLine("Cannot resolve type: {0}", name);
                Console.WriteLine("Candidates:");
                foreach (var candidate in candidates)
                    Console.WriteLine(candidate.FullName);
                throw new InvalidOperationException("Cannot resolve type");
            }

            return candidates.Single();
        }

        [NotNull]
        private static string GetSpecialName([NotNull] Type type)
        {
            var compiler = new CSharpCodeProvider();
            var typeRef = new CodeTypeReference(type);
            return compiler.GetTypeOutput(typeRef);
        }
    }
}