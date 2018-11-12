using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text.RegularExpressions;
using System.Xml;
using JetBrains.Annotations;
using JetBrains.Util;

namespace ApiParser
{
    public class ApiParser
    {
        // "Namespace:" is only used in 5.0
        private static readonly Regex CaptureKindAndNamespaceRegex = new Regex(@"^((?<type>class|struct) in|Namespace:)\W*(?<namespace>\w+(?:\.\w+)*)$", RegexOptions.Compiled);
        private static readonly Regex IsCoroutineRegex = new Regex(@"(?:can be|as) a co-routine", RegexOptions.IgnoreCase | RegexOptions.Compiled);

        // Capture the argument string from e.g. Class.Method(string s, int a)
        // I don't know why we match without brackets
        private static readonly Regex CaptureArgumentsRegex = new Regex(@"^(?:[\w.]+)?\.(?:\w+)(?:\((?<args>.*)\)|(?<args>.*))$", RegexOptions.Compiled);

        private static readonly string[] ScriptReferenceRelativePaths = new[]
        {
            @"Documentation" + Path.DirectorySeparatorChar + "en" + Path.DirectorySeparatorChar + "ScriptReference",
            @"Documentation" + Path.DirectorySeparatorChar + "ScriptReference", // 2018.1.6f1
            @"20182-08cdcee9b18e-edited" + Path.DirectorySeparatorChar + "Documentation" + Path.DirectorySeparatorChar + "ScriptReference" // 2018.2.2f1
        };

        private readonly UnityApi myApi;

        public ApiParser(UnityApi api)
        {
            myApi = api;
        }

        public event EventHandler<ProgressEventArgs> Progress;

        public void ExportTo(XmlTextWriter writer)
        {
            myApi.ExportTo(writer);
        }

        public void ParseFolder(string path, Version apiVersion)
        {
            var currentDirectory = Directory.GetCurrentDirectory();
            try
            {
                Directory.SetCurrentDirectory(path);

                var scriptReferenceRelativePath = ScriptReferenceRelativePaths.First(Directory.Exists);

                var files = Directory.EnumerateFiles(scriptReferenceRelativePath, @"*.html").Reverse().ToArray();
                var processed = new HashSet<string>();

                for (var i = 0; i < files.Length; ++i)
                {
                    ParseFile(files[i], apiVersion, processed);
                    OnProgress(new ProgressEventArgs(i + 1, files.Length));
                }
            }
            finally
            {
                Directory.SetCurrentDirectory(currentDirectory);
            }

            Console.WriteLine();
        }

        private static ApiNode[] PickExample([NotNull] ApiNode details, [NotNull] string type)
        {
            return details.SelectMany($@"div.subsection/pre.codeExample{type}");
        }

        private static ApiNode[] PickExample([NotNull] ApiNode details)
        {
            // Favour C#, it's the most strongly typed
            var examples = PickExample(details, "CS");
            if (examples.IsEmpty())
                examples = PickExample(details, "JS");
            if (examples.IsEmpty())
                examples = PickExample(details, "Raw");
            return examples;
        }

        private void OnProgress([NotNull] ProgressEventArgs e)
        {
            Progress?.Invoke(this, e);
        }

        private void ParseFile(string filename, Version apiVersion, HashSet<string> processed)
        {
            if (processed.Contains(filename))
                return;

            processed.Add(filename);

            // We're only interested in the file if it contains messages. Bail early
            // so we don't have to parse it to HTML
            var content = File.ReadAllText(filename);
            if (!content.Contains("Messages"))
                return;

            var document = ApiNode.LoadContent(content);
            var section = document?.SelectOne(@"//div.content/div.section");
            var header = section?.SelectOne(@"div.mb20.clear");
            var removed = header?.SelectOne(@"div[@class='message message-error mb20']");
            var name = header?.SelectOne(@"h1.heading.inherit"); // Type or type member name
            var ns = header?.SelectOne(@"p");   // "class in {ns}"/"struct in {ns}"/"Namespace: {ns}"

            // Only interested in types at this point
            if (name == null || ns == null)
            {
//                Console.WriteLine("File has no types: {0}", filename);
                return;
            }

            // Only types that have messages
            var messages = section.Subsection("Messages").ToArray();
            if (messages.Length == 0) return;

            var match = CaptureKindAndNamespaceRegex.Match(ns.Text);
            var clsType = match.Groups["type"].Value;
            var nsName = match.Groups["namespace"].Value;

            if (string.IsNullOrEmpty(clsType)) clsType = "class";
            if (string.IsNullOrEmpty(nsName))
            {
                // Quick fix up for the 5.0 docs, which don't specify a namespace for AssetModificationProcessor
                if (apiVersion == new Version(5, 0) && name.Text == "AssetModificationProcessor")
                    nsName = "UnityEditor";
                else
                {
                    Console.WriteLine("Missing namespace: {0}", name.Text);
                    return;
                }
            }

            if (removed != null && removed.Text.StartsWith("Removed"))
            {
                Console.WriteLine($"{nsName}.{name.Text} no longer available in {apiVersion}: {removed.Text}");
                return;
            }

            var unityApiType = myApi.AddType(nsName, name.Text, clsType, filename, apiVersion);

            foreach (var message in messages)
            {
                var eventFunction = ParseMessage(name.Text, message, apiVersion, nsName, processed);
                unityApiType.MergeEventFunction(eventFunction, apiVersion);
            }
        }

        [CanBeNull]
        private UnityApiEventFunction ParseMessage(string className, ApiNode message, Version apiVersion,
            string hintNamespace, HashSet<string> processed)
        {
            var link = message.SelectOne(@"td.lbl/a");
            var desc = message.SelectOne(@"td.desc");
            if (link == null || desc == null) return null;

            var detailsPath = link[@"href"];
            if (string.IsNullOrWhiteSpace(detailsPath)) return null;

            var scriptReferenceRelativePath = ScriptReferenceRelativePaths.First(Directory.Exists);
            var path = Path.Combine(scriptReferenceRelativePath, detailsPath);
            processed.Add(path);
            if (!File.Exists(path)) return null;

            var detailsDoc = ApiNode.Load(path);
            var details = detailsDoc?.SelectOne(@"//div.content/div.section");
            var signature = details?.SelectOne(@"div.mb20.clear/h1.heading.inherit");
            var staticNode = details?.SelectOne(@"div.subsection/p/code.varname[text()='static']");

            if (signature == null) return null;

            var isCoroutine = IsCoroutineRegex.IsMatch(details.Text);

            var messageName = link.Text;
            var returnType = ApiType.Void;
            var argumentNames = EmptyArray<string>.Instance;
            var isStaticFromExample = false;

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
                        case "AssetPostprocessor.OnPostprocessSprites": // 2018.3 adds example with incorrect casing - OnPostProcessSprites
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

                            throw new InvalidOperationException($"Failed to parse example for {className}.{messageName}");
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

            var docPath = Path.Combine(scriptReferenceRelativePath, detailsPath);
            var eventFunction = new UnityApiEventFunction(messageName, staticNode != null || isStaticFromExample,
                isCoroutine, returnType, apiVersion, desc.Text, docPath);

            ParseParameters(eventFunction, signature, details, hintNamespace, argumentNames);

            return eventFunction;
        }

        private static void ParseParameters(UnityApiEventFunction eventFunction, ApiNode signature, ApiNode details, string owningMessageNamespace, string[] argumentNames)
        {
            // Capture the arguments string. Note that this might be `string s, int i` or `string, int`
            var match = CaptureArgumentsRegex.Match(signature.Text);
            if (!match.Success)
                return;

            var argumentString = match.Groups["args"].Value;
            var argumentStrings = argumentString.Split(new[] {','}, StringSplitOptions.RemoveEmptyEntries)
                .Select(s => s.Trim())
                .ToArray();
            var total = argumentStrings.Length;
            var arguments = argumentStrings.Select((s, i) => new Argument(s, i, total, owningMessageNamespace)).ToArray();

            ResolveArguments(details, arguments, argumentNames);

            foreach (var argument in arguments)
                eventFunction.AddParameter(argument.Name, argument.Type, argument.Description);
        }

        private static void ResolveArguments([NotNull] ApiNode details, [NotNull] IReadOnlyList<Argument> arguments, string[] argumentNames)
        {
            for (var i = 0; i < arguments.Count && i < argumentNames.Length; i++)
            {
                if (!string.IsNullOrEmpty(argumentNames[i]))
                    arguments[i].Name = argumentNames[i];
            }

            var parameters = details.Subsection("Parameters").ToArray();
            if (Enumerable.Any(parameters))
                ParseMessageParameters(arguments, parameters);
        }

        private static void ParseMessageParameters([NotNull] IEnumerable<Argument> arguments, [NotNull] IReadOnlyList<ApiNode> parameters)
        {
            var i = 0;
            foreach (var argument in arguments)
            {
                argument.Name = parameters[i].SelectOne(@"td.name.lbl")?.Text ?? argument.Name;
                argument.Description = parameters[i].SelectOne(@"td.desc")?.Text ?? argument.Description;
                ++i;
            }
        }

        private static readonly Regex SingleLineCommentsRegex = new Regex(@"^\s*//.*$", RegexOptions.Compiled | RegexOptions.Multiline);
        private static readonly Regex BlankCleanup1 = new Regex(@"\s+", RegexOptions.Compiled);
        private static readonly Regex BlankCleanup2 = new Regex(@"\s*(\W)\s*", RegexOptions.Compiled);
        private static readonly Regex ArrayFixup = new Regex(@"(\[\])(\w)", RegexOptions.Compiled);
        private static readonly Regex ParameterNameRegex = new Regex(@"^.*?\W(\w+)$", RegexOptions.Compiled);

        // Gets return type and argument names from example
        [CanBeNull]
        private static Tuple<ApiType, string[], bool> ParseDetailsFromExample(string messageName, ApiNode[] examples,
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

        private static Tuple<ApiType, string[], bool> ParseDetailsFromExample(string messageName, ApiNode example, string owningMessageNamespace)
        {
            var exampleText = example.Text;
            exampleText = SingleLineCommentsRegex.Replace(exampleText, string.Empty);
            exampleText = BlankCleanup1.Replace(exampleText, " ");
            exampleText = BlankCleanup2.Replace(exampleText, "$1");
            exampleText = ArrayFixup.Replace(exampleText, "$1 $2");

            // This matches both C# and JS function signatures
            var functionRegex = new Regex($@"(?:\W|^)(?<static>static\s+)?(?<returnType>\w+\W*)\s+{messageName}\((?<parameters>[^)]*)\)(?::(?<returnType>\w+\W*))?{{");
            var m = functionRegex.Match(exampleText);
            if (m.Success)
            {
                var returnTypeName = m.Groups["returnType"].Value;
                if (returnTypeName == "function") // JS without an explicit return type
                    returnTypeName = "void";
                var returnType = new ApiType(returnTypeName, owningMessageNamespace);
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
    }
}