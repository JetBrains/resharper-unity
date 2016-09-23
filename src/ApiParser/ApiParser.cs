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
        private readonly ApiWriter api = new ApiWriter();
        private readonly string rootPath;

        public ApiParser([NotNull] string path)
        {
            rootPath = path;
        }

        public event EventHandler<ProgressEventArgs> Progress;

        public void ExportTo(XmlWriter writer)
        {
            api.WriteTo(writer);
        }

        public void ParseFolder()
        {
            var files = Directory.EnumerateFiles(rootPath, @"*.html").ToArray();

            api.Enter("messages");
            for (var i = 0; i < files.Length; ++i)
            {
                ParseFile(files[i]);
                OnProgress(new ProgressEventArgs(i + 1, files.Length));
            }

            api.Leave("messages");
        }

        private static void ParseMessageParameters([NotNull] IEnumerable<Argument> arguments,
            [NotNull] IReadOnlyList<ApiNode> parameters)
        {
            var i = 0;
            foreach (var argument in arguments)
            {
                argument.Name = parameters[i]?[1]?.Text ?? argument.Name;
                argument.Description = parameters[i]?[3]?.Text ?? argument.Description;
                ++i;
            }
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

        private static void ParseMessageExample(string messageName, IReadOnlyList<Argument> arguments,
            ApiNode example, ref ApiType type)
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
                type = new ApiType(m.Groups[2].Value);
                var parameters = m.Groups[1].Value.Split(',');

                for (var i = 0; i < arguments.Count; ++i)
                {
                    arguments[i].Name = parameters[i].Split(':')[0];
                }

                return;
            }

            var csRegex = new Regex($@"(\w+) {messageName}\(([^)]*)\)");
            m = csRegex.Match(exampleText);
            if (m.Success)
            {
                var nameRegex = new Regex(@"\W(\w+)$");

                type = new ApiType(m.Groups[1].Value);
                var parameters = m.Groups[2].Value.Split(',');
                for (var i = 0; i < arguments.Count; ++i)
                {
                    arguments[i].Name = nameRegex.Replace(parameters[i], "$1");
                }

                return;
            }

            Console.WriteLine(exampleText);
        }

        private static void ResolveArguments([NotNull] string message, [NotNull] ApiNode details,
            [NotNull] IReadOnlyList<Argument> arguments, [NotNull] ref ApiType type)
        {
            var parameters = details.Subsection("Parameters").ToArray();
            if (parameters.Any())
            {
                ParseMessageParameters(arguments, parameters);
                return;
            }

            var example = PickExample(details);
            if (example == null) return;

            ParseMessageExample(message, arguments, example, ref type);
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

            api.Enter("type");

            var clsType = NsRegex.Replace(ns.Text, "$1");
            api.SetAttribute("kind", clsType);
            api.SetAttribute("name", cls.Text);

            var nsName = NsRegex.Replace(ns.Text, "$2");
            api.SetAttribute(@"ns", nsName);

            var hostType = new ApiType(string.Concat(nsName, ".", cls.Text));

            api.SetAttribute("path", new Uri(filename).AbsoluteUri);

            foreach (var message in messages)
            {
                string detailsPath;
                ApiType type;
                if (!ParseMessage(message, out detailsPath, out type)) continue;

                api.LeaveTo("message");
                api.SetAttribute("path", new Uri(detailsPath).AbsoluteUri);

                api.Enter("returns");
                api.SetAttribute("type", type.FullName);
                api.SetAttribute("array", type.IsArray);
            }
        }

        private bool ParseMessage([NotNull] ApiNode message, out string path, out ApiType type)
        {
            path = string.Empty;
            type = new ApiType("void");

            var link = message.SelectOne(@"td.lbl/a");
            var desc = message.SelectOne(@"td.desc");
            if (link == null || desc == null) return false;

            var detailsPath = link[@"href"];
            if (string.IsNullOrWhiteSpace(detailsPath)) return false;

            path = Path.Combine(rootPath, detailsPath);
            if (!File.Exists(path)) return false;

            var detailsDoc = ApiNode.Load(path);
            var details = detailsDoc?.SelectOne(@"//div.content/div.section");
            var signature = details?.SelectOne(@"div.mb20.clear/h1.heading.inherit");
            var staticNode = details?.SelectOne(@"div.subsection/p/code.varname[text()='static']");

            if (signature == null) return false;

            api.Enter("message");
            api.SetAttribute("name", link.Text);
            api.SetAttribute("static", staticNode != null);
            var description = desc.Text;
            if (!string.IsNullOrWhiteSpace(description))
                api.SetAttribute("description", description);

            // E.g. OnCollisionExit2D(Collision2D)
            var argumentString = SigRegex.Replace(signature.Text, "$2$3");
            if (string.IsNullOrWhiteSpace(argumentString)) return true;

            var argumentStrings = argumentString.Split(',')
                .Select(s => s.Trim())
                .ToArray();
            var total = argumentStrings.Length;
            var arguments = argumentStrings.Select((s, i) => new Argument(s, i, total)).ToArray();

            ResolveArguments(link.Text, details, arguments, ref type);

            api.Enter("parameters");
            foreach (var argument in arguments)
            {
                api.Enter("parameter");

                api.SetAttribute("type", argument.Type.FullName);
                api.SetAttribute("array", argument.Type.IsArray);
                api.SetAttribute("name", argument.Name);
                if (!string.IsNullOrWhiteSpace(argument.Description))
                    api.SetAttribute("description", argument.Description);
            }

            return true;
        }
    }
}