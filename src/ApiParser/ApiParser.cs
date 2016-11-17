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
        private static readonly Regex NsRegex = new Regex(@"^(class|struct) in\W*(\w+(?:\.\w+)*)$");
        private static readonly Regex SigRegex = new Regex(@"^(?:[\w.]+)?\.(\w+)(?:\((.*)\)|(.*))$");
        private readonly string rootPath;
        [NotNull] private readonly UnityApi api;

        public ApiParser([NotNull] string path, [NotNull] UnityApi api)
        {
            rootPath = path;
            this.api = api;
        }

        public event EventHandler<ProgressEventArgs> Progress;

        public void ExportTo(XmlTextWriter writer)
        {
            api.ExportTo(writer);
        }

        public void ParseFolder()
        {
            var files = Directory.EnumerateFiles(rootPath, @"*.html").ToArray();

            for (var i = 0; i < files.Length; ++i)
            {
                ParseFile(files[i]);
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

        private void ParseFile([NotNull] string filename)
        {
            var document = ApiNode.Load(filename);
            var section = document?.SelectOne(@"//div.content/div.section");
            var header = section?.SelectOne(@"div.mb20.clear");
            var cls = header?.SelectOne(@"h1.heading.inherit");
            var ns = header?.SelectOne(@"p");
            if (cls == null || ns == null) return;

            var messages = section.Subsection("Messages").ToArray();
            if (messages.Length == 0) return;

            var clsType = NsRegex.Replace(ns.Text, "$1");
            var nsName = NsRegex.Replace(ns.Text, "$2");
            var unityApiType = api.AddType(nsName, cls.Text, clsType, new Uri(filename).AbsoluteUri);

            foreach (var message in messages)
                ParseMessage(message, unityApiType);
        }

        private void ParseMessage([NotNull] ApiNode message, UnityApiType unityApiType)
        {
            var link = message.SelectOne(@"td.lbl/a");
            var desc = message.SelectOne(@"td.desc");
            if (link == null || desc == null) return;

            var detailsPath = link[@"href"];
            if (string.IsNullOrWhiteSpace(detailsPath)) return;

            var path = Path.Combine(rootPath, detailsPath);
            if (!File.Exists(path)) return;

            var detailsDoc = ApiNode.Load(path);
            var details = detailsDoc?.SelectOne(@"//div.content/div.section");
            var signature = details?.SelectOne(@"div.mb20.clear/h1.heading.inherit");
            var staticNode = details?.SelectOne(@"div.subsection/p/code.varname[text()='static']");

            if (signature == null) return;

            var messageName = link.Text;
            var returnType = new ApiType("void");
            string[] argumentNames = null;

            var example = PickExample(details);
            if (example != null)
            {
                var tuple = ParseDetailsFromExample(messageName, example);
                returnType = tuple.Item1;
                argumentNames = tuple.Item2;
            }

            var eventFunction = unityApiType.AddEventFunction(messageName, staticNode != null, desc.Text, new Uri(path).AbsoluteUri, returnType);
            ParseParameters(eventFunction, signature, details, argumentNames);
        }

        private static void ParseParameters(UnityApiEventFunction eventFunction, ApiNode signature, ApiNode details, string[] argumentNames)
        {
            // E.g. OnCollisionExit2D(Collision2D) - doesn't always include the argument name
            // Hopefully, we parsed the argument name from the example
            var argumentString = SigRegex.Replace(signature.Text, "$2$3");
            if (string.IsNullOrWhiteSpace(argumentString)) return;

            var argumentStrings = argumentString.Split(',')
                .Select(s => s.Trim())
                .ToArray();
            var total = argumentStrings.Length;
            var arguments = argumentStrings.Select((s, i) => new Argument(s, i, total)).ToArray();

            ResolveArguments(details, arguments, argumentNames);

            foreach (var argument in arguments)
                eventFunction.AddParameter(argument.Name, argument.Type, argument.Description);
        }

        private static void ResolveArguments([NotNull] ApiNode details, [NotNull] IReadOnlyList<Argument> arguments, string[] argumentNames)
        {
            if (argumentNames != null)
            {
                for (var i = 0; i < arguments.Count; i++)
                    arguments[i].Name = argumentNames[i];
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
        private static Tuple<ApiType, string[]> ParseDetailsFromExample(string messageName, ApiNode example)
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
                var returnType = new ApiType(m.Groups[2].Value);
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

                var returnType = new ApiType(m.Groups[1].Value);
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