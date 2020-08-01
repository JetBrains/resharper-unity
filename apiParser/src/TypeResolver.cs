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
        private readonly Dictionary<string, Version> myIsObsolete = new Dictionary<string, Version>();

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

            // TODO: Provide a better way of handling generics?
            // We only have one use so far, in AssetModificationProcessor.CanOpenForEdit in 2020.2. ApiParser will fail
            // if we get any others, as the list above are the only non-Unity types we recognise. Note that using
            // typeof(List<string>).FullName will give an assembly qualified name for the type parameter, including
            // culture, version and public key token. We need a simple type name.
            myFullNames.Add("List<string>", "System.Collections.Generic.List`1[[System.String]]");
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
                // message. This works for experimental types that move namespaces such as PlayState and Playable
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

        public bool IsObsolete(string fullName, Version currentVersion)
        {
            return myIsObsolete.TryGetValue(fullName, out var fromVersion) && currentVersion >= fromVersion;
        }

        public void MarkObsolete(string name, Version version)
        {
            var fullName = ResolveFullName(name, "");
            if (myIsObsolete.TryGetValue(fullName, out var fromVersion) && fromVersion < version)
                return;

            myIsObsolete[fullName] = version;
        }

        public void AddType(string shortName, string fullName)
        {
            myFullNames.Add(shortName, fullName);
            myFullNames.Add(fullName, fullName);
        }

        public ApiType CreateApiType(string name, string namespaceHint = "")
        {
            var fullname = ResolveFullName(name, namespaceHint);
            return new ApiType(fullname);
        }
    }
}