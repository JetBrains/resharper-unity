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

        static TypeResolver()
        {
            Specials = typeof(string).Assembly.GetTypes().ToDictionary(GetSpecialName, t => t);
        }

        public static void AddAssembly([NotNull] Assembly assembly)
        {
            if (Assemblies.Contains(assembly)) return;

            Console.WriteLine($"Loading types from {assembly.FullName}...");
            Type[] types = assembly.GetTypes();
            Console.WriteLine($"Adding {types.Length} types from {assembly.FullName}...");
            Entries.Insert(0, types);
        }

        [NotNull]
        public static Type Resolve([NotNull] string name)
        {
            if (Specials.ContainsKey(name)) return Specials[name];
            Type[] candidates = Entries.SelectMany(l => l).Where(t => name == t.Name).ToArray();

            if (candidates.Any()) return candidates.First();

            throw new ApplicationException($"Unknown type '{name}'.");
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