using System;
using System.Collections.Generic;
using System.Diagnostics.CodeAnalysis;
using System.IO;
using System.Linq;
using System.Net;
using System.Text.RegularExpressions;
using System.Xml;
using JetBrains.Annotations;
using JetBrains.Util;
using Newtonsoft.Json;

namespace ApiParser
{
    internal class ApiParser
    {
        private static readonly Regex IsCoroutineRegex =
            new Regex(@"(?:can be|as) a co-routine", RegexOptions.IgnoreCase | RegexOptions.Compiled);

        // Capture the argument string from e.g. Class.Method(string s, int a)
        // I don't know why we match without brackets
        private static readonly Regex CaptureArgumentsRegex =
            new Regex(@"^(?:[\w.]+)?\.(?:\w+)(?:\((?<args>.*)\)|(?<args>.*))$", RegexOptions.Compiled);

        private static readonly string ScriptReferenceRelativePath = "ScriptReference";

        private readonly UnityApi myApi;
        private readonly TypeResolver myTypeResolver;

        public ApiParser(UnityApi api, TypeResolver typeResolver)
        {
            myApi = api;
            myTypeResolver = typeResolver;
        }

        public void ExportTo(XmlTextWriter writer)
        {
            myApi.ExportTo(writer);
        }

        public void ParseFolder(string path, Version apiVersion, RiderSupportedLanguages langCode)
        {
            var currentDirectory = Directory.GetCurrentDirectory();
            try
            {
                Directory.SetCurrentDirectory(path);

                var links = LoadTypes(Path.Combine(ScriptReferenceRelativePath, "docdata/toc.json"), apiVersion)
                    .Select(f => (file: Path.Combine(ScriptReferenceRelativePath, f.link) + ".html", f.fullName)).ToArray();

                Console.WriteLine("Number of types: {0}", links.Length);

                var messages = new List<TypeDocument>();
                var progress = 1;
                for (int i = 0; i < links.Length; ++i)
                {
                    // Some of the links in toc.json aren't valid...
                    if (!File.Exists(links[i].file))
                    {
                        Console.WriteLine($"Cannot find file {links[i].file}");
                        progress++;
                        continue;
                    }

                    var document = TypeDocument.Load(links[i].file, links[i].fullName, langCode);
                    progress++;
                    if (document != null)
                    {
                        if (document.IsRemoved)
                        {
                            myTypeResolver.MarkObsolete(document.FullName, apiVersion);
                            if (document.Messages.Length != 0)
                            {
                                Console.WriteLine(
                                    $"{document.FullName} is documented but no longer available in {apiVersion}");
                            }
                        }
                        else if (document.Messages.Length > 0)
                        {
                            messages.Add(document);
                            progress--;
                        }
                    }
                    ReportProgress(progress, links.Length);
                }

                foreach (var document in messages)
                {
                    ReportProgress(progress++, links.Length);

                    var unityApiType =
                        myApi.AddType(document.Namespace, document.ShortName, document.Kind, document.DocPath, apiVersion);

                    foreach (var message in document.Messages)
                    {
                        var eventFunction =
                            ParseMessage(document.ShortName, message, apiVersion, document.Namespace, langCode);
                        if (eventFunction == null)
                            continue;

                        unityApiType.MergeEventFunction(eventFunction, apiVersion);
                    }
                }
            }
            finally
            {
                Directory.SetCurrentDirectory(currentDirectory);
            }

            Console.WriteLine();
        }

        private static void ReportProgress(int current, int total)
        {
            var cursorTop = Console.CursorTop;
            Console.WriteLine("{0,5} / {1,5} ({2:F0}%)", current, total, ((float)current / total) * 100.0f);
            Console.SetCursorPosition(0, cursorTop);
        }

        private IEnumerable<(string link, string fullName)> LoadTypes(string tocJsonPath, Version apiVersion)
        {
            var tocJson = File.ReadAllText(tocJsonPath);
            var toc = JsonConvert.DeserializeObject<Toc>(tocJson);

            if (toc.children == null)
                throw new InvalidDataException($"Cannot load {tocJsonPath}");

            var links = new List<(string link, string fullName)>();
            LoadTypes(toc.children.Single(c => c.title == "UnityEngine"), string.Empty, links);
            LoadTypes(toc.children.Single(c => c.title == "UnityEditor"), string.Empty, links);

            // AssetModificationProcessor is missing from the 5.0 index
            if (apiVersion == new Version(5, 0))
                links.Add(("AssetModificationProcessor", "UnityEditor.AssetModificationProcessor"));

            // Playable is missing from 5.2 and 5.3 index
            if (apiVersion == new Version(5, 2) || apiVersion == new Version(5,3))
                links.Add(("Experimental.Director.Playable", "UnityEngine.Experimental.Directory.Playable"));

            return links;
        }

        private void LoadTypes(Toc entry, string namespacePrefix, List<(string link, string fullName)> links)
        {
            if (entry.IsType)
            {
                var fullName = namespacePrefix + entry.title;
                myTypeResolver.AddType(entry.title, fullName);
                links.Add((entry.link, fullName));
            }

            if (entry.children == null)
            {
                if (!entry.IsType)
                    Console.WriteLine($"Entry {entry.title} has no children and no link");
                return;
            }

            foreach (var child in entry.children)
            {
                if (child.title == "Attributes" || child.title == "Assemblies")
                    continue;

                if (child.title == "Classes" || child.title == "Interfaces" || child.title == "Enumerations")
                    namespacePrefix = entry.IsType ? namespacePrefix + entry.title + "+" : entry.title + ".";
                LoadTypes(child, namespacePrefix, links);
            }
        }

        [CanBeNull]
        private UnityApiEventFunction ParseMessage(string className, SimpleHtmlNode message, Version apiVersion,
            string hintNamespace, RiderSupportedLanguages langCode)
        {
            var link = message.SelectOne(@"td.lbl/a");
            var desc = message.SelectOne(@"td.desc");
            if (link == null || desc == null) return null;

            var detailsPath = link[@"href"];
            if (string.IsNullOrWhiteSpace(detailsPath)) return null;

            var path = Path.Combine(ScriptReferenceRelativePath, detailsPath);
            if (!File.Exists(path)) return null;

            var detailsDoc = SimpleHtmlNode.Load(path);
            var details = detailsDoc?.SelectOne(@"//div.content/div.section");
            var signature = details?.SelectOne(@"div.mb20.clear/h1.heading.inherit");
            var staticNode = details?.SelectOne(@"div.subsection/p/code.varname[text()='static']");

            if (signature == null) return null;

            var isCoroutine = IsCoroutineRegex.IsMatch(details.Text);

            var messageName = link.Text;
            var returnType = ApiType.Void;
            var argumentNames = EmptyArray<string>.Instance;
            var isStaticFromExample = false;

            // Unity 2020.2.0a18 includes EditorWindow.hasUnsavedChanges and EditorWindow.saveChangesMessage as messages
            if (className == "EditorWindow" &&
                (messageName == "hasUnsavedChanges" || messageName == "saveChangesMessage"))
            {
                Console.WriteLine($"Skipping {className}.{messageName} - not a message");
                return null;
            }

            var examples = PickExample(details);
            if (examples.Length > 0)
            {
                var tuple = ParseDetailsFromExample(messageName, examples, hintNamespace);

                // As of 2017.4, the docs for MonoBehaviour.OnCollisionEnter2D don't include a valid example. It demonstrates
                // OnTriggerEnter2D instead. Similar problems for these other methods
                if (tuple == null)
                {
                    var fullName = $"{className}.{messageName}";
                    switch (fullName)
                    {
                        case "MonoBehaviour.OnCollisionEnter2D":
                        case "MonoBehaviour.OnCollisionExit2D":
                        case "MonoBehaviour.OnCollisionStay2D":
                        case "MonoBehaviour.Start":
                        case "MonoBehaviour.OnDestroy":
                        case "EditorWindow.OnProjectChange":
                        case "AssetPostprocessor.OnPostprocessSprites"
                            : // 2018.3 adds example with incorrect casing - OnPostProcessSprites
                            Console.WriteLine(
                                $"WARNING: Unable to parse example for {fullName}. Example incorrect in docs");
                            break;

//                        case "Network.OnDisconnectedFromServer":
//                            Bug in 2018.2 documentation
//                            Console.WriteLine($"WARNING: Missing example for {fullName}");
//                            break;

                        default:
                            foreach (var example in examples)
                            {
                                Console.WriteLine(example.Text);
                                Console.WriteLine();
                            }

                            throw new InvalidOperationException(
                                $"Failed to parse example for {className}.{messageName}");
                    }
                }

                if (tuple != null)
                {
                    returnType = tuple.Item1;
                    argumentNames = tuple.Item2;
                    isStaticFromExample = tuple.Item3;
                }
            }

            if (Equals(returnType, ApiType.IEnumerator))
            {
                returnType = ApiType.Void;
                isCoroutine = true;
            }

            var docPath = Path.Combine(ScriptReferenceRelativePath, detailsPath);
            var eventFunction = new UnityApiEventFunction(messageName, staticNode != null || isStaticFromExample,
                isCoroutine, returnType, apiVersion, docPath);

            eventFunction.AddDescription(desc.Text, langCode);
            
            return ParseParameters(eventFunction, signature, details, hintNamespace, argumentNames, apiVersion, langCode)
                ? eventFunction
                : null;
        }

        private static SimpleHtmlNode[] PickExample([NotNull] SimpleHtmlNode details, [NotNull] string type)
        {
            var nodes = details.SelectMany($@"div.subsection/pre.codeExample{type}");
            if (nodes.Length > 0)
            {
                if (nodes[0].Text.StartsWith("no example available"))
                    return EmptyArray<SimpleHtmlNode>.Instance;
            }

            return nodes;
        }

        private static SimpleHtmlNode[] PickExample([NotNull] SimpleHtmlNode details)
        {
            // Favour C#, it's the most strongly typed
            var examples = PickExample(details, "CS");
            if (examples.IsEmpty())
                examples = PickExample(details, "JS");
            if (examples.IsEmpty())
                examples = PickExample(details, "Raw");
            return examples;
        }

        private bool ParseParameters(UnityApiEventFunction eventFunction, SimpleHtmlNode signature,
            SimpleHtmlNode details, string owningMessageNamespace, string[] argumentNames, Version apiVersion, RiderSupportedLanguages langCode)
        {
            // Capture the arguments string. Note that this might be `string s, int i` or `string, int`
            var match = CaptureArgumentsRegex.Match(signature.Text);
            if (!match.Success)
                return false;

            var argumentString = match.Groups["args"].Value;
            var argumentStrings = argumentString.Split(new[] {','}, StringSplitOptions.RemoveEmptyEntries)
                .Select(s => s.Trim())
                .ToArray();
            var total = argumentStrings.Length;
            var arguments = argumentStrings.Select((arg, i) =>
            {
                var argName = total > 1 ? $"arg{i + 1}" : @"arg";
                var typeName = WebUtility.HtmlDecode(arg);    // E.g. for List&lt;string&gt;
                if (arg.Contains(' '))
                {
                    var parts = arg.Split(' ');
                    argName = parts[1];
                    typeName = parts[0];
                }

                var apiType = myTypeResolver.CreateApiType(typeName, owningMessageNamespace);
                return new Argument(apiType, argName);
            }).ToArray();

            ResolveArguments(details, arguments, argumentNames, langCode);

            // If any of the types we're using have been removed/marked obsolete, then the message is also obsolete
            foreach (var argument in arguments)
            {
                if (myTypeResolver.IsObsolete(argument.Type.FullName, apiVersion))
                    return false;
            }

            foreach (var argument in arguments)
                eventFunction.AddParameter(argument.Name, argument.Type, argument.Descriptions);

            return true;
        }

        private static void ResolveArguments([NotNull] SimpleHtmlNode details,
            [NotNull] IReadOnlyList<Argument> arguments, string[] argumentNames, RiderSupportedLanguages langCode)
        {
            for (var i = 0; i < arguments.Count && i < argumentNames.Length; i++)
            {
                if (!string.IsNullOrEmpty(argumentNames[i]))
                    arguments[i].Name = argumentNames[i];
            }

            var parameters = details.Subsection(LocalizationUtil.GetParametersDivTextByLangCode(langCode)).ToArray();
            if (Enumerable.Any(parameters))
                ParseMessageParameters(arguments, parameters, langCode);
        }

        private static void ParseMessageParameters([NotNull] IEnumerable<Argument> arguments,
            [NotNull] IReadOnlyList<SimpleHtmlNode> parameters, RiderSupportedLanguages langCode)
        {
            var i = 0;
            foreach (var argument in arguments)
            {
                argument.Name = parameters[i].SelectOne(@"td.name.lbl")?.Text ?? argument.Name;
                argument.Descriptions.Add(langCode, parameters[i].SelectOne(@"td.desc")?.Text ?? argument.Descriptions.GetByLangCode(langCode));
                ++i;
            }
        }

        private static readonly Regex SingleLineCommentsRegex =
            new Regex(@"^\s*//.*$", RegexOptions.Compiled | RegexOptions.Multiline);

        private static readonly Regex BlankCleanup1 = new Regex(@"\s+", RegexOptions.Compiled);
        private static readonly Regex BlankCleanup2 = new Regex(@"\s*(\W)\s*", RegexOptions.Compiled);
        private static readonly Regex ArrayFixup = new Regex(@"(\[\])(\w)", RegexOptions.Compiled);
        private static readonly Regex ParameterNameRegex = new Regex(@"^.*?\W(\w+)$", RegexOptions.Compiled);

        // Gets return type and argument names from example
        [CanBeNull]
        private Tuple<ApiType, string[], bool> ParseDetailsFromExample(string messageName, SimpleHtmlNode[] examples,
            string owningMessageNamespace)
        {
            foreach (var example in examples)
            {
                var result = ParseDetailsFromExample(messageName, example, owningMessageNamespace);
                if (result != null)
                    return result;
            }

            return null;
        }

        private Tuple<ApiType, string[], bool> ParseDetailsFromExample(string messageName, SimpleHtmlNode example,
            string owningMessageNamespace)
        {
            var exampleText = example.Text;
            exampleText = SingleLineCommentsRegex.Replace(exampleText, string.Empty);
            exampleText = BlankCleanup1.Replace(exampleText, " ");
            exampleText = BlankCleanup2.Replace(exampleText, "$1");
            exampleText = ArrayFixup.Replace(exampleText, "$1 $2");

            // This matches both C# and JS function signatures
            var functionRegex =
                new Regex(
                    $@"(?:\W|^)(?<static>static\s+)?(?<returnType>\w+\W*)\s+{messageName}\((?<parameters>[^)]*)\)(?::(?<returnType>\w+\W*))?{{");
            var m = functionRegex.Match(exampleText);
            if (m.Success)
            {
                var returnTypeName = m.Groups["returnType"].Value;
                if (returnTypeName == "function") // JS without an explicit return type
                    returnTypeName = "void";
                var returnType = myTypeResolver.CreateApiType(returnTypeName, owningMessageNamespace);
                var parameters = m.Groups["parameters"].Value
                    .Split(new[] {','}, StringSplitOptions.RemoveEmptyEntries);
                var isStatic = m.Groups["static"].Success;

                var arguments = new string[parameters.Length];
                for (var i = 0; i < parameters.Length; ++i)
                {
                    if (parameters[i].Contains(":"))
                        arguments[i] = parameters[i].Split(':')[0];
                    else
                        arguments[i] = ParameterNameRegex.Replace(parameters[i], "$1");
                }

                return Tuple.Create(returnType, arguments, isStatic);
            }

            return null;
        }

#pragma warning disable 649
        [SuppressMessage("ReSharper", "InconsistentNaming")]
        private class Toc
        {
            public string link;
            public string title;
            [CanBeNull] public List<Toc> children;

            public bool IsType => link != null && link != "null";
        }
#pragma warning restore 649
    }
}