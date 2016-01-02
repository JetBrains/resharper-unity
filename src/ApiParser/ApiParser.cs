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
        private static readonly Regex NsRegex = new Regex( @"^(class|struct) in\W*(\w+(?:\.\w+)*)$" );
        private static readonly Regex SigRegex = new Regex( @"^(?:[\w.]+)?\.(\w+)(?:\((.*)\)|(.*))$" );
        private readonly ApiWriter _api = new ApiWriter();
        private readonly string _path;

        public ApiParser([NotNull] string path)
        {
            _path = path;
        }

        public event EventHandler<ProgressEventArgs> Progress;

        public void ExportTo(XmlWriter writer)
        {
            _api.WriteTo(writer);
        }

        public void ParseFolder()
        {
            string[] files = Directory.EnumerateFiles(_path, @"*.html").ToArray();

            _api.Enter("messages");
            for (var i = 0; i < files.Length; ++i)
            {
                ParseFile(files[i]);
                OnProgress(new ProgressEventArgs(i + 1, files.Length));
            }

            _api.Leave("messages");
            _api.Enter("types");
            foreach (KeyValuePair<Type, string> pair in TypeIdResolver.CustomEntries.OrderBy(p=>p.Value))
            {
                _api.Enter("type");
                _api.SetAttribute("name", pair.Value);
                _api.SetAttribute("type", pair.Key.FullName);
            }
        }

        private static void ParseMessageParameters([NotNull] IEnumerable<Argument> arguments,
            [NotNull] IReadOnlyList<ApiNode> parameters)
        {
            var i = 0;
            foreach (Argument argument in arguments)
            {
                argument.Name = parameters[i]?[1]?.Text ?? argument.Name;
                argument.Description = parameters[i]?[3]?.Text ?? argument.Description;
                ++i;
            }
        }

        [CanBeNull]
        private static ApiNode PickExample([NotNull] ApiNode details, [NotNull] string type)
        {
            ApiNode example = details.SelectOne($@"div.subsection/pre.codeExample{type}");
            return example == null || example.Text.StartsWith("no example available") ? null : example;
        }

        [CanBeNull]
        private static ApiNode PickExample([NotNull] ApiNode details)
        {
            return PickExample(details, "Raw") ?? PickExample(details, "JS") ?? PickExample(details, "CS");
        }

        private static void ParseMessageExample( string messageName, IReadOnlyList<Argument> arguments,
            ApiNode example, ref ApiType type )
        {
            var blankCleanup1 = new Regex( @"\s+" );
            var blankCleanup2 = new Regex( @"\s*(\W)\s*" );

            string exampleText = example.Text;
            exampleText = blankCleanup1.Replace( exampleText, " " );
            exampleText = blankCleanup2.Replace( exampleText, "$1" );

            var jsRegex = new Regex( $@"(?:\W|^)function {messageName}\(([^)]*)\)(?::(\w+))?\{{" );
            Match m = jsRegex.Match( exampleText );
            if ( m.Success )
            {
                type = new ApiType(m.Groups[2].Value);
                string[] parameters = m.Groups[ 1 ].Value.Split( ',' );

                for ( var i = 0; i < arguments.Count; ++i )
                {
                    arguments[ i ].Name = parameters[ i ].Split( ':' )[ 0 ];
                }

                return;
            }

            var csRegex = new Regex( $@"(\w+) {messageName}\(([^)]*)\)" );
            m = csRegex.Match( exampleText );
            if ( m.Success )
            {
                var nameRegex = new Regex( @"\W(\w+)$" );

                type = new ApiType(m.Groups[1].Value);
                string[] parameters = m.Groups[ 2 ].Value.Split( ',' );
                for ( var i = 0; i < arguments.Count; ++i )
                {
                    arguments[ i ].Name = nameRegex.Replace( parameters[ i ], "$1" );
                }

                return;
            }

            Console.WriteLine( exampleText );
        }

        private static void ResolveArguments( [NotNull] string message, [NotNull] ApiNode details,
            [NotNull] IReadOnlyList<Argument> arguments, [NotNull] ref ApiType type )
        {
            ApiNode[] parameters = details.Subsection( "Parameters" ).ToArray();
            if ( parameters.Any() )
            {
                ParseMessageParameters( arguments, parameters );
                return;
            }

            ApiNode example = PickExample( details );
            if ( example == null ) return;

            ParseMessageExample( message, arguments, example, ref type );
        }

        private void OnProgress([NotNull] ProgressEventArgs e)
        {
            Progress?.Invoke(this, e);
        }

        private void ParseFile([NotNull] string path)
        {
            ApiNode document = ApiNode.Load(path);
            ApiNode section = document?.SelectOne(@"//div.content/div.section");
            ApiNode header = section?.SelectOne( @"div.mb20.clear" );
            ApiNode cls = header?.SelectOne( @"h1.heading.inherit" );
            ApiNode ns = header?.SelectOne( @"p" );
            if ( cls == null || ns == null ) return;

            ApiNode[] messages = section.Subsection("Messages").ToArray();
            if ( messages.Length == 0 ) return;

            _api.Enter( "type" );

            string clsType = NsRegex.Replace( ns.Text, "$1" );
            _api.SetAttribute( @"type", clsType );
            _api.SetAttribute( "name", cls.Text );

            string nsName = NsRegex.Replace( ns.Text, "$2" );
            _api.SetAttribute( @"ns", nsName );
            _api.SetAttribute("path", new Uri(path).AbsoluteUri);

            foreach (ApiNode message in messages)
            {
                string detailsPath;
                ApiType type;
                if ( !ParseMessage(message, out detailsPath, out type)) continue;

                _api.LeaveTo("message");
                _api.SetAttribute("path", new Uri(detailsPath).AbsoluteUri);

                _api.Enter("returns");
                _api.SetAttribute("type", type.FullName);
                _api.SetAttribute("array", type.IsArray);
                _api.SetAttribute("id", type.Identifier);
            }
        }

        private bool ParseMessage([NotNull] ApiNode message, out string path, out ApiType type)
        {
            path = string.Empty;
            type = new ApiType("void");

            ApiNode link = message.SelectOne(@"td.lbl/a");
            ApiNode desc = message.SelectOne( @"td.desc" );
            if (link == null || desc == null) return false;

            string detailsPath = link[@"href"];
            if (string.IsNullOrWhiteSpace(detailsPath)) return false;

            path = Path.Combine(_path, detailsPath);
            if (!File.Exists(path)) return false;

            ApiNode detailsDoc = ApiNode.Load(path);
            ApiNode details = detailsDoc?.SelectOne( @"//div.content/div.section" );
            ApiNode signature = details?.SelectOne(@"div.mb20.clear/h1.heading.inherit");
            ApiNode staticNode = details?.SelectOne(@"div.subsection/p/code.varname[text()='static']");

            if (signature == null) return false;

            _api.Enter("message");
            _api.SetAttribute("name", link.Text);
            _api.SetAttribute("static", staticNode != null);

            string argumentString = SigRegex.Replace(signature.Text, "$2$3");
            if (string.IsNullOrWhiteSpace(argumentString)) return true;

            string[] argumentStrings = argumentString.Split( ',' )
                .Select( s => s.Trim() )
                .ToArray();
            int total = argumentStrings.Length;
            Argument[] arguments = argumentStrings.Select((s, i) => new Argument(s, i, total)).ToArray();

            ResolveArguments(link.Text, details, arguments, ref type);

            _api.Enter("parameters");
            foreach (Argument argument in arguments)
            {
                _api.Enter("parameter");

                _api.SetAttribute("type", argument.Type.FullName);
                _api.SetAttribute("array", argument.Type.IsArray);
                _api.SetAttribute("id", argument.Type.Identifier);
                _api.SetAttribute("name", argument.Name);
                _api.SetDescription(argument.Description);
            }

            return true;
        }
    }
}