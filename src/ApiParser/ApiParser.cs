using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text.RegularExpressions;
using System.Xml;
using JetBrains.Annotations;

namespace ApiParser
{
    public class ApiParser
    {
        private static readonly Regex NsRegex = new Regex(@"^(?<type>class|struct) in\W*(?<namespace>\w+(?:\.\w+)*)$");
        private static readonly Regex SigRegex = new Regex(@"^(?:[\w.]+)?\.(\w+)(?:\((.*)\)|(.*))$");

        private readonly UnityApi api;
        private readonly string myDocRootPath;
        private readonly string myScriptReferencePath;

        public ApiParser(UnityApi api, string docRootPath, string scriptReferencePath)
        {
            this.api = api;
            myDocRootPath = docRootPath;
            myScriptReferencePath = scriptReferencePath;
        }

        public event EventHandler<ProgressEventArgs> Progress;

        public void ExportTo(XmlTextWriter writer)
        {
            api.ExportTo(writer);
        }

        public void ParseFolder(string rootPath, Version apiVersion)
        {
            var path = Path.Combine(rootPath, myScriptReferencePath);
            var files = Directory.EnumerateFiles(path, @"*.html").ToArray();

            for (var i = 0; i < files.Length; ++i)
            {
                ParseFile(rootPath, files[i], apiVersion);
                OnProgress(new ProgressEventArgs(i + 1, files.Length));
            }

            Console.WriteLine();
        }

        [CanBeNull]
        private static ApiNode PickExample([NotNull] ApiNode details, [NotNull] string type)
        {
            var example = details.SelectOne($@"div.subsection/pre.codeExample{type}");
            return example == null || example.Text.StartsWith("no example available") ? null : example;
        }

        [CanBeNull]
        private static ApiNode PickExample([NotNull] ApiNode details)
        {
            return PickExample(details, "Raw") ?? PickExample(details, "JS") ?? PickExample(details, "CS");
        }

        private void OnProgress([NotNull] ProgressEventArgs e)
        {
            Progress?.Invoke(this, e);
        }

        private void ParseFile(string rootPath, string filename, Version apiVersion)
        {
            var document = ApiNode.Load(filename);
            var section = document?.SelectOne(@"//div.content/div.section");
            var header = section?.SelectOne(@"div.mb20.clear");
            var name = header?.SelectOne(@"h1.heading.inherit"); // Type or type member name
            var ns = header?.SelectOne(@"p");   // "class in {ns}"

            // Only interested in types at this point
            if (name == null || ns == null) return;

            // Only types that have messages
            var messages = section.Subsection("Messages").ToArray();
            if (messages.Length == 0) return;

            var match = NsRegex.Match(ns.Text);
            var clsType = match.Groups["type"].Value;
            var nsName = match.Groups["namespace"].Value;

            var docPath = filename.Replace(rootPath, myDocRootPath);
            var unityApiType = api.AddType(nsName, name.Text, clsType, new Uri(docPath).AbsoluteUri, apiVersion);

            foreach (var message in messages)
            {
                var eventFunction = ParseMessage(rootPath, message, apiVersion, nsName);
                unityApiType.MergeEventFunction(eventFunction, apiVersion);
            }
        }

        [CanBeNull]
        private UnityApiEventFunction ParseMessage(string rootPath, ApiNode message, Version apiVersion, string hintNamespace)
        {
            var link = message.SelectOne(@"td.lbl/a");
            var desc = message.SelectOne(@"td.desc");
            if (link == null || desc == null) return null;

            var detailsPath = link[@"href"];
            if (string.IsNullOrWhiteSpace(detailsPath)) return null;

            var path = Path.Combine(rootPath, myScriptReferencePath, detailsPath);
            if (!File.Exists(path)) return null;

            var detailsDoc = ApiNode.Load(path);
            var details = detailsDoc?.SelectOne(@"//div.content/div.section");
            var signature = details?.SelectOne(@"div.mb20.clear/h1.heading.inherit");
            var staticNode = details?.SelectOne(@"div.subsection/p/code.varname[text()='static']");

            if (signature == null) return null;

            var messageName = link.Text;
            var returnType = ApiType.Void;
            string[] argumentNames = null;

            var example = PickExample(details);
            if (example != null)
            {
                var tuple = ParseDetailsFromExample(messageName, example, hintNamespace);
                returnType = tuple.Item1;
                argumentNames = tuple.Item2;
            }

            var docPath = Path.Combine(myDocRootPath, myScriptReferencePath, detailsPath);
            var eventFunction = new UnityApiEventFunction(messageName, staticNode != null, returnType, apiVersion, desc.Text, new Uri(docPath).AbsoluteUri, false);

            ParseParameters(eventFunction, signature, details, hintNamespace, argumentNames);

            return eventFunction;
        }

        private static void ParseParameters(UnityApiEventFunction  eventFunction, ApiNode signature, ApiNode details, string owningMessageNamespace, string[] argumentNames)
        {
            // E.g. OnCollisionExit2D(Collision2D) - doesn't always include the argument name
            // Hopefully, we parsed the argument name from the example
            var argumentString = SigRegex.Replace(signature.Text, "$2$3");
            if (string.IsNullOrWhiteSpace(argumentString)) return;

            var argumentStrings = argumentString.Split(',')
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
            if (argumentNames != null)
            {
                for (var i = 0; i < arguments.Count; i++)
                {
                    if (!string.IsNullOrEmpty(argumentNames[i]))
                        arguments[i].Name = argumentNames[i];
                }
            }

            var parameters = details.Subsection("Parameters").ToArray();
            if (parameters.Any())
                ParseMessageParameters(arguments, parameters);
        }

        private static void ParseMessageParameters([NotNull] IEnumerable<Argument> arguments, [NotNull] IReadOnlyList<ApiNode> parameters)
        {
            var i = 0;
            foreach (var argument in arguments)
            {
                argument.Name = parameters[i]?[1]?.Text ?? argument.Name;
                argument.Description = parameters[i]?[3]?.Text ?? argument.Description;
                ++i;
            }
        }

        // Gets return type and argument names from example
        private static Tuple<ApiType, string[]> ParseDetailsFromExample(string messageName, ApiNode example, string owningMessageNamespace)
        {
            var blankCleanup1 = new Regex(@"\s+");
            var blankCleanup2 = new Regex(@"\s*(\W)\s*");

            var exampleText = example.Text;
            exampleText = blankCleanup1.Replace(exampleText, " ");
            exampleText = blankCleanup2.Replace(exampleText, "$1");

            var jsRegex = new Regex($@"(?:\W|^)function {messageName}\(([^)]*)\)(?::(\w+))?\{{");
            var m = jsRegex.Match(exampleText);
            if (m.Success)
            {
                var returnType = new ApiType(m.Groups[2].Value, owningMessageNamespace);
                var parameters = m.Groups[1].Value.Split(',');

                var arguments = new string[parameters.Length];
                for (var i = 0; i < parameters.Length; ++i)
                {
                    arguments[i] = parameters[i].Split(':')[0];
                }

                return Tuple.Create(returnType, arguments);
            }

            var csRegex = new Regex($@"(\w+) {messageName}\(([^)]*)\)");
            m = csRegex.Match(exampleText);
            if (m.Success)
            {
                var nameRegex = new Regex(@"\W(\w+)$");

                var returnType = new ApiType(m.Groups[1].Value, owningMessageNamespace);
                var parameters = m.Groups[2].Value.Split(',');

                var arguments = new string[parameters.Length];
                for (var i = 0; i < parameters.Length; ++i)
                {
                    arguments[i] = nameRegex.Replace(parameters[i], "$1");
                }

                return Tuple.Create(returnType, arguments);
            }

            return null;
        }
    }
}