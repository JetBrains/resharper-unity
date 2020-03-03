using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using JetBrains.Annotations;
using JetBrains.Util;

namespace ApiParser
{
    internal class TypeResolver
    {
        private readonly OneToSetMap<string, string> myFullNames = new OneToSetMap<string, string>();
        private readonly HashSet<string> myIsObsolete = new HashSet<string>();

        public TypeResolver()
        {
            myFullNames.Add("string", typeof(string).FullName);
            myFullNames.Add("int", typeof(int).FullName);
            myFullNames.Add("void", typeof(void).FullName);
            myFullNames.Add("bool", typeof(bool).FullName);
            myFullNames.Add("object", typeof(object).FullName);
            myFullNames.Add("float", typeof(float).FullName);
            myFullNames.Add("double", typeof(double).FullName);
            myFullNames.Add("IEnumerator", typeof(IEnumerator).FullName);
        }

        private string ResolveFullName([NotNull] string name, string namespaceHint)
        {
            var suffix = "";
            if (name.EndsWith("&"))
            {
                suffix = "&";
                name = name.Substring(0, name.Length - 1);
            }
            else if (name.EndsWith("[]"))
            {
                suffix = "[]";
                name = name.Substring(0, name.Length - 2);
            }
            
            var candidates = myFullNames[name];
            if (!candidates.Any())
            {
                var n = name;
                candidates = myFullNames[n];
                if (!candidates.Any())
                {
                    if (name.LastIndexOf('.') > 0)
                    {
                        n = name.Substring(name.LastIndexOf('.') + 1);

                        candidates = myFullNames[n];
                    }
                }

                if (!candidates.Any())
                    throw new ApplicationException($"Unknown type '{name}'.");
            }

            if (candidates.Count > 1)
            {
                // If we have more than one type with the same name, choose the one in the same namespace as the owning
                // message. This works for experimental types that move namespaces PlayState and Playable
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

            return candidates.Single() + suffix;
        }

        public bool IsObsolete(string fullName)
        {
            return myIsObsolete.Contains(fullName);
        }

        public void AddType(string shortName, string fullName, bool isObsolete)
        {
            myFullNames.Add(shortName, fullName);
            myFullNames.Add(fullName, fullName);
            if (isObsolete)
            {
                myIsObsolete.Add(shortName);
                myIsObsolete.Add(fullName);
            }
        }

        public ApiType CreateApiType(string name, string namespaceHint = "")
        {
            var fullname = ResolveFullName(name, namespaceHint);
            return new ApiType(fullname);
        }
    }
}