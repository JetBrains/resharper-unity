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
        private static readonly OneToListMap<string, Type> Types = new OneToListMap<string, Type>();

        static TypeResolver()
        {
            Types.Add("string", typeof(string));
            Types.Add("int", typeof(int));
            Types.Add("void", typeof(void));
            Types.Add("bool", typeof(bool));
            Types.Add("object", typeof(object));
            Types.Add("float", typeof(float));
            Types.Add("double", typeof(double));
        }

        public static void AddAssembly([NotNull] Assembly assembly)
        {
            if (Assemblies.Contains(assembly)) return;
            Assemblies.Add(assembly);

            var types = assembly.GetExportedTypes();
            Console.WriteLine($"Adding {types.Length} types from {assembly.FullName}...");

            foreach (var type in types)
            {
                Types.Add(type.Name, type);
                if (type.FullName != null)
                    Types.Add(type.FullName, type);
            }
        }

        [NotNull]
        public static Type Resolve([NotNull] string name, string namespaceHint)
        {
            var candidates = Types[name];
            if (!candidates.Any()) throw new ApplicationException($"Unknown type '{name}'.");

            if (candidates.Count > 1)
            {
                // If we have more than one type with the same name, choose the one in the
                // same namespace as the owning message. This works for PlayState and Playable
                foreach (var candidate in candidates)
                {
                    Console.WriteLine($"Namespace hint: {namespaceHint}");
                    if (candidate.Namespace == namespaceHint)
                    {
                        Console.WriteLine("WARNING: Multiple candidates for `{0}`. Choosing `{1}` based on namespace", name, candidate.FullName);
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
    }
}